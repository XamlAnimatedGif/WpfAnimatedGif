using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Threading;
using WpfAnimatedGif.Decoding;
using static WpfAnimatedGif.DelayFrameCollection;

namespace WpfAnimatedGif
{
    internal class DelayFrameAnimation : ObjectAnimationBase
    {
        public DelayFrameCollection KeyFrames { get; }
        public DelayFrame FirstFrame => KeyFrames[0];

        private TimeSpan _oversleep = TimeSpan.Zero;

        public DelayFrameAnimation(DelayFrameCollection keyFrames)
        {
            KeyFrames = keyFrames;
            Duration = keyFrames.Duration;
        }

        protected override Freezable CreateInstanceCore()
            => new DelayFrameAnimation(KeyFrames);

        protected override object GetCurrentValueCore(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            if (KeyFrames is null)
            {
                return defaultDestinationValue;
            }

            TimeSpan value = animationClock.CurrentTime.Value - _oversleep;

            while (KeyFrames.Duration < value)
            {
                value -= KeyFrames.Duration;
            }

            DelayFrame prev = null;
            foreach (var frame in KeyFrames)
            {
                //if (frame.Bitmap.IsFaulted)
                //{
                //    throw frame.Bitmap.Exception;
                //}
                //
                //if (!frame.Bitmap.IsCompleted)
                //{
                //    if (prev is null)
                //    {
                //        _oversleep = animationClock.CurrentTime.Value;
                //        return defaultDestinationValue;
                //    }
                //    else
                //    {
                //        _oversleep = animationClock.CurrentTime.Value - prev.StartTime;
                //        return prev.Bitmap.Result;
                //    }
                //}

                prev = frame;

                if (frame.StartTime <= value && value < frame.EndTime)
                    return frame.Bitmap.Result;
            }

            throw new InvalidOperationException();
        }
    }

    internal class DelayFrame
    {
        public DelayBitmapSource Bitmap { get; }
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }

