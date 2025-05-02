#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;

using UnityEngine;

namespace Swole
{ 

    public static class TextureUtils
    {

        public static Texture2D Duplicate(this Texture2D texture) => texture == null ? null : new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount, texture.IsLinear());   

        public static bool IsLinear(this Texture2D tex) => !tex.graphicsFormat.ToString().ToLower().Contains("srgb");

        public static Texture2D InstantiateTexture(this Texture2D original, bool asPNG)
        {
            if (asPNG) return new Texture2D(original.width, original.height, UnityEngine.TextureFormat.ARGB32, original.mipmapCount > 0, original.IsLinear());
            return new Texture2D(original.width, original.height, UnityEngine.TextureFormat.ARGB32, original.mipmapCount > 0, original.IsLinear());
        }

        #region Texture Scaling

        // source: https://gist.github.com/gszauer/7799899
        // Modified from: http://wiki.unity3d.com/index.php/TextureScale#TextureScale.cs
        // Only works on ARGB32, RGB24 and Alpha8 textures that are marked readable 

        private static Color[] texColors;
        private static Color[] newColors;
        private static int w;
        private static float ratioX;
        private static float ratioY;
        private static int w2;

        [Obsolete("Appears to produce incorrect results. Should be revised before use.")]
        public static void Scale(Texture2D tex, int newWidth, int newHeight)
        {
            Scale(tex.GetPixels(), tex.width, tex.height, newWidth, newHeight);

            tex.Reinitialize(newWidth, newHeight);
            tex.SetPixels(newColors);
            tex.Apply();
        }
        [Obsolete("Appears to produce incorrect results. Should be revised before use.")]
        public static Color[] Scale(Color[] originalPixels, int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            texColors = originalPixels;
            newColors = new Color[newWidth * newHeight];
            ratioX = 1.0f / ((float)newWidth / (oldWidth - 1));
            ratioY = 1.0f / ((float)newHeight / (oldHeight - 1));
            w = oldWidth;
            w2 = newWidth;

            BilinearScale(0, newHeight);

            return newColors;
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value,
                              c1.g + (c2.g - c1.g) * value,
                              c1.b + (c2.b - c1.b) * value,
                              c1.a + (c2.a - c1.a) * value);
        }
        private static void BilinearScale(int start, int end)
        {
            for (var y = start; y < end; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * w;
                var y2 = (yFloor + 1) * w;
                var yw = y * w2;

                for (var x = 0; x < w2; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
                                                           ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
                                                           y * ratioY - yFloor);
                }
            }
        }

        public static Color[] GetScaledPixels(this Texture2D tex, int newWidth, int newHeight)
        {
            //return Scale(tex.GetPixels(), tex.width, tex.height, newWidth, newHeight); // Not working properly

            var pixels = new Color[newWidth * newHeight];
            for (var y = 0; y < newHeight; y++)
            {
                for (var x = 0; x < newWidth; x++)
                {
                    // Calculate the fraction of the way across the image
                    // that this pixel positon corresponds to.
                    float xFrac = x * 1.0f / (newWidth - 1);
                    float yFrac = y * 1.0f / (newHeight - 1);

                    // Get the non-integer pixel positions using GetPixelBilinear.
                    pixels[y * newWidth + x] = tex.GetPixelBilinear(xFrac, yFrac);
                }
            }

            return pixels;
        }
        public static void ResizeAndApply(this Texture2D tex, int newWidth, int newHeight)
        {
            var pixels = GetScaledPixels(tex, newWidth, newHeight);

            tex.Reinitialize(newWidth, newHeight);
            tex.SetPixels(pixels);
            tex.Apply();
        }

        #endregion
         
        #region Texture Exporting
        [Serializable]
        public class TextureExportSettings
        {
            public Texture2D texture;
            public bool channelR = true;
            public bool channelG = true;
            public bool channelB = true;
            public bool channelA = true;

            public bool exportChannelsIntoSeperateTextures;

            public ExportFileType exportFileType;

            public string exportPathDirectory;
            public string exportFileName;
        }

        [Serializable]
        public enum ExportFileType
        {
            JPG, PNG
        }
        public static string GetFileExtension(ExportFileType fileType)
        {
            switch (fileType)
            {
                case ExportFileType.JPG: return ".jpg";
                case ExportFileType.PNG: return ".png";
            }

            return ".jpg";
        }

        public static void Export(TextureExportSettings textureExport)
        {
            var tex = textureExport.texture;
            var exportFileType = textureExport.exportFileType;
            string path = textureExport.exportPathDirectory;
            path = Path.Combine(path, (string.IsNullOrWhiteSpace(textureExport.exportFileName) ? textureExport.texture.name : textureExport.exportFileName));   

            if (textureExport.exportChannelsIntoSeperateTextures)
            {
                void ExportChannel(DataStructures.RGBAChannel channel)
                {
                    float GetPixelChannel(Color pixel) => channel == DataStructures.RGBAChannel.R ? pixel.r : (channel == DataStructures.RGBAChannel.G ? pixel.g : (channel == DataStructures.RGBAChannel.B ? pixel.b : pixel.a));

                    var pixels = textureExport.texture.GetPixels(); 
                    tex = textureExport.texture.InstantiateTexture(exportFileType == ExportFileType.PNG);
                    for (int a = 0; a < pixels.Length; a++)
                    {
                        var pixel = pixels[a];

                        pixel.r = pixel.g = pixel.b = pixel.a = GetPixelChannel(pixel);

                        pixels[a] = pixel;
                    }
                    tex.SetPixels(pixels);
                    tex.Apply();

                    var dir = Path.GetDirectoryName(path);
                    var pathName = Path.GetFileNameWithoutExtension(path) + "_" + channel.ToString();
                    var pathExtension = Path.GetExtension(path);
                    Export(tex, Path.Combine(dir, pathName + "." + pathExtension), exportFileType);
                }

                ExportChannel(DataStructures.RGBAChannel.R);
                ExportChannel(DataStructures.RGBAChannel.G);
                ExportChannel(DataStructures.RGBAChannel.B);
                ExportChannel(DataStructures.RGBAChannel.A);
            }
            else
            {
                if (!textureExport.channelR || !textureExport.channelG || !textureExport.channelB || !textureExport.channelA)
                {
                    var defaultChannel = DataStructures.RGBAChannel.R;
                    if (!textureExport.channelR) defaultChannel = DataStructures.RGBAChannel.G;
                    if (!textureExport.channelG) defaultChannel = DataStructures.RGBAChannel.B;
                    if (!textureExport.channelB) defaultChannel = DataStructures.RGBAChannel.A;
                    if (!textureExport.channelA) defaultChannel = DataStructures.RGBAChannel.R;
                    float GetDefaultPixelChannel(Color pixel) => defaultChannel == DataStructures.RGBAChannel.R ? pixel.r : (defaultChannel == DataStructures.RGBAChannel.G ? pixel.g : (defaultChannel == DataStructures.RGBAChannel.B ? pixel.b : pixel.a));

                    var pixels = tex.GetPixels();
                    tex = tex.InstantiateTexture(exportFileType == ExportFileType.PNG);
                    for (int a = 0; a < pixels.Length; a++)
                    {
                        var pixel = pixels[a];

                        if (!textureExport.channelR) pixel.r = GetDefaultPixelChannel(pixel);
                        if (!textureExport.channelG) pixel.g = GetDefaultPixelChannel(pixel);
                        if (!textureExport.channelB) pixel.b = GetDefaultPixelChannel(pixel);
                        if (!textureExport.channelA) pixel.a = GetDefaultPixelChannel(pixel);

                        pixels[a] = pixel;
                    }
                    tex.SetPixels(pixels);
                    tex.Apply();
                }

                Export(tex, path, exportFileType);
            }
        }
        public static bool Export(Texture2D texture, string path, ExportFileType exportFileType = ExportFileType.JPG)
        {
            if (texture == null) return false;

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) return false;

            path = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(path)}{GetFileExtension(exportFileType)}");

            byte[] bytes = null;
            switch (exportFileType)
            {
                case ExportFileType.JPG:
                    bytes = texture.EncodeToJPG();
                    break;

                case ExportFileType.PNG:
                    bytes = texture.EncodeToPNG();
                    break;
            }

            if (bytes != null) File.WriteAllBytes(path, bytes);

            return true;
        }
        #endregion

    }

}

#endif