using System;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using WpfAnimatedGif.Decoding;

namespace WpfAnimatedGif
{
    /// <summary>
    /// Provides attached properties that display animated GIFs in a standard Image control.
    /// </summary>
    public static class ImageBehavior
    {
        #region Public attached properties and events

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
              new PropertyMetadata(
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
              new PropertyMetadata(
                  default(RepeatBehavior),
                  AnimationPropertyChanged));

        /// <summary>
        /// Gets the value of the <c>AnimationSpeedRatio</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The speed ratio for the animated image.</returns>
        public static double? GetAnimationSpeedRatio(DependencyObject obj)
        {
            return (double?)obj.GetValue(AnimationSpeedRatioProperty);
        }

        /// <summary>
        /// Sets the value of the <c>AnimationSpeedRatio</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">The speed ratio of the animated image.</param>
        /// <remarks>The <c>AnimationSpeedRatio</c> and <c>AnimationDuration</c> properties are mutually exclusive, only one can be set at a time.</remarks>
        public static void SetAnimationSpeedRatio(DependencyObject obj, double? value)
        {
            obj.SetValue(AnimationSpeedRatioProperty, value);
        }

        /// <summary>
        /// Identifies the <c>AnimationSpeedRatio</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimationSpeedRatioProperty =
            DependencyProperty.RegisterAttached(
                "AnimationSpeedRatio",
                typeof(double?),
                typeof(ImageBehavior),
                new PropertyMetadata(
                    null,
                    AnimationPropertyChanged));

        /// <summary>
        /// Gets the value of the <c>AnimationDuration</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The duration for the animated image.</returns>
        public static Duration? GetAnimationDuration(DependencyObject obj)
        {
            return (Duration?)obj.GetValue(AnimationDurationProperty);
        }

        /// <summary>
        /// Sets the value of the <c>AnimationDuration</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">The duration of the animated image.</param>
        /// <remarks>The <c>AnimationSpeedRatio</c> and <c>AnimationDuration</c> properties are mutually exclusive, only one can be set at a time.</remarks>
        public static void SetAnimationDuration(DependencyObject obj, Duration? value)
        {
            obj.SetValue(AnimationDurationProperty, value);
        }

        /// <summary>
        /// Identifies the <c>AnimationDuration</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.RegisterAttached(
                "AnimationDuration",
                typeof(Duration?),
                typeof(ImageBehavior),
                new PropertyMetadata(
                    null,
                    AnimationPropertyChanged));

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
        /// Gets the value of the <c>AutoStart</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>true if the animation should start immediately when loaded. Otherwise, false.</returns>
        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static bool GetAutoStart(Image obj)
        {
            return (bool)obj.GetValue(AutoStartProperty);
        }

        /// <summary>
        /// Sets the value of the <c>AutoStart</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <param name="value">true if the animation should start immediately when loaded. Otherwise, false.</param>
        /// <remarks>The default value is true.</remarks>
        public static void SetAutoStart(Image obj, bool value)
        {
            obj.SetValue(AutoStartProperty, value);
        }

        /// <summary>
        /// Identifies the <c>AutoStart</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AutoStartProperty =
            DependencyProperty.RegisterAttached("AutoStart", typeof(bool), typeof(ImageBehavior), new PropertyMetadata(true));

        /// <summary>
        /// Gets the animation controller for the specified <c>Image</c> control.
        /// </summary>
        /// <param name="imageControl"></param>
        /// <returns></returns>
        public static ImageAnimationController GetAnimationController(Image imageControl)
        {
            return (ImageAnimationController)imageControl.GetValue(AnimationControllerPropertyKey.DependencyProperty);
        }

        private static void SetAnimationController(DependencyObject obj, ImageAnimationController value)
        {
            obj.SetValue(AnimationControllerPropertyKey, value);
        }

        private static readonly DependencyPropertyKey AnimationControllerPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("AnimationController", typeof(ImageAnimationController), typeof(ImageBehavior), new PropertyMetadata(null));

        /// <summary>
        /// Gets the value of the <c>IsAnimationLoaded</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element from which to read the property value.</param>
        /// <returns>true if the animation is loaded. Otherwise, false.</returns>
        public static bool GetIsAnimationLoaded(Image image)
        {
            return (bool)image.GetValue(IsAnimationLoadedProperty);
        }

        private static void SetIsAnimationLoaded(Image image, bool value)
        {
            image.SetValue(IsAnimationLoadedPropertyKey, value);
        }