        public DelayFrame(DelayBitmapSource bitmap, TimeSpan start, TimeSpan end)
        {
            Bitmap = bitmap;
            StartTime = start;
            EndTime = end;
        }
    }

    internal class DelayFrameCollection : IEnumerable<DelayFrame>
    {
        private List<DelayFrame> _frames;
        public TimeSpan Duration { get; }
        public int RepeatCount { get; }

        public DelayFrame this[int idx] => _frames[idx];
        public int Count => _frames.Count;

        public DelayFrameCollection(BitmapDecoder decoder, GifFile gifMetadata)
        {
            _frames = new List<DelayFrame>();

            int index = 0;
            var fullSize = GetFullSize(decoder, gifMetadata);
            var duration = TimeSpan.Zero;
            var baseFrameTask = new DelayBitmapSource(default(BitmapSource));
            foreach (var rawFrame in decoder.Frames)
            {
                var frame =
                    MakeDelayFrame(
                        rawFrame,
                        fullSize,
                        duration,
                        baseFrameTask,
                        decoder,
                        gifMetadata, index,
                        out baseFrameTask);

                duration = frame.EndTime;
                index += 1;

                _frames.Add(frame);
            }
            Duration = duration;
            RepeatCount = GetRepeatCountFromMetadata(decoder, gifMetadata);
        }

        private DelayFrame MakeDelayFrame(
            BitmapFrame rawFrame,
            Int32Size fullSize,
            TimeSpan duration,
            DelayBitmapSource baseFrameTask,
            BitmapDecoder decoder,
            GifFile gifMetadata,
            int index,
            out DelayBitmapSource nextBaseFrameTask)
        {
            var metadata = GetFrameMetadata(decoder, gifMetadata, index);

            var frame = index == 0 ?
                            new DelayBitmapSource(MakeFrame(fullSize, rawFrame, metadata, baseFrameTask.Result)) :
                            new DelayBitmapSource(() => MakeFrame(fullSize, rawFrame, metadata, baseFrameTask.Result));

            switch (metadata.DisposalMethod)
            {
                case FrameDisposalMethod.None:
                case FrameDisposalMethod.DoNotDispose:
                    nextBaseFrameTask = frame;
                    break;
                case FrameDisposalMethod.RestoreBackground:
                    if (IsFullFrame(metadata, fullSize))
                    {
                        nextBaseFrameTask = new DelayBitmapSource(default(BitmapSource));
                    }
                    else
                    {
                        nextBaseFrameTask = new DelayBitmapSource(() => ClearArea(frame.Result, metadata));
                    }
                    break;
                case FrameDisposalMethod.RestorePrevious:
                    // Reuse same base frame
                    nextBaseFrameTask = baseFrameTask;
                    break;

                default:
                    throw new InvalidOperationException();
            }

            return new DelayFrame(frame, duration, duration + metadata.Delay);
        }

        public static bool TryCreate(BitmapSource source, IUriContext context, out DelayFrameCollection collection)
        {
            var decoder = GetDecoder(source, context, out var gifMetadata);

            if (decoder is null || decoder.Frames.Count <= 1)
            {
                collection = null;
                return false;
            }

            collection = new DelayFrameCollection(decoder, gifMetadata);
            return true;
        }

        private static BitmapDecoder GetDecoder(BitmapSource image, IUriContext context, out GifFile gifFile)
        {
            gifFile = null;
            BitmapDecoder decoder = null;
            Stream stream = null;
            Uri uri = null;
            BitmapCreateOptions createOptions = BitmapCreateOptions.None;

            var bmp = image as BitmapImage;
            if (bmp != null)
            {
                createOptions = bmp.CreateOptions;
                if (bmp.StreamSource != null)
                {
                    stream = bmp.StreamSource;
                }
                else if (bmp.UriSource != null)
                {
                    uri = bmp.UriSource;
                    if (!uri.IsAbsoluteUri)
                    {
                        var baseUri = bmp.BaseUri ?? context?.BaseUri;
                        if (baseUri != null)
                            uri = new Uri(baseUri, uri);
                    }
                }
            }
            else
            {
                BitmapFrame frame = image as BitmapFrame;
                if (frame != null)
                {
                    decoder = frame.Decoder;
                    Uri.TryCreate(frame.BaseUri, frame.ToString(), out uri);
                }
            }

            if (decoder == null)
            {
                if (stream != null)
                {
                    stream.Position = 0;
                    decoder = BitmapDecoder.Create(stream, createOptions, BitmapCacheOption.OnLoad);
                }
                else if (uri != null && uri.IsAbsoluteUri)
                {
                    decoder = BitmapDecoder.Create(uri, createOptions, BitmapCacheOption.OnLoad);
                }
            }

            if (decoder is GifBitmapDecoder && !CanReadNativeMetadata(decoder))
            {
                if (stream != null)
                {
                    stream.Position = 0;
                    gifFile = GifFile.ReadGifFile(stream, true);
                }
                else if (uri != null)
                {
                    gifFile = DecodeGifFile(uri);
                }
                else
                {
                    throw new InvalidOperationException("Can't get URI or Stream from the source. AnimatedSource should be either a BitmapImage, or a BitmapFrame constructed from a URI.");
                }
            }

            if (decoder == null)
            {
                throw new InvalidOperationException("Can't get a decoder from the source. AnimatedSource should be either a BitmapImage or a BitmapFrame.");
            }
            return decoder;
        }

        private static bool CanReadNativeMetadata(BitmapDecoder decoder)
        {
            try
            {
                var m = decoder.Metadata;
                return m != null;
            }
            catch
            {
                return false;
            }
        }

        private static GifFile DecodeGifFile(Uri uri)
        {
            Stream stream = null;
            if (uri.Scheme == PackUriHelper.UriSchemePack)
            {
                StreamResourceInfo sri;
                if (uri.Authority == "siteoforigin:,,,")
                    sri = Application.GetRemoteStream(uri);
                else
                    sri = Application.GetResourceStream(uri);

                if (sri != null)
                    stream = sri.Stream;
            }
            else
            {
                WebClient wc = new WebClient();
                stream = wc.OpenRead(uri);
            }
            if (stream != null)
            {
                using (stream)
                {
                    return GifFile.ReadGifFile(stream, true);
                }
            }
            return null;
        }

        private static Int32Size GetFullSize(BitmapDecoder decoder, GifFile gifMetadata)
        {
            if (gifMetadata != null)
            {
                var lsd = gifMetadata.Header.LogicalScreenDescriptor;
                return new Int32Size(lsd.Width, lsd.Height);
            }
            int width = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Width", 0);
            int height = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Height", 0);
            return new Int32Size(width, height);
        }

        private static FrameMetadata GetFrameMetadata(BitmapDecoder decoder, GifFile gifMetadata, int frameIndex)
        {
            if (gifMetadata != null && gifMetadata.Frames.Count > frameIndex)
            {
                return GetFrameMetadata(gifMetadata.Frames[frameIndex]);
            }

            return GetFrameMetadata(decoder.Frames[frameIndex]);
        }

        private static FrameMetadata GetFrameMetadata(BitmapFrame frame)
        {
            var metadata = (BitmapMetadata)frame.Metadata;
            var delay = TimeSpan.FromMilliseconds(100);
            var metadataDelay = metadata.GetQueryOrDefault("/grctlext/Delay", 10);
            if (metadataDelay != 0)
                delay = TimeSpan.FromMilliseconds(metadataDelay * 10);
            var disposalMethod = (FrameDisposalMethod)metadata.GetQueryOrDefault("/grctlext/Disposal", 0);
            var frameMetadata = new FrameMetadata
            {
                Left = metadata.GetQueryOrDefault("/imgdesc/Left", 0),
                Top = metadata.GetQueryOrDefault("/imgdesc/Top", 0),
                Width = metadata.GetQueryOrDefault("/imgdesc/Width", frame.PixelWidth),
                Height = metadata.GetQueryOrDefault("/imgdesc/Height", frame.PixelHeight),
                Delay = delay,
                DisposalMethod = disposalMethod
            };
            return frameMetadata;
        }

        private static FrameMetadata GetFrameMetadata(GifFrame gifMetadata)
        {
            var d = gifMetadata.Descriptor;
            var frameMetadata = new FrameMetadata
            {
                Left = d.Left,
                Top = d.Top,
                Width = d.Width,
                Height = d.Height,
                Delay = TimeSpan.FromMilliseconds(100),
                DisposalMethod = FrameDisposalMethod.None
            };

            var gce = gifMetadata.Extensions.OfType<GifGraphicControlExtension>().FirstOrDefault();
            if (gce != null)
            {
                if (gce.Delay != 0)
                    frameMetadata.Delay = TimeSpan.FromMilliseconds(gce.Delay);
                frameMetadata.DisposalMethod = (FrameDisposalMethod)gce.DisposalMethod;
            }
            return frameMetadata;
        }

        private static BitmapSource MakeFrame(
            Int32Size fullSize,
            BitmapSource rawFrame, FrameMetadata metadata,
            BitmapSource baseFrame)
        {
            if (baseFrame == null && IsFullFrame(metadata, fullSize))
            {
                // No previous image to combine with, and same size as the full image
                // Just return the frame as is
                return rawFrame;
            }

            DrawingVisual visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                if (baseFrame != null)
                {
                    var fullRect = new Rect(0, 0, fullSize.Width, fullSize.Height);
                    context.DrawImage(baseFrame, fullRect);
                }

                var rect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                context.DrawImage(rawFrame, rect);
            }
            var bitmap = new RenderTargetBitmap(
                fullSize.Width, fullSize.Height,
                96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var result = new WriteableBitmap(bitmap);

            if (result.CanFreeze && !result.IsFrozen)
                result.Freeze();
            return result;
        }

        private static bool IsFullFrame(FrameMetadata metadata, Int32Size fullSize)
        {
            return metadata.Left == 0
                   && metadata.Top == 0
                   && metadata.Width == fullSize.Width
                   && metadata.Height == fullSize.Height;
        }

        private static BitmapSource ClearArea(BitmapSource frame, FrameMetadata metadata)
        {
            DrawingVisual visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var fullRect = new Rect(0, 0, frame.PixelWidth, frame.PixelHeight);
                var clearRect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                var clip = Geometry.Combine(
                    new RectangleGeometry(fullRect),
                    new RectangleGeometry(clearRect),
                    GeometryCombineMode.Exclude,
                    null);
                context.PushClip(clip);
                context.DrawImage(frame, fullRect);
            }

            var bitmap = new RenderTargetBitmap(
                    frame.PixelWidth, frame.PixelHeight,
                    frame.DpiX, frame.DpiY,
                    PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var result = new WriteableBitmap(bitmap);

            if (result.CanFreeze && !result.IsFrozen)
                result.Freeze();
            return result;
        }

        private static int GetRepeatCountFromMetadata(BitmapDecoder decoder, GifFile gifMetadata)
        {
            if (gifMetadata != null)
            {
                return gifMetadata.RepeatCount;
            }
            else
            {
                var ext = GetApplicationExtension(decoder, "NETSCAPE2.0");
                if (ext != null)
                {
                    byte[] bytes = ext.GetQueryOrNull<byte[]>("/Data");
                    if (bytes != null && bytes.Length >= 4)
                        return BitConverter.ToUInt16(bytes, 2);
                }
                return 1;
            }
        }

        private static BitmapMetadata GetApplicationExtension(BitmapDecoder decoder, string application)
        {
            int count = 0;
            string query = "/appext";
            BitmapMetadata extension = decoder.Metadata.GetQueryOrNull<BitmapMetadata>(query);
            while (extension != null)
            {
                byte[] bytes = extension.GetQueryOrNull<byte[]>("/Application");
                if (bytes != null)
                {
                    string extApplication = Encoding.ASCII.GetString(bytes);
                    if (extApplication == application)
                        return extension;
                }
                query = string.Format("/[{0}]appext", ++count);
                extension = decoder.Metadata.GetQueryOrNull<BitmapMetadata>(query);
            }
            return null;
        }

        public IEnumerator<DelayFrame> GetEnumerator() => _frames.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Int32Size
        {
            public Int32Size(int width, int height) : this()
            {
                Width = width;
                Height = height;
            }

            public int Width { get; private set; }
            public int Height { get; private set; }
        }

        private class FrameMetadata
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public TimeSpan Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
        }

        private enum FrameDisposalMethod
        {
            None = 0,
            DoNotDispose = 1,
            RestoreBackground = 2,
            RestorePrevious = 3
        }

        internal class DelayBitmapSource
        {
            private bool _solved;
            private BitmapSource _value;
            private Func<BitmapSource> _func;

            public BitmapSource Result
            {
                get
                {
                    if (_solved)
                        return _value;
                    else
                    {
                        _solved = true;
                        return _value = _func();
                    }
                }
            }

            public DelayBitmapSource(BitmapSource value)
            {
                _solved = true;
                _value = value;
            }

            public DelayBitmapSource(Func<BitmapSource> func)
            {
                var dispatcher = Dispatcher.CurrentDispatcher;

                _func = () => dispatcher.Invoke(func);
            }
        }
    }


}
