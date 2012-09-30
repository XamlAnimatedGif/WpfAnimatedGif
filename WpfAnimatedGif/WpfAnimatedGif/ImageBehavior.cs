using System;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif.Decoding;

namespace WpfAnimatedGif
{
    /// <summary>
    /// Provides attached properties that display animated GIFs in a standard Image control.
    /// </summary>
    public static class ImageBehavior
    {
        /// <summary>
        /// Gets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The currently displayed animated image.</returns>
        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static ImageSource GetAnimatedSource(Image obj)
        {
            return (ImageSource)obj.GetValue(AnimatedSourceProperty);
        }

        /// <summary>
        /// Sets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">The animated image to display.</param>
        public static void SetAnimatedSource(Image obj, ImageSource value)
        {
            obj.SetValue(AnimatedSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <c>AnimatedSource</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimatedSourceProperty =
            DependencyProperty.RegisterAttached(
              "AnimatedSource",
              typeof(ImageSource),
              typeof(ImageBehavior),
              new UIPropertyMetadata(
                null,
                AnimatedSourceChanged));

        /// <summary>
        /// Gets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The repeat behavior of the animated image.</returns>
        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static RepeatBehavior GetRepeatBehavior(Image obj)
        {
            return (RepeatBehavior)obj.GetValue(RepeatBehaviorProperty);
        }

        /// <summary>
        /// Sets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">The repeat behavior of the animated image.</param>
        public static void SetRepeatBehavior(Image obj, RepeatBehavior value)
        {
            obj.SetValue(RepeatBehaviorProperty, value);
        }

        /// <summary>
        /// Identifies the <c>RepeatBehavior</c> attached property.
        /// </summary>
        public static readonly DependencyProperty RepeatBehaviorProperty =
            DependencyProperty.RegisterAttached(
              "RepeatBehavior",
              typeof(RepeatBehavior),
              typeof(ImageBehavior),
              new UIPropertyMetadata(
                  default(RepeatBehavior),
                  RepeatBehaviorChanged));

        /// <summary>
        /// Gets the value of the <c>AnimateInDesignMode</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>true if GIF animations are shown in design mode; false otherwise.</returns>
        public static bool GetAnimateInDesignMode(DependencyObject obj)
        {
            return (bool)obj.GetValue(AnimateInDesignModeProperty);
        }

        /// <summary>
        /// Sets the value of the <c>AnimateInDesignMode</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">true to show GIF animations in design mode; false otherwise.</param>
        public static void SetAnimateInDesignMode(DependencyObject obj, bool value)
        {
            obj.SetValue(AnimateInDesignModeProperty, value);
        }

        /// <summary>
        /// Identifies the <c>AnimateInDesignMode</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimateInDesignModeProperty =
            DependencyProperty.RegisterAttached(
                "AnimateInDesignMode",
                typeof(bool),
                typeof(ImageBehavior),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits,
                    AnimateInDesignModeChanged));

        /// <summary>
        /// Identifies the <c>AnimationCompleted</c> attached event.
        /// </summary>
        public static readonly RoutedEvent AnimationCompletedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationCompleted",
                RoutingStrategy.Bubble,
                typeof (RoutedEventHandler),
                typeof (ImageBehavior));

        /// <summary>
        /// Adds a handler for the AnimationCompleted attached event.
        /// </summary>
        /// <param name="d">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be added.</param>
        public static void AddAnimationCompletedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            var element = d as UIElement;
            if (element == null)
                return;
            element.AddHandler(AnimationCompletedEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the AnimationCompleted attached event.
        /// </summary>
        /// <param name="d">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be removed.</param>
        public static void RemoveAnimationCompletedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            var element = d as UIElement;
            if (element == null)
                return;
            element.RemoveHandler(AnimationCompletedEvent, handler);
        }

