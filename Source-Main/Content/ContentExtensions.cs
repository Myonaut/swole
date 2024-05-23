using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Swole 
{

    public static class ContentExtensions
    {

        public static bool HasPackage(this IContent content) => content == null ? false : content.PackageInfo.NameIsValid;

        public static IContent SetOriginPathAndUpdateRelativePath(ref IContent content, string originPath)
        {
            if (content == null) return default;
            content = content.SetOriginPath(originPath).SetRelativePath(GetRelativePathFromLocalPackageFileSystemPath(originPath));
            return content;
        }
        public static IContent SetOriginPath(ref IContent content, string path) 
        {
            if (content == null) return default;
            content = content.SetOriginPath(path);
            return content;
        }
        public static IContent SetRelativePath(ref IContent content, string path)
        {
            if (content == null) return default;
            content = content.SetRelativePath(path);
            return content;
        }

        public static string GetRelativePathFromLocalPackageFileSystemPath(string fileSystemPath)
        {
            if (string.IsNullOrEmpty(fileSystemPath) || !File.Exists(fileSystemPath)) return string.Empty;

            bool IsRoot(DirectoryInfo dir)
            {
                var files = dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                if (files != null)
                {
                    foreach (var file in files) if (file.Name.AsID() == ContentManager.commonFiles_Manifest.AsID()) return true;
                }
                return false;
            }

            string relativePath = string.Empty;
            fileSystemPath = Path.GetDirectoryName(fileSystemPath);
            if (string.IsNullOrEmpty(fileSystemPath)) return string.Empty;
            DirectoryInfo tempDir = new DirectoryInfo(fileSystemPath);
            if (tempDir == null || IsRoot(tempDir)) return relativePath;
            relativePath = tempDir.Name + Path.DirectorySeparatorChar;
            while((tempDir = tempDir.Parent) != null)
            {
                if (tempDir == null || IsRoot(tempDir)) return relativePath;
                relativePath = Path.Combine(tempDir.Name, relativePath);
            } 
            return relativePath;
        }
         
        public static string GetRelativePathFromOriginPath(string rootFolderName, string originPath)
        {
            if (string.IsNullOrEmpty(originPath)) return string.Empty;

            bool IsRoot(DirectoryInfo dir) => dir.Name == rootFolderName;

            string relativePath = string.Empty;
            string temp = originPath;
            originPath = Path.GetDirectoryName(originPath);
            if (string.IsNullOrEmpty(originPath)) return string.Empty;
            string tempDirPath = originPath;
            DirectoryInfo tempDir = new DirectoryInfo(tempDirPath);
            if (IsRoot(tempDir)) return relativePath;
            relativePath = tempDir.Name + Path.DirectorySeparatorChar;
            while (!string.IsNullOrEmpty((tempDirPath = Path.GetDirectoryName(tempDirPath))))
            {
                tempDir = new DirectoryInfo(tempDirPath); 
                if (IsRoot(tempDir)) return relativePath;
                relativePath = Path.Combine(tempDir.Name, relativePath);
            }
            return relativePath;
        }

        private static readonly CultureInfo _cultureInfo = new CultureInfo("en-us");
        public static bool TryConvertDateStringToDateTime(string dateString, out DateTime date)
        {
            date = default;
            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, _cultureInfo, DateTimeStyles.None, out date)) return true;
            return false;
        }
        public static DateTime ConvertDateStringToDateTime(string dateString)
        {
            if (TryConvertDateStringToDateTime(dateString, out DateTime date)) return date;
            return default;
        }
        public static DateTime LastEditDate(this IContent content)
        {

            if (content == null) return default;

            if (TryConvertDateStringToDateTime(content.LastEditDate, out DateTime date)) return date;

            return content.CreationDate();

        }
        public static DateTime CreationDate(this IContent content)
        {

            if (content == null) return default;

            DateTime date = DateTime.Now;

            if (TryConvertDateStringToDateTime(content.CreationDate, out DateTime result)) date = result;

            return date;

        }

    }

}
