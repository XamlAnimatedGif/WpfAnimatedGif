using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WpfAnimatedGif
{
    static class AnimationCache
    {
        private struct CacheKey
        {
            private readonly ImageSource _source;

            public CacheKey(ImageSource source)
            {
                _source = source;
            }

            private bool Equals(CacheKey other)
            {
                return ImageEquals(_source, other._source);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((CacheKey)obj);
            }

            public override int GetHashCode()
            {
                return ImageGetHashCode(_source);
            }

            private static int ImageGetHashCode(ImageSource image)
            {
                if (image != null)
                {
                    var uri = GetUri(image);
                    if (uri != null)
                        return uri.GetHashCode();
                }
                return 0;
            }

            private static bool ImageEquals(ImageSource x, ImageSource y)
            {
                if (Equals(x, y))
                    return true;
                if ((x == null) != (y == null))
                    return false;
                // They can't both be null or Equals would have returned true
                // and if any is null, the previous would have detected it
                // ReSharper disable PossibleNullReferenceException
                if (x.GetType() != y.GetType())
                    return false;
                // ReSharper restore PossibleNullReferenceException
                var xUri = GetUri(x);
                var yUri = GetUri(y);
                return xUri != null && xUri == yUri;
            }

            private static Uri GetUri(ImageSource image)
            {
                var bmp = image as BitmapImage;
                if (bmp != null && bmp.UriSource != null)
                {
                    if (bmp.UriSource.IsAbsoluteUri)
                        return bmp.UriSource;
                    if (bmp.BaseUri != null)
                        return new Uri(bmp.BaseUri, bmp.UriSource);
                }
                var frame = image as BitmapFrame;
                if (frame != null)
                {
                    string s = frame.ToString();
                    if (s != frame.GetType().FullName)
                    {
                        Uri fUri;
                        if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out fUri))
                        {
                            if (fUri.IsAbsoluteUri)
                                return fUri;
                            if (frame.BaseUri != null)
                                return new Uri(frame.BaseUri, fUri);
                        }
                    }
                }
                return null;
            }
        }

        private static readonly Dictionary<CacheKey, AnimationCacheEntry> _animationCache = new Dictionary<CacheKey, AnimationCacheEntry>();
        private static readonly Dictionary<CacheKey, HashSet<Image>> _imageControls = new Dictionary<CacheKey, HashSet<Image>>();

        public static void AddControlForSource(ImageSource source, Image imageControl)
        {
            var cacheKey = new CacheKey(source);
            if (!_imageControls.TryGetValue(cacheKey, out var controls))
            {
                _imageControls[cacheKey] = controls = new HashSet<Image>();
            }

            controls.Add(imageControl);
        }

        public static void RemoveControlForSource(ImageSource source, Image imageControl)
        {
            var cacheKey = new CacheKey(source);
            if (_imageControls.TryGetValue(cacheKey, out var controls))
            {
                if (controls.Remove(imageControl))
                {
                    if (controls.Count == 0)
                    {
                        _animationCache.Remove(cacheKey);
                        _imageControls.Remove(cacheKey);
                    }
                }
            }
        }

        public static void Add(ImageSource source, AnimationCacheEntry entry)
        {
            var key = new CacheKey(source);
            _animationCache[key] = entry;
        }

        public static void Remove(ImageSource source)
        {
            var key = new CacheKey(source);
            _animationCache.Remove(key);
        }

        public static AnimationCacheEntry Get(ImageSource source)
        {
            var key = new CacheKey(source);
            _animationCache.TryGetValue(key, out var entry);
            return entry;
        }
    }

    internal class AnimationCacheEntry
    {
        public AnimationCacheEntry(ObjectKeyFrameCollection keyFrames, Duration duration, int repeatCountFromMetadata)
        {
            KeyFrames = keyFrames;
            Duration = duration;
            RepeatCountFromMetadata = repeatCountFromMetadata;
        }

        public ObjectKeyFrameCollection KeyFrames { get; }
        public Duration Duration { get; }
        public int RepeatCountFromMetadata { get; }
    }
}