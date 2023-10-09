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

            if (!string.IsNullOrEmpty(content.LastEditDateString) && DateTime.TryParse(content.LastEditDateString, new CultureInfo("en-us"), DateTimeStyles.None, out DateTime date)) return date;

            return content.CreationDate();

        }

        public static DateTime CreationDate(this IContent content)
        {

            if (content == null) return default;

            DateTime date = DateTime.Now;

            if (!string.IsNullOrEmpty(content.CreationDateString) && DateTime.TryParse(content.CreationDateString, new CultureInfo("en-us"), DateTimeStyles.None, out DateTime result)) date = result;

            return date;

        }

    }

}
