#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.Unity
{

    [ExecuteAlways]
    public class TexturePacker : MonoBehaviour
    {

        public bool pack;

        public string savePath;
        public string textureName;

        public Color defaultPixel;

        [Serializable]
        public enum PixelComparisonMethod
        {
            Replace, Max, Min
        }
        public static float ComparePixelVal(float valA, float valB, PixelComparisonMethod method)
        {
            switch(method)
            {
                case PixelComparisonMethod.Max:
                    return Mathf.Max(valA, valB);

                case PixelComparisonMethod.Min:
                    return Mathf.Min(valA, valB);
            }

            return valB;
        }
        [Serializable]
        public struct TextureChannel 
        {
            public Texture2D[] textures;
            public DataStructures.RGBAChannel channel;
            public PixelComparisonMethod pixelComparisonMethod;
            public bool invert;
        }

        public TextureChannel channelR;
        public TextureChannel channelG;
        public TextureChannel channelB;
        public TextureChannel channelA;

        public Vector2Int textureDimensions = new Vector2Int(1024, 1024);
        public TextureFormat textureFormat = TextureFormat.RGBA32;
        public bool mipMaps;
        public bool linear;
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp; 
        public FilterMode filterMode = FilterMode.Bilinear;

        public Texture2D outputTexture;

        public void Update()
        {
            if (pack)
            {
                pack = false;
                outputTexture = PackTexture(textureName, defaultPixel, channelR, channelG, channelB, channelA, textureDimensions.x, textureDimensions.y, textureFormat, mipMaps, linear, wrapMode, filterMode);

#if UNITY_EDITOR

                if (string.IsNullOrEmpty(savePath)) savePath = "";
 
                outputTexture = outputTexture.CreateOrReplaceAsset(outputTexture.CreateUnityAssetPathString(savePath, "asset"));
                UnityEditor.AssetDatabase.SaveAssetIfDirty(outputTexture);

#endif
            }
        }

        public static Texture2D PackTexture(string textureName, Color defaultPixel, TextureChannel channelR, TextureChannel channelG, TextureChannel channelB, TextureChannel channelA, int width, int height, TextureFormat format, bool mipMaps, bool linear, TextureWrapMode wrapMode, FilterMode filterMode)
        {
            Color[] pixels = new Color[width * height];
            for (int a = 0; a < pixels.Length; a++) pixels[a] = defaultPixel;

            if (channelR.textures != null)
            {
                foreach (var tex in channelR.textures)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int pixIndex = (y * width) + x;
                            var pixel = pixels[pixIndex];

                            Color channelPixel;
                            if (tex.width != width || tex.height != height)
                            {
                                float u = x / (width - 1f);
                                float v = y / (height - 1f);
                                channelPixel = tex.GetPixelBilinear(u, v);
                            } 
                            else
                            {
                                channelPixel = tex.GetPixel(x, y);
                            }
                            switch (channelR.channel)
                            {
                                case DataStructures.RGBAChannel.R:
                                    pixel.r = channelR.invert ? 1 - ComparePixelVal(pixel.r, channelPixel.r, channelR.pixelComparisonMethod) : ComparePixelVal(pixel.r, channelPixel.r, channelR.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.G:
                                    pixel.r = channelR.invert ? 1 - ComparePixelVal(pixel.g, channelPixel.g, channelR.pixelComparisonMethod) : ComparePixelVal(pixel.g, channelPixel.g, channelR.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.B:
                                    pixel.r = channelR.invert ? 1 - ComparePixelVal(pixel.b, channelPixel.b, channelR.pixelComparisonMethod) : ComparePixelVal(pixel.b, channelPixel.b, channelR.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.A:
                                    pixel.r = channelR.invert ? 1 - ComparePixelVal(pixel.a, channelPixel.a, channelR.pixelComparisonMethod) : ComparePixelVal(pixel.a, channelPixel.a, channelR.pixelComparisonMethod);
                                    break;
                            }

                            pixels[pixIndex] = pixel;
                        }
                    }
                }
            }

            if (channelG.textures != null)
            {
                foreach (var tex in channelG.textures)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int pixIndex = (y * width) + x;
                            var pixel = pixels[pixIndex];

                            Color channelPixel;
                            if (tex.width != width || tex.height != height)
                            {
                                float u = x / (width - 1f);
                                float v = y / (height - 1f);
                                channelPixel = tex.GetPixelBilinear(u, v);
                            }
                            else
                            {
                                channelPixel = tex.GetPixel(x, y);
                            }
                            switch (channelG.channel)
                            {
                                case DataStructures.RGBAChannel.R:
                                    pixel.g = channelG.invert ? 1 - ComparePixelVal(pixel.r, channelPixel.r, channelG.pixelComparisonMethod) : ComparePixelVal(pixel.r, channelPixel.r, channelG.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.G:
                                    pixel.g = channelG.invert ? 1 - ComparePixelVal(pixel.g, channelPixel.g, channelG.pixelComparisonMethod) : ComparePixelVal(pixel.g, channelPixel.g, channelG.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.B:
                                    pixel.g = channelG.invert ? 1 - ComparePixelVal(pixel.b, channelPixel.b, channelG.pixelComparisonMethod) : ComparePixelVal(pixel.b, channelPixel.b, channelG.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.A:
                                    pixel.g = channelG.invert ? 1 - ComparePixelVal(pixel.a, channelPixel.a, channelG.pixelComparisonMethod) : ComparePixelVal(pixel.a, channelPixel.a, channelG.pixelComparisonMethod);
                                    break;
                            }

                            pixels[pixIndex] = pixel;
                        }
                    }
                }
            }

            if (channelB.textures != null)
            {
                foreach (var tex in channelB.textures)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int pixIndex = (y * width) + x; 
                            var pixel = pixels[pixIndex];

                            Color channelPixel;
                            if (tex.width != width || tex.height != height)
                            {
                                float u = x / (width - 1f);
                                float v = y / (height - 1f);
                                channelPixel = tex.GetPixelBilinear(u, v);
                            }
                            else
                            {
                                channelPixel = tex.GetPixel(x, y);
                            }
                            switch (channelB.channel)
                            {
                                case DataStructures.RGBAChannel.R:
                                    pixel.b = channelB.invert ? 1 - ComparePixelVal(pixel.r, channelPixel.r, channelB.pixelComparisonMethod) : ComparePixelVal(pixel.r, channelPixel.r, channelB.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.G:
                                    pixel.b = channelB.invert ? 1 - ComparePixelVal(pixel.g, channelPixel.g, channelB.pixelComparisonMethod) : ComparePixelVal(pixel.g, channelPixel.g, channelB.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.B:
                                    pixel.b = channelB.invert ? 1 - ComparePixelVal(pixel.b, channelPixel.b, channelB.pixelComparisonMethod) : ComparePixelVal(pixel.b, channelPixel.b, channelB.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.A:
                                    pixel.b = channelB.invert ? 1 - ComparePixelVal(pixel.a, channelPixel.a, channelB.pixelComparisonMethod) : ComparePixelVal(pixel.a, channelPixel.a, channelB.pixelComparisonMethod);
                                    break;
                            }

                            pixels[pixIndex] = pixel;
                        }
                    }
                }
            }

            if (channelA.textures != null)
            {
                foreach (var tex in channelA.textures)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int pixIndex = (y * width) + x; 
                            var pixel = pixels[pixIndex];

                            Color channelPixel;
                            if (tex.width != width || tex.height != height)
                            {
                                float u = x / (width - 1f);
                                float v = y / (height - 1f);
                                channelPixel = tex.GetPixelBilinear(u, v);
                            }
                            else
                            {
                                channelPixel = tex.GetPixel(x, y);
                            }
                            switch (channelA.channel)
                            {
                                case DataStructures.RGBAChannel.R:
                                    pixel.a = channelA.invert ? 1 - ComparePixelVal(pixel.r, channelPixel.r, channelA.pixelComparisonMethod) : ComparePixelVal(pixel.r, channelPixel.r, channelA.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.G:
                                    pixel.a = channelA.invert ? 1 - ComparePixelVal(pixel.g, channelPixel.g, channelA.pixelComparisonMethod) : ComparePixelVal(pixel.g, channelPixel.g, channelA.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.B:
                                    pixel.a = channelA.invert ? 1 - ComparePixelVal(pixel.b, channelPixel.b, channelA.pixelComparisonMethod) : ComparePixelVal(pixel.b, channelPixel.b, channelA.pixelComparisonMethod);
                                    break;
                                case DataStructures.RGBAChannel.A:
                                    pixel.a = channelA.invert ? 1 - ComparePixelVal(pixel.a, channelPixel.a, channelA.pixelComparisonMethod) : ComparePixelVal(pixel.a, channelPixel.a, channelA.pixelComparisonMethod);
                                    break;
                            }

                            pixels[pixIndex] = pixel;
                        }
                    }
                }
            }

            var texture = new Texture2D(width, height, format, mipMaps, linear);

            texture.name = textureName;

            texture.wrapMode = wrapMode;
            texture.filterMode = filterMode;
            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

    }
}

#endif