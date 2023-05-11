using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using WpfAnimatedGif.Decoding;

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
                var imageTask = frame.Bitmap.Task;

                if (imageTask.IsFaulted)
                {
                    throw imageTask.Exception;
                }

                if (!imageTask.IsCompleted)
                {
                    if (prev is null)
                    {
                        _oversleep = animationClock.CurrentTime.Value;
                        return defaultDestinationValue;
                    }
                    else
                    {
                        _oversleep = animationClock.CurrentTime.Value - prev.StartTime;
                        return prev.Bitmap.Task.Result;
                    }
                }

                prev = frame;

                if (frame.StartTime <= value && value < frame.EndTime)
                    return frame.Bitmap.Task.Result;
            }

            return prev.Bitmap.Task.Result;
        }
    }

    internal class DelayFrameCollection : IEnumerable<DelayFrame>
    {
        private List<DelayFrame> _frames;
        public TimeSpan Duration { get; }
        public int RepeatCount { get; }

        public DelayFrame this[int idx] => _frames[idx];
        public int Count => _frames.Count;

        public DelayFrameCollection(GifFile gifMetadata)
        {
            _frames = new List<DelayFrame>();

            int index = 0;
            var fullSize = GetFullSize(gifMetadata);
            var duration = TimeSpan.Zero;

            var baseFrameTask = new DelayBitmapSource(new WriteableBitmap(fullSize.Width, fullSize.Height, 96, 96, PixelFormats.Pbgra32, null));
            foreach (var rawFrame in gifMetadata.Frames)
            {
                var frame =
                    MakeDelayFrame(
                        rawFrame,
                        fullSize,
                        duration,
                        baseFrameTask,
                        gifMetadata, index,
                        out baseFrameTask);

                duration = frame.EndTime;
                index += 1;

                _frames.Add(frame);
            }
            Duration = duration;
            RepeatCount = gifMetadata.RepeatCount;
        }

        private DelayFrame MakeDelayFrame(
            GifFrame rawFrame,
            Int32Size fullSize,
            TimeSpan duration,
            DelayBitmapSource baseFrameTask,
            GifFile gifMetadata,
            int index,
            out DelayBitmapSource nextBaseFrameTask)
        {
            var frameMeta = GetFrameMetadata(gifMetadata.Frames[index]);

            var frame = index == 0 ?
                            DelayBitmapSource.Create(gifMetadata, rawFrame, frameMeta, baseFrameTask.Task) :
                            DelayBitmapSource.CreateAsync(gifMetadata, rawFrame, frameMeta, baseFrameTask.Task);

            switch (frameMeta.DisposalMethod)
            {
                case FrameDisposalMethod.None:
                case FrameDisposalMethod.DoNotDispose:
                    nextBaseFrameTask = frame;
                    break;
                case FrameDisposalMethod.RestoreBackground:
                    if (IsFullFrame(frameMeta, fullSize))
                    {
                        nextBaseFrameTask = new DelayBitmapSource(new WriteableBitmap(fullSize.Width, fullSize.Height, 96, 96, PixelFormats.Pbgra32, null));
                    }
                    else
                    {
                        nextBaseFrameTask = DelayBitmapSource.CreateClearAsync(frame.Task, rawFrame);
                    }
                    break;
                case FrameDisposalMethod.RestorePrevious:
                    // Reuse same base frame
                    nextBaseFrameTask = baseFrameTask;
                    break;

                default:
                    throw new InvalidOperationException();
            }

            return new DelayFrame(frame, duration, duration + frameMeta.Delay);
        }

        public static bool TryCreate(BitmapSource source, IUriContext context, out DelayFrameCollection collection)
        {
            var decoder = GetDecoder(source, context, out var gifMetadata);

            if (decoder is null || decoder.Frames.Count <= 1)
            {
                collection = null;
                return false;
            }

            collection = new DelayFrameCollection(gifMetadata);
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

            if (decoder is GifBitmapDecoder)
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
                    return GifFile.ReadGifFile(stream, false);
                }
            }
            return null;
        }

        private static Int32Size GetFullSize(GifFile gifMetadata)
        {
            var lsd = gifMetadata.Header.LogicalScreenDescriptor;
            return new Int32Size(lsd.Width, lsd.Height);
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

                frameMetadata.HasTransparency = gce.HasTransparency;
                frameMetadata.TransparencyIndex = gce.TransparencyIndex;
            }
            return frameMetadata;
        }

        private static bool IsFullFrame(FrameMetadata metadata, Int32Size fullSize)
        {
            return metadata.Left == 0
                   && metadata.Top == 0
                   && metadata.Width == fullSize.Width
                   && metadata.Height == fullSize.Height;
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
    }

    internal class FrameMetadata
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public TimeSpan Delay { get; set; }
        public FrameDisposalMethod DisposalMethod { get; set; }
        public bool HasTransparency { get; set; }
        public int TransparencyIndex { get; set; }
    }

    internal enum FrameDisposalMethod
    {
        None = 0,
        DoNotDispose = 1,
        RestoreBackground = 2,
        RestorePrevious = 3
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

    internal class DelayBitmapSource
    {
        public Task<WriteableBitmap> Task { get; }

        public DelayBitmapSource(WriteableBitmap value)
        {
            Task = System.Threading.Tasks.Task.FromResult(value);
        }

        public DelayBitmapSource(Task<WriteableBitmap> task)
        {
            Task = task;
        }

        public static DelayBitmapSource CreateClearAsync(Task<WriteableBitmap> baseImageTask, GifFrame frame)
        {
            var task = System.Threading.Tasks.Task.Run(async () =>
            {
                var dispatcher = Application.Current.Dispatcher;
                var baseImage = await baseImageTask;

                return dispatcher.Invoke(() =>
                {
                    var bitmap = new WriteableBitmap(baseImage);

                    var rect = new Int32Rect(
                        frame.Descriptor.Left, frame.Descriptor.Top,
                        frame.Descriptor.Width, frame.Descriptor.Height);

                    bitmap.WritePixels(rect, new byte[4 * rect.Width * rect.Height], 4 * rect.Width, 0);

                    if (bitmap.CanFreeze)
                        bitmap.Freeze();

                    return bitmap;
                });
            });

            return new DelayBitmapSource(task);
        }

        public static DelayBitmapSource CreateAsync(GifFile metadata, GifFrame frame, FrameMetadata framemeta, Task<WriteableBitmap> baseImageTask)
        {
            var data = new DelayBitmapData(frame);
            var task = System.Threading.Tasks.Task.Run(async () =>
            {
                var dispatcher = Application.Current.Dispatcher;

                var colormap = data.Frame.Descriptor.HasLocalColorTable ?
                                    data.Frame.LocalColorTable :
                                    metadata.GlobalColorTable;

                var indics = data.Decompress();

                var baseImage = await baseImageTask;

                return dispatcher.Invoke(() =>
                {
                    var bitmap = new WriteableBitmap(baseImage);

                    var rect = new Int32Rect(
                        frame.Descriptor.Left, frame.Descriptor.Top,
                        frame.Descriptor.Width, frame.Descriptor.Height);

                    Draw(
                        bitmap,
                        rect,
                        indics,
                        colormap,
                        framemeta.HasTransparency ? framemeta.TransparencyIndex : -1);

                    if (bitmap.CanFreeze)
                        bitmap.Freeze();

                    return bitmap;
                });
            });

            return new DelayBitmapSource(task);
        }

        public static DelayBitmapSource Create(GifFile metadata, GifFrame frame, FrameMetadata framemeta, Task<WriteableBitmap> backgroundTask)
        {
            var data = new DelayBitmapData(frame);
            var dispatcher = Application.Current.Dispatcher;

            var colormap = data.Frame.Descriptor.HasLocalColorTable ?
                                data.Frame.LocalColorTable :
                                metadata.GlobalColorTable;

            var indics = data.Decompress();

            var bitmap = new WriteableBitmap(backgroundTask.Result);
            var rect = new Int32Rect(
                frame.Descriptor.Left, frame.Descriptor.Top,
                frame.Descriptor.Width, frame.Descriptor.Height);

            Draw(
                bitmap,
                rect,
                indics,
                colormap,
                framemeta.HasTransparency ? framemeta.TransparencyIndex : -1);

            if (bitmap.CanFreeze)
                bitmap.Freeze();

            return new DelayBitmapSource(bitmap);
        }

        private static void Draw(WriteableBitmap bitmap, Int32Rect rect, byte[] indics, GifColor[] colormap, int transparencyIdx)
        {
            var colors = new byte[indics.Length * 4];

            bitmap.CopyPixels(rect, colors, 4 * rect.Width, 0);

            for (var i = 0; i < indics.Length; ++i)
            {
                var idx = indics[i];

                if (idx == transparencyIdx)
                    continue;

                var color = colormap[idx];
                colors[4 * i + 0] = color.B;
                colors[4 * i + 1] = color.G;
                colors[4 * i + 2] = color.R;
                colors[4 * i + 3] = 255;
            }

            bitmap.WritePixels(rect, colors, 4 * rect.Width, 0);
        }
    }

    internal class DelayBitmapData
    {
        private static readonly int MaxStackSize = 4096;
        private static readonly int MaxBits = 4097;

        public GifFrame Frame { get; }
        public GifImageData Data { get; }

        public DelayBitmapData(GifFrame frame)
        {
            Frame = frame;
            Data = frame.ImageData;
        }

        public byte[] Decompress()
        {
            var totalPixels = Frame.Descriptor.Width * Frame.Descriptor.Height;

            // Initialize GIF data stream decoder.
            var dataSize = Data.LzwMinimumCodeSize;
            var clear = 1 << dataSize;
            var endOfInformation = clear + 1;
            var available = clear + 2;
            var oldCode = -1;
            var codeSize = dataSize + 1;
            var codeMask = (1 << codeSize) - 1;

            var prefixBuf = new short[MaxStackSize];
            var suffixBuf = new byte[MaxStackSize];
            var pixelStack = new byte[MaxStackSize];
            var indics = new byte[totalPixels];

            for (var code = 0; code < clear; code++)
            {
                suffixBuf[code] = (byte)code;
            }

            // Decode GIF pixel stream.
            int bits, first, top, pixelIndex;
            var datum = bits = first = top = pixelIndex = 0;

            var blockSize = Data.CompressedData.Length;
            var tempBuf = Data.CompressedData;

            var blockPos = 0;

            while (blockPos < blockSize)
            {
                datum += tempBuf[blockPos] << bits;
                blockPos++;

                bits += 8;

                while (bits >= codeSize)
                {
                    // Get the next code.
                    var code = datum & codeMask;
                    datum >>= codeSize;
                    bits -= codeSize;

                    // Interpret the code
                    if (code == clear)
                    {
                        // Reset decoder.
                        codeSize = dataSize + 1;
                        codeMask = (1 << codeSize) - 1;
                        available = clear + 2;
                        oldCode = -1;
                        continue;
                    }

                    // Check for explicit end-of-stream
                    if (code == endOfInformation)
                        return indics;

                    if (oldCode == -1)
                    {
                        indics[pixelIndex++] = suffixBuf[code];
                        oldCode = code;
                        first = code;
                        continue;
                    }

                    var inCode = code;
                    if (code >= available)
                    {
                        pixelStack[top++] = (byte)first;
                        code = oldCode;

                        if (top == 4097)
                            ThrowException();
                    }

                    while (code >= clear)
                    {
                        if (code >= MaxBits || code == prefixBuf[code])
                            ThrowException();

                        pixelStack[top++] = suffixBuf[code];
                        code = prefixBuf[code];

                        if (top == MaxBits)
                            ThrowException();
                    }

                    first = suffixBuf[code];
                    pixelStack[top++] = (byte)first;

                    // Add new code to the dictionary
                    if (available < MaxStackSize)
                    {
                        prefixBuf[available] = (short)oldCode;
                        suffixBuf[available] = (byte)first;
                        available++;

                        if (((available & codeMask) == 0) && (available < MaxStackSize))
                        {
                            codeSize++;
                            codeMask += available;
                        }
                    }

                    oldCode = inCode;

                    // Drain the pixel stack.
                    do
                    {
                        indics[pixelIndex++] = pixelStack[--top];
                    } while (top > 0);
                }
            }

            while (pixelIndex < totalPixels)
                indics[pixelIndex++] = 0; // clear missing pixels

            return indics;

            void ThrowException() => throw new InvalidDataException();
        }
    }
}
