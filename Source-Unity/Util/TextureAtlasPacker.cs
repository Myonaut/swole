#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.Unity
{

    [ExecuteAlways]
    public class TextureAtlasPacker : MonoBehaviour
    {

        public bool pack;
        public bool packAsJpg;
        public bool packAsPng;

        public string savePath;
        public string textureName;

        public Color defaultPixel;

        [Serializable]
        public struct PixelChannelMapping
        {
            public DataStructures.RGBAChannel sourceChannel;
            public DataStructures.RGBAChannel destinationChannel;
        }
        [Serializable]
        public struct TextureToPack
        {
            public Texture2D texture;
            public Vector2 regionX;
            public bool floorX;
            public Vector2 regionY;
            public bool floorY;
            public PixelChannelMapping channelA;
            public PixelChannelMapping channelB;
            public PixelChannelMapping channelC;
            public PixelChannelMapping channelD;
            public TexturePacker.PixelComparisonMethod pixelComparisonMethod;
            public bool invert;
        }

        public List<TextureToPack> texturesToPack = new List<TextureToPack>();

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
                outputTexture = PackTexture(textureName, defaultPixel, texturesToPack, textureDimensions.x, textureDimensions.y, textureFormat, mipMaps, linear, wrapMode, filterMode);

#if UNITY_EDITOR
                if (string.IsNullOrEmpty(savePath)) savePath = "";

                var _savePath = savePath; 
                if (packAsJpg)
                {
                    _savePath = Extensions.CreateUnityAssetPathString(_savePath, textureName, "jpg");

                    DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
                    directoryInfo = directoryInfo.Parent;

                    var bytes = outputTexture.EncodeToJPG(100);
                    File.WriteAllBytes(Path.Join(directoryInfo.FullName, _savePath), bytes);

                    UnityEditor.AssetDatabase.Refresh();
                }
                else if (packAsPng)
                {
                    _savePath = Extensions.CreateUnityAssetPathString(_savePath, textureName, "png"); 

                    DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
                    directoryInfo = directoryInfo.Parent;

                    var bytes = outputTexture.EncodeToPNG();
                    File.WriteAllBytes(Path.Join(directoryInfo.FullName, _savePath), bytes); 

                    UnityEditor.AssetDatabase.Refresh(); 
                }
                else
                {
                    outputTexture = outputTexture.CreateOrReplaceAsset(outputTexture.CreateUnityAssetPathString(_savePath, "asset"));
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(outputTexture);
                }
#endif
            }
        }

        private static float GetAndComparePixelChannel(float currentValue, bool invert, Color comparisonPixel, DataStructures.RGBAChannel comparisonChannel, TexturePacker.PixelComparisonMethod comparisonMethod)
        {
            float value = currentValue;
            switch (comparisonChannel)
            {
                case DataStructures.RGBAChannel.R:
                    value = invert ? 1f - TexturePacker.ComparePixelVal(currentValue, comparisonPixel.r, comparisonMethod) : TexturePacker.ComparePixelVal(currentValue, comparisonPixel.r, comparisonMethod);
                    break;
                case DataStructures.RGBAChannel.G:
                    value = invert ? 1f - TexturePacker.ComparePixelVal(currentValue, comparisonPixel.g, comparisonMethod) : TexturePacker.ComparePixelVal(currentValue, comparisonPixel.g, comparisonMethod);
                    break;
                case DataStructures.RGBAChannel.B:
                    value = invert ? 1f - TexturePacker.ComparePixelVal(currentValue, comparisonPixel.b, comparisonMethod) : TexturePacker.ComparePixelVal(currentValue, comparisonPixel.b, comparisonMethod);
                    break;
                case DataStructures.RGBAChannel.A:
                    value = invert ? 1f - TexturePacker.ComparePixelVal(currentValue, comparisonPixel.a, comparisonMethod) : TexturePacker.ComparePixelVal(currentValue, comparisonPixel.a, comparisonMethod);
                    break;
            }

            return value;
        }
        public static Texture2D PackTexture(string textureName, Color defaultPixel, ICollection<TextureToPack> texturesToPack, int width, int height, TextureFormat format, bool mipMaps, bool linear, TextureWrapMode wrapMode, FilterMode filterMode)
        {
            Color[] pixels = new Color[width * height];
            for (int a = 0; a < pixels.Length; a++) pixels[a] = defaultPixel;

            float widthM1f = width - 1f;
            float heightM1f = height - 1f;
            foreach(var toPack in texturesToPack)
            {
                int packX0 = toPack.floorX ? Mathf.FloorToInt(widthM1f * toPack.regionX.x) : Mathf.CeilToInt(widthM1f * toPack.regionX.x);
                int packX1 = toPack.floorX ? Mathf.FloorToInt(widthM1f * toPack.regionX.y) : Mathf.CeilToInt(widthM1f * toPack.regionX.y);

                int packY0 = toPack.floorY ? Mathf.FloorToInt(heightM1f * toPack.regionY.x) : Mathf.CeilToInt(heightM1f * toPack.regionY.x);
                int packY1 = toPack.floorY ? Mathf.FloorToInt(heightM1f * toPack.regionY.y) : Mathf.CeilToInt(heightM1f * toPack.regionY.y);

                int packWidth = packX1 - packX0;
                int packHeight = packY1 - packY0;
                if (packWidth <= 0 || packHeight <= 0) continue;

                for(int x = packX0; x <= packX1; x++)
                {
                    for (int y = packY0; y <= packY1; y++)
                    {
                        int packPixIndex = (y * width) + x;
                        var packPixel = pixels[packPixIndex];

                        Color channelPixel;
                        if (toPack.texture.width != packWidth || toPack.texture.height != packHeight)
                        {
                            float u = (x - packX0) / (packWidth - 1f);
                            float v = (y - packY0) / (packHeight - 1f);
                            channelPixel = toPack.texture.GetPixelBilinear(u, v);
                        }
                        else
                        {
                            channelPixel = toPack.texture.GetPixel(x - packX0, y - packY0);
                        }

                        switch (toPack.channelA.destinationChannel)
                        {
                            case DataStructures.RGBAChannel.R:
                                packPixel.r = GetAndComparePixelChannel(packPixel.r, toPack.invert, channelPixel, toPack.channelA.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.G:
                                packPixel.g = GetAndComparePixelChannel(packPixel.g, toPack.invert, channelPixel, toPack.channelA.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.B:
                                packPixel.b = GetAndComparePixelChannel(packPixel.b, toPack.invert, channelPixel, toPack.channelA.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.A:
                                packPixel.a = GetAndComparePixelChannel(packPixel.a, toPack.invert, channelPixel, toPack.channelA.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                        }
                        switch (toPack.channelB.destinationChannel)
                        {
                            case DataStructures.RGBAChannel.R:
                                packPixel.r = GetAndComparePixelChannel(packPixel.r, toPack.invert, channelPixel, toPack.channelB.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.G:
                                packPixel.g = GetAndComparePixelChannel(packPixel.g, toPack.invert, channelPixel, toPack.channelB.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.B:
                                packPixel.b = GetAndComparePixelChannel(packPixel.b, toPack.invert, channelPixel, toPack.channelB.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.A:
                                packPixel.a = GetAndComparePixelChannel(packPixel.a, toPack.invert, channelPixel, toPack.channelB.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                        }
                        switch (toPack.channelC.destinationChannel)
                        {
                            case DataStructures.RGBAChannel.R:
                                packPixel.r = GetAndComparePixelChannel(packPixel.r, toPack.invert, channelPixel, toPack.channelC.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.G:
                                packPixel.g = GetAndComparePixelChannel(packPixel.g, toPack.invert, channelPixel, toPack.channelC.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.B:
                                packPixel.b = GetAndComparePixelChannel(packPixel.b, toPack.invert, channelPixel, toPack.channelC.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.A:
                                packPixel.a = GetAndComparePixelChannel(packPixel.a, toPack.invert, channelPixel, toPack.channelC.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                        }
                        switch (toPack.channelD.destinationChannel)
                        {
                            case DataStructures.RGBAChannel.R:
                                packPixel.r = GetAndComparePixelChannel(packPixel.r, toPack.invert, channelPixel, toPack.channelD.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.G:
                                packPixel.g = GetAndComparePixelChannel(packPixel.g, toPack.invert, channelPixel, toPack.channelD.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.B:
                                packPixel.b = GetAndComparePixelChannel(packPixel.b, toPack.invert, channelPixel, toPack.channelD.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                            case DataStructures.RGBAChannel.A:
                                packPixel.a = GetAndComparePixelChannel(packPixel.a, toPack.invert, channelPixel, toPack.channelD.sourceChannel, toPack.pixelComparisonMethod);
                                break;
                        }

                        pixels[packPixIndex] = packPixel;
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