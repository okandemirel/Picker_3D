// adapted from https://stackoverflow.com/questions/30103425/find-dominant-color-in-an-image

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace AssetInventory
{
    public static class ImageUtils
    {
        // palette adapted from http://eastfarthing.com/blog/2016-05-06-palette/
        public static Color[] PALETTE_32 =
        {
            FromHex("#d6a090"),
            FromHex("#fe3b1e"),
            FromHex("#a12c32"),
            FromHex("#fa2f7a"),
            FromHex("#fb9fda"),
            FromHex("#e61cf7"),
            FromHex("#992f7c"),
            FromHex("#47011f"),
            FromHex("#051155"),
            FromHex("#4f02ec"),
            FromHex("#2d69cb"),
            FromHex("#00a6ee"),
            FromHex("#6febff"),
            FromHex("#08a29a"),
            FromHex("#2a666a"),
            FromHex("#063619"),
            FromHex("#000000"),
            FromHex("#4a4957"),
            FromHex("#8e7ba4"),
            FromHex("#b7c0ff"),
            FromHex("#ffffff"),
            FromHex("#acbe9c"),
            FromHex("#827c70"),
            FromHex("#5a3b1c"),
            FromHex("#ae6507"),
            FromHex("#f7aa30"),
            FromHex("#f4ea5c"),
            FromHex("#9b9500"),
            FromHex("#566204"),
            FromHex("#11963b"),
            FromHex("#51e113"),
            FromHex("#08fdcc")
        };

        private static Color UNITY_BACKGROUND = new Color(0.321568638f, 0.321568638f, 0.321568638f, 1f);

        public static Color FromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color result))
            {
                return result;
            }
            return Color.clear;
        }

        public static Color GetNearestColor(Color inputColor)
        {
            double inputRed = Convert.ToDouble(inputColor.r);
            double inputGreen = Convert.ToDouble(inputColor.g);
            double inputBlue = Convert.ToDouble(inputColor.b);

            Color nearestColor = Color.clear;
            double distance = 500.0;
            foreach (Color color in PALETTE_32)
            {
                // Compute Euclidean distance between the two colors
                double testRed = Math.Pow(Convert.ToDouble(color.r) - inputRed, 2.0);
                double testGreen = Math.Pow(Convert.ToDouble(color.g) - inputGreen, 2.0);
                double testBlue = Math.Pow(Convert.ToDouble(color.b) - inputBlue, 2.0);
                double tempDistance = Math.Sqrt(testBlue + testGreen + testRed);
                if (tempDistance == 0.0) return color;
                if (tempDistance < distance)
                {
                    distance = tempDistance;
                    nearestColor = color;
                }
            }
            return nearestColor;
        }

        public static Color GetMostUsedColor(Texture2D texture)
        {
            Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color pixelColor = texture.GetPixel(x, y);
                    if (pixelColor == UNITY_BACKGROUND) continue;
                    if (pixelColor.a == 0) continue;

                    if (colorCount.Keys.Contains(pixelColor))
                    {
                        colorCount[pixelColor]++;
                    }
                    else
                    {
                        colorCount.Add(pixelColor, 1);
                    }
                }
            }
            if (colorCount.Count == 0) return Color.clear;

            return colorCount.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key;
        }

        public static float GetHue(Texture2D source)
        {
            if (source == null) return -1;

            Color32[] texColors = source.GetPixels32();
            int total = texColors.Length;
            float r = 0;
            float g = 0;
            float b = 0;
            float count = 0;

            for (int i = 0; i < total; i++)
            {
                Color32 pixelColor = texColors[i];
                if (pixelColor.a > .25f)
                {
                    count++;
                    r += pixelColor.r;
                    g += pixelColor.g;
                    b += pixelColor.b;
                }
            }
            return RGBToHue(r / 256f / count, g / 256f / count, b / 256f / count);
        }

        public static float ToHue(this Color color) => RGBToHue(color.r, color.g, color.b);

        // adapted from https://stackoverflow.com/questions/23090019/fastest-formula-to-get-hue-from-rgb
        public static float RGBToHue(float r, float g, float b)
        {
            float min = Mathf.Min(Mathf.Min(r, g), b);
            float max = Mathf.Max(Mathf.Max(r, g), b);
            if (min == max) return 0;
            float delta = max - min;

            float hue = 0;
            if (r == max)
            {
                hue = (g - b) / delta;
            }
            else if (g == max)
            {
                hue = 2 + (b - r) / delta;
            }
            else if (b == max)
            {
                hue = 4 + (r - g) / delta;
            }
            hue *= 60;

            if (hue < 0.0f) hue += 360;

            return hue;
        }

        public static Texture FillTexture(this Texture2D texture, Color color)
        {
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        public static Texture2D Resize(this Texture2D texture, int size)
        {
            int targetX = size;
            int targetY = size;

            if (texture.width > texture.height) targetY = (int) (targetX * ((float) texture.height / texture.width));
            if (texture.height > texture.width) targetX = (int) (targetY * ((float) texture.width / texture.height));

            RenderTexture rt = RenderTexture.GetTemporary(targetX, targetY, 24, GraphicsFormat.R32G32B32A32_SFloat, 1);
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Texture2D result = new Texture2D(targetX, targetY, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            result.Apply();
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}