        private static readonly DependencyPropertyKey IsAnimationLoadedPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("IsAnimationLoaded", typeof(bool), typeof(ImageBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <c>IsAnimationLoaded</c> attached property.
        /// </summary>
        public static readonly DependencyProperty IsAnimationLoadedProperty =
            IsAnimationLoadedPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <c>AnimationLoaded</c> attached event.
        /// </summary>
        public static readonly RoutedEvent AnimationLoadedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationLoaded",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ImageBehavior));

        /// <summary>
        /// Adds a handler for the AnimationLoaded attached event.
        /// </summary>
        /// <param name="image">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be added.</param>
        public static void AddAnimationLoadedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (handler == null)
                throw new ArgumentNullException("handler");
            image.AddHandler(AnimationLoadedEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the AnimationLoaded attached event.
        /// </summary>
        /// <param name="image">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be removed.</param>
        public static void RemoveAnimationLoadedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (handler == null)
                throw new ArgumentNullException("handler");
            image.RemoveHandler(AnimationLoadedEvent, handler);
        }

        /// <summary>
        /// Identifies the <c>AnimationCompleted</c> attached event.
        /// </summary>
        public static readonly RoutedEvent AnimationCompletedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationCompleted",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ImageBehavior));

        /// <summary>
        /// Adds a handler for the AnimationCompleted attached event.
        /// </summary>
        /// <param name="d">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be added.</param>
        public static void AddAnimationCompletedHandler(Image d, RoutedEventHandler handler)
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
        public static void RemoveAnimationCompletedHandler(Image d, RoutedEventHandler handler)
        {
            var element = d as UIElement;
            if (element == null)
                return;
            element.RemoveHandler(AnimationCompletedEvent, handler);
        }

        #endregion

        private static void AnimatedSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            var oldValue = e.OldValue as ImageSource;
            var newValue = e.NewValue as ImageSource;
            if (ReferenceEquals(oldValue, newValue))
            {
                if (imageControl.IsLoaded)
                {
                    var isAnimLoaded = GetIsAnimationLoaded(imageControl);
                    if (!isAnimLoaded)
                        InitAnimationOrImage(imageControl);
                }
                return;
            }
            if (oldValue != null)
            {
                imageControl.Loaded -= ImageControlLoaded;
                imageControl.Unloaded -= ImageControlUnloaded;
                imageControl.IsVisibleChanged -= VisibilityChanged;

                AnimationCache.RemoveControlForSource(oldValue, imageControl);
                var controller = GetAnimationController(imageControl);
                if (controller != null)
                    controller.Dispose();
                imageControl.Source = null;
            }
            if (newValue != null)
            {
                imageControl.Loaded += ImageControlLoaded;
                imageControl.Unloaded += ImageControlUnloaded;
                imageControl.IsVisibleChanged += VisibilityChanged;

                if (imageControl.IsLoaded)
                    InitAnimationOrImage(imageControl);
            }
        }

        private static void VisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Image img && img.IsLoaded)
            {
                var controller = GetAnimationController(img);
                if (controller != null)
                {
                    bool isVisible = (bool)e.NewValue;
                    controller.SetSuspended(!isVisible);
                }
            }
        }

        private static void ImageControlLoaded(object sender, RoutedEventArgs e)
        {
            Image imageControl = sender as Image;
            if (imageControl == null)
                return;
            InitAnimationOrImage(imageControl);
        }

        static void ImageControlUnloaded(object sender, RoutedEventArgs e)
        {
            Image imageControl = sender as Image;
            if (imageControl == null)
                return;
            var source = GetAnimatedSource(imageControl);
            if (source != null)
                AnimationCache.RemoveControlForSource(source, imageControl);
            var controller = GetAnimationController(imageControl);
            if (controller != null)
                controller.Dispose();
        }

        private static void AnimationPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            ImageSource source = GetAnimatedSource(imageControl);
            if (source != null)
            {
                if (imageControl.IsLoaded)
                    InitAnimationOrImage(imageControl);
            }
        }

        private static void AnimateInDesignModeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            bool newValue = (bool)e.NewValue;

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
            var controller = GetAnimationController(imageControl);
            if (controller != null)
                controller.Dispose();
            SetAnimationController(imageControl, null);
            SetIsAnimationLoaded(imageControl, false);

            var rawSource = GetAnimatedSource(imageControl);
            BitmapSource source = rawSource as BitmapSource;
            if (source == null && rawSource != null)
            {
                imageControl.Source = rawSource;
                return;
            }

            bool isInDesignMode = DesignerProperties.GetIsInDesignMode(imageControl);
            bool animateInDesignMode = GetAnimateInDesignMode(imageControl);
            bool shouldAnimate = !isInDesignMode || animateInDesignMode;

            // For a BitmapImage with a relative UriSource, the loading is deferred until
            // BaseUri is set. This method will be called again when BaseUri is set.
            bool isLoadingDeferred = IsLoadingDeferred(source, imageControl);

            if (source != null && shouldAnimate && !isLoadingDeferred)
            {
                // Case of image being downloaded: retry after download is complete
                if (source.IsDownloading)
                {
                    EventHandler handler = null;
                    handler = (sender, args) =>
                    {
                        source.DownloadCompleted -= handler;
                        InitAnimationOrImage(imageControl);
                    };
                    source.DownloadCompleted += handler;
                    imageControl.Source = source;
                    return;
                }

                var animation = GetAnimation(imageControl, source);
                if (animation != null)
                {
                    if (animation.KeyFrames.Count > 0)
                    {
                        // For some reason, it sometimes throws an exception the first time... the second time it works.
                        TryTwice(() => imageControl.Source = animation.FirstFrame.Bitmap.Result);
                    }
                    else
                    {
                        imageControl.Source = source;
                    }

                    controller = new ImageAnimationController(imageControl, animation, GetAutoStart(imageControl));
                    SetAnimationController(imageControl, controller);
                    SetIsAnimationLoaded(imageControl, true);
                    imageControl.RaiseEvent(new RoutedEventArgs(AnimationLoadedEvent, imageControl));
                    return;
                }
            }
            imageControl.Source = source;
            if (source != null)
            {
                SetIsAnimationLoaded(imageControl, true);
                imageControl.RaiseEvent(new RoutedEventArgs(AnimationLoadedEvent, imageControl));
            }
        }

        private static DelayFrameAnimation GetAnimation(Image imageControl, BitmapSource source)
        {
            var cacheEntry = AnimationCache.Get(source);
            if (cacheEntry == null)
            {
                if (DelayFrameCollection.TryCreate(source, imageControl, out var collection))
                {
                    cacheEntry = new AnimationCacheEntry(collection, collection.Duration, collection.RepeatCount);
                    AnimationCache.Add(source, cacheEntry);
                }
            }

            if (cacheEntry != null)
            {
                var animation = new DelayFrameAnimation(cacheEntry.KeyFrames)
                {
                    RepeatBehavior = GetActualRepeatBehavior(imageControl, cacheEntry.RepeatCountFromMetadata),
                    SpeedRatio = GetActualSpeedRatio(imageControl, cacheEntry.Duration)
                };

                AnimationCache.AddControlForSource(source, imageControl);
                return animation;
            }

            return null;
        }

        private static double GetActualSpeedRatio(Image imageControl, Duration naturalDuration)
        {
            var speedRatio = GetAnimationSpeedRatio(imageControl);
            var duration = GetAnimationDuration(imageControl);

            if (speedRatio.HasValue && duration.HasValue)
                throw new InvalidOperationException("Cannot set both AnimationSpeedRatio and AnimationDuration");

            if (speedRatio.HasValue)
                return speedRatio.Value;

            if (duration.HasValue)
            {
                if (!duration.Value.HasTimeSpan)
                    throw new InvalidOperationException("AnimationDuration cannot be Automatic or Forever");
                if (duration.Value.TimeSpan.Ticks <= 0)
                    throw new InvalidOperationException("AnimationDuration must be strictly positive");
                return naturalDuration.TimeSpan.Ticks / (double)duration.Value.TimeSpan.Ticks;
            }

            return 1.0;
        }

        private static void TryTwice(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                action();
            }
        }

        private static bool IsLoadingDeferred(BitmapSource source, Image imageControl)
        {
            var bmp = source as BitmapImage;
            if (bmp == null)
                return false;
            if (bmp.UriSource != null && !bmp.UriSource.IsAbsoluteUri)
                return bmp.BaseUri == null && (imageControl as IUriContext)?.BaseUri == null;
            return false;
        }

        private static RepeatBehavior GetActualRepeatBehavior(Image imageControl, int repeatCountFromMetadata)
        {
            // If specified explicitly, use this value
            var repeatBehavior = GetRepeatBehavior(imageControl);
            if (repeatBehavior != default(RepeatBehavior))
                return repeatBehavior;

            if (repeatCountFromMetadata == 0)
                return RepeatBehavior.Forever;
            return new RepeatBehavior(repeatCountFromMetadata);
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

        // For debug purposes
        //private static void Save(BitmapSource image, string path)
        //{
        //    var encoder = new PngBitmapEncoder();
        //    encoder.Frames.Add(BitmapFrame.Create(image));
        //    using (var stream = File.OpenWrite(path))
        //    {
        //        encoder.Save(stream);
        //    }
        //}
    }
}
