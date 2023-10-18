#if (UNITY_EDITOR || UNITY_STANDALONE) && BULKOUT_ENV
#define FOUND_BULKOUT
using UnityEngine;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Swole
{

    public class BulkOutHook : BulkOutIntermediaryHook
    {

        public override string Name => "BulkOut+Unity";

#if FOUND_BULKOUT

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Initialize()
        {
            if (!(typeof(BulkOutHook).IsAssignableFrom(Swole.Engine.GetType()))) 
            {
                activeHook = new BulkOutHook();
                Swole.SetEngine(activeHook); 
            }
        }

        #region File Compression

        /// <summary>
        /// Uses Paid Asset Integration https://assetstore.unity.com/packages/tools/input-management/zip-gzip-multiplatform-native-plugin-39411
        /// </summary>
        public override void DecompressZIP(byte[] data, ref List<FileDescr> fileInfo, ref List<byte[]> fileData)
        {
            if (fileInfo == null) fileInfo = new List<FileDescr>();
            if (fileData == null) fileData = new List<byte[]>();

            if (data == null) return;

            #region Paid Asset Integration https://assetstore.unity.com/packages/tools/input-management/zip-gzip-multiplatform-native-plugin-39411

            lzip.getFileInfo(string.Empty, data);
            for(int a = 0; a < lzip.zipFiles; a++)
            {
                string fileName = lzip.ninfo[a];
                ulong fileSize = lzip.uinfo[a];
                fileInfo.Add(new FileDescr() { fileName = fileName, fileSize = fileSize });
            }

            int progress = 0;
            var fileDataArrays = lzip.entries2Buffers(string.Empty, lzip.ninfo.ToArray(), ref progress, data);
            if (fileDataArrays != null) fileData.AddRange(fileDataArrays);

            #endregion

        }

        /// <summary>
        /// Uses Paid Asset Integration https://assetstore.unity.com/packages/tools/input-management/zip-gzip-multiplatform-native-plugin-39411
        /// </summary>
        public override bool CompressZIP(string sourceDirectoryPath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourceDirectoryPath) || string.IsNullOrEmpty(destinationPath)) return false;

            #region Paid Asset Integration https://assetstore.unity.com/packages/tools/input-management/zip-gzip-multiplatform-native-plugin-39411

            return lzip.compressDir(sourceDirectoryPath, 9, destinationPath) == 1;

            #endregion
        }

        #endregion

        #region Conversions | Swole -> Bulk Out!

        #endregion

        #region Conversions | Bulk Out! -> Swole

        #endregion

#else
        public override bool HookWasSuccessful => false;
#endif

    }

}
