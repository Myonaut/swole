using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Swole 
{

    public static class ContentExtensions
    {

        public static bool HasPackage(this IContent content) => content == null ? false : content.PackageInfo.NameIsValid;

        public static DateTime LastEditDate(this IContent content)
        {

            if (content == null) return default;

            if (!string.IsNullOrEmpty(content.LastEditDate) && DateTime.TryParse(content.LastEditDate, new CultureInfo("en-us"), DateTimeStyles.None, out DateTime date)) return date;

            return content.CreationDate();

        }

        public static DateTime CreationDate(this IContent content)
        {

            if (content == null) return default;

            DateTime date = DateTime.Now;

            if (!string.IsNullOrEmpty(content.CreationDate) && DateTime.TryParse(content.CreationDate, new CultureInfo("en-us"), DateTimeStyles.None, out DateTime result)) date = result;

            return date;

        }

    }

}
