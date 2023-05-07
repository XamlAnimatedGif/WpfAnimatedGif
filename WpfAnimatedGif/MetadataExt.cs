using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace WpfAnimatedGif
{
    internal static class MetadataExt
    {
        public static T GetQueryOrDefault<T>(this BitmapMetadata metadata, string query, T defaultValue)
        {
            if (metadata.ContainsQuery(query))
                return (T)Convert.ChangeType(metadata.GetQuery(query), typeof(T));
            return defaultValue;
        }

        public static T GetQueryOrNull<T>(this BitmapMetadata metadata, string query)
            where T : class
        {
            if (metadata.ContainsQuery(query))
                return metadata.GetQuery(query) as T;
            return null;
        }
    }
}