        private static void AnimatedSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            var oldValue = e.OldValue as ImageSource;
            var newValue = e.NewValue as ImageSource;
            if (oldValue != null)
            {
                imageControl.BeginAnimation(Image.SourceProperty, null);
            }
            if (newValue != null)
            {
                imageControl.DoWhenLoaded(InitAnimationOrImage);
            }
        }

        private static void RepeatBehaviorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            ImageSource source = GetAnimatedSource(imageControl);
            if (source != null && imageControl.IsLoaded)
                InitAnimationOrImage(imageControl);
        }

        private static void AnimateInDesignModeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            bool newValue = (bool) e.NewValue;

            ImageSource source = GetAnimatedSource(imageControl);
            if (source != null && imageControl.IsLoaded)
            {
                if (newValue)
                    InitAnimationOrImage(imageControl);
                else
                    imageControl.BeginAnimation(Image.SourceProperty, null);
            }
        }

        private static void InitAnimationOrImage(Image imageControl)
        {
            BitmapSource source = GetAnimatedSource(imageControl) as BitmapSource;
            bool isInDesignMode = DesignerProperties.GetIsInDesignMode(imageControl);
            bool animateInDesignMode = GetAnimateInDesignMode(imageControl);
            bool shouldAnimate = !isInDesignMode || animateInDesignMode;
            if (source != null && shouldAnimate)
            {
                GifFile gifMetadata;
                var decoder = GetDecoder(source, out gifMetadata) as GifBitmapDecoder;
                if (decoder != null && decoder.Frames.Count > 1)
                {
                    int index = 0;
                    var animation = new ObjectAnimationUsingKeyFrames();
                    var totalDuration = TimeSpan.Zero;
                    BitmapSource baseFrame = null;
                    foreach (var rawFrame in decoder.Frames)
                    {
                        var metadata = GetFrameMetadata(decoder, gifMetadata, index);

                        var frame = MakeFrame(source, rawFrame, metadata, baseFrame);

                        var keyFrame = new DiscreteObjectKeyFrame(frame, totalDuration);
                        animation.KeyFrames.Add(keyFrame);
                        
                        totalDuration += metadata.Delay;

                        switch (metadata.DisposalMethod)
                        {
                            case FrameDisposalMethod.None:
                            case FrameDisposalMethod.DoNotDispose:
                                baseFrame = frame;
                                break;
                            case FrameDisposalMethod.RestoreBackground:
                                baseFrame = null;
                                break;
                            case FrameDisposalMethod.RestorePrevious:
                                // Reuse same base frame
                                break;
                        }

                        index++;
                    }
                    animation.Duration = totalDuration;
                    
                    animation.RepeatBehavior = GetActualRepeatBehavior(imageControl, decoder, gifMetadata);

                    if (animation.KeyFrames.Count > 0)
                        imageControl.Source = (ImageSource)animation.KeyFrames[0].Value;
                    else
                        imageControl.Source = decoder.Frames[0];
                    animation.Completed += delegate
                                           {
                                               imageControl.RaiseEvent(
                                                   new RoutedEventArgs(AnimationCompletedEvent, imageControl));
                                           };
                    imageControl.BeginAnimation(Image.SourceProperty, animation);
                    return;
                }
            }
            imageControl.Source = source;
        }

        private static BitmapDecoder GetDecoder(BitmapSource image, out GifFile gifFile)
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
                    if (bmp.BaseUri != null && !uri.IsAbsoluteUri)
                        uri = new Uri(bmp.BaseUri, uri);
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

            if (stream != null)
            {
                stream.Position = 0;
                decoder = BitmapDecoder.Create(stream, createOptions, BitmapCacheOption.OnLoad);
                stream.Position = 0;

                if (!CanReadNativeMetadata(decoder))
                    gifFile = GifFile.ReadGifFile(stream, true);
            }
            else if (uri != null)
            {
                decoder = BitmapDecoder.Create(uri, createOptions, BitmapCacheOption.OnLoad);
                if (!CanReadNativeMetadata(decoder))
                    gifFile = DecodeGifFile(uri);
            }
            return decoder;
        }

        private static bool CanReadNativeMetadata(BitmapDecoder decoder)
        {
            try
            {
#pragma warning disable 168
                var m = decoder.Metadata;
                return true;
#pragma warning restore 168
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
                var sri = Application.GetResourceStream(uri);
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

        private static BitmapSource MakeFrame(
            BitmapSource fullImage,
            BitmapSource rawFrame, FrameMetadata metadata,
            BitmapSource baseFrame)
        {
            if (baseFrame == null
                && rawFrame.PixelWidth == fullImage.PixelWidth
                && rawFrame.PixelHeight == fullImage.PixelHeight)
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
                    var fullRect = new Rect(0, 0, fullImage.PixelWidth, fullImage.PixelHeight);
                    context.DrawImage(baseFrame, fullRect);
                }

                var rect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                context.DrawImage(rawFrame, rect);
            }
            var bitmap = new RenderTargetBitmap(
                fullImage.PixelWidth, fullImage.PixelHeight,
                fullImage.DpiX, fullImage.DpiY,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            if (bitmap.CanFreeze && !bitmap.IsFrozen)
                bitmap.Freeze();
            return bitmap;
        }


        private static RepeatBehavior GetActualRepeatBehavior(Image imageControl, BitmapDecoder decoder, GifFile gifMetadata)
        {
            // If specified explicitly, use this value
            var repeatBehavior = GetRepeatBehavior(imageControl);
            if (repeatBehavior != default(RepeatBehavior))
                return repeatBehavior;

            int repeatCount;
            if (gifMetadata != null)
            {
                repeatCount = gifMetadata.RepeatCount;
            }
            else
            {
                repeatCount = GetRepeatCount(decoder);
            }
            if (repeatCount == 0)
                return RepeatBehavior.Forever;
            return new RepeatBehavior(repeatCount);
        }

        private static int GetRepeatCount(BitmapDecoder decoder)
        {
            var ext = GetApplicationExtension(decoder, "NETSCAPE2.0");
            if (ext != null)
            {
                byte[] bytes = ext.GetQueryOrNull<byte[]>("/Data");
                if (bytes != null && bytes.Length >= 4)
                    return BitConverter.ToUInt16(bytes, 2);
            }
            return 0;
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
            var disposalMethod = (FrameDisposalMethod) metadata.GetQueryOrDefault("/grctlext/Disposal", 0);
            var frameMetadata = new FrameMetadata
                                {
                                    Left = metadata.GetQueryOrDefault("/imgdesc/Left", 0.0),
                                    Top = metadata.GetQueryOrDefault("/imgdesc/Top", 0.0),
                                    Width = metadata.GetQueryOrDefault("/imgdesc/Width", frame.Width),
                                    Height = metadata.GetQueryOrDefault("/imgdesc/Height", frame.Height),
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
                frameMetadata.DisposalMethod = (FrameDisposalMethod) gce.DisposalMethod;
            }
            return frameMetadata;
        }

        private class FrameMetadata
        {
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
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

        private static void DoWhenLoaded<T>(this T element, Action<T> action)
            where T : FrameworkElement
        {
            if (element.IsLoaded)
            {
                action(element);
            }
            else
            {
                RoutedEventHandler handler = null;
                handler = (sender, e) =>
                {
                    element.Loaded -= handler;
                    action(element);
                };
                element.Loaded += handler;
            }
        }

        private static T GetQueryOrDefault<T>(this BitmapMetadata metadata, string query, T defaultValue)
        {
            if (metadata.ContainsQuery(query))
                return (T)Convert.ChangeType(metadata.GetQuery(query), typeof(T));
            return defaultValue;
        }

        private static T GetQueryOrNull<T>(this BitmapMetadata metadata, string query)
            where T : class
        {
            if (metadata.ContainsQuery(query))
                return metadata.GetQuery(query) as T;
            return null;
        }
    }
}
