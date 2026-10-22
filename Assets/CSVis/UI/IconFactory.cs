using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSVis.UI
{
    public enum UIIcon
    {
        Home,
        Back,
        Chevron,
        Close,
        Minimize,
        Pin,
        Trash,
        Folder,
        Info,
        Check,
        Warning,
        Error,
        Target,
    }

    /// <summary>
    /// Draws crisp, resolution independent UI glyphs at runtime (signed distance shapes rendered to a small
    /// white texture) so the UI needs no icon assets and can tint them with any colour.
    /// </summary>
    public static class IconFactory
    {
        private const int Size = 256;
        private const float Stroke = 0.055f;

        private static readonly Dictionary<UIIcon, Sprite> Cache = new Dictionary<UIIcon, Sprite>();

        public static Sprite Get(UIIcon icon)
        {
            if (Cache.TryGetValue(icon, out var sprite) && sprite != null)
            {
                return sprite;
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
            {
                name = "CSVis icon " + icon,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[Size * Size];
            var shape = ShapeFor(icon);
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    // Distance in unit-square coordinates (y up); anti-alias over about one pixel
                    var d = shape((x + 0.5f) / Size, (y + 0.5f) / Size);
                    var alpha = Mathf.Clamp01(0.5f - d * Size);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, true);

            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[icon] = sprite;
            return sprite;
        }

        private delegate float Sdf(float x, float y);

        private static Sdf ShapeFor(UIIcon icon)
        {
            switch (icon)
            {
                case UIIcon.Back:
                    return Union(Line(0.62f, 0.2f, 0.34f, 0.5f), Line(0.34f, 0.5f, 0.62f, 0.8f));
                case UIIcon.Chevron:
                    return Union(Line(0.38f, 0.2f, 0.66f, 0.5f), Line(0.66f, 0.5f, 0.38f, 0.8f));
                case UIIcon.Close:
                    return Union(Line(0.24f, 0.24f, 0.76f, 0.76f), Line(0.24f, 0.76f, 0.76f, 0.24f));
                case UIIcon.Minimize:
                    return Line(0.22f, 0.5f, 0.78f, 0.5f);
                case UIIcon.Home:
                    return Union(
                        Line(0.12f, 0.5f, 0.5f, 0.84f), Line(0.5f, 0.84f, 0.88f, 0.5f),
                        Line(0.24f, 0.5f, 0.24f, 0.16f), Line(0.76f, 0.5f, 0.76f, 0.16f), Line(0.24f, 0.16f, 0.76f, 0.16f),
                        Line(0.43f, 0.16f, 0.43f, 0.4f), Line(0.57f, 0.16f, 0.57f, 0.4f), Line(0.43f, 0.4f, 0.57f, 0.4f));
                case UIIcon.Pin:
                    return Union(
                        Disc(0.5f, 0.64f, 0.19f), Line(0.5f, 0.5f, 0.5f, 0.1f), Line(0.27f, 0.45f, 0.73f, 0.45f));
                case UIIcon.Trash:
                    return Union(
                        Line(0.18f, 0.74f, 0.82f, 0.74f),
                        Line(0.4f, 0.74f, 0.4f, 0.86f), Line(0.4f, 0.86f, 0.6f, 0.86f), Line(0.6f, 0.86f, 0.6f, 0.74f),
                        Line(0.27f, 0.74f, 0.32f, 0.14f), Line(0.32f, 0.14f, 0.68f, 0.14f), Line(0.68f, 0.14f, 0.73f, 0.74f),
                        Line(0.5f, 0.62f, 0.5f, 0.28f));
                case UIIcon.Folder:
                    return Union(
                        Line(0.1f, 0.2f, 0.9f, 0.2f), Line(0.9f, 0.2f, 0.9f, 0.68f), Line(0.9f, 0.68f, 0.46f, 0.68f),
                        Line(0.46f, 0.68f, 0.38f, 0.8f), Line(0.38f, 0.8f, 0.1f, 0.8f), Line(0.1f, 0.8f, 0.1f, 0.2f));
                case UIIcon.Info:
                    return Union(Ring(0.5f, 0.5f, 0.38f), Disc(0.5f, 0.72f, 0.055f), Line(0.5f, 0.56f, 0.5f, 0.28f));
                case UIIcon.Check:
                    return Union(Line(0.18f, 0.5f, 0.42f, 0.26f), Line(0.42f, 0.26f, 0.82f, 0.72f));
                case UIIcon.Warning:
                    return Union(
                        Line(0.5f, 0.86f, 0.1f, 0.18f), Line(0.1f, 0.18f, 0.9f, 0.18f), Line(0.9f, 0.18f, 0.5f, 0.86f),
                        Line(0.5f, 0.64f, 0.5f, 0.4f), Disc(0.5f, 0.29f, 0.05f));
                case UIIcon.Error:
                    return Union(Ring(0.5f, 0.5f, 0.38f), Line(0.36f, 0.36f, 0.64f, 0.64f), Line(0.36f, 0.64f, 0.64f, 0.36f));
                case UIIcon.Target:
                    return Union(
                        Ring(0.5f, 0.5f, 0.27f), Disc(0.5f, 0.5f, 0.06f),
                        Line(0.5f, 0.1f, 0.5f, 0.22f), Line(0.5f, 0.78f, 0.5f, 0.9f),
                        Line(0.1f, 0.5f, 0.22f, 0.5f), Line(0.78f, 0.5f, 0.9f, 0.5f));
                default:
                    return Disc(0.5f, 0.5f, 0.3f);
            }
        }

        private static Sdf Union(params Sdf[] shapes)
        {
            return (x, y) =>
            {
                var d = float.MaxValue;
                foreach (var shape in shapes)
                {
                    d = Mathf.Min(d, shape(x, y));
                }

                return d;
            };
        }

        private static Sdf Disc(float cx, float cy, float radius)
        {
            return (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius;
        }

        private static Sdf Ring(float cx, float cy, float radius)
        {
            return (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius) - Stroke;
        }

        private static Sdf Line(float ax, float ay, float bx, float by)
        {
            return (x, y) =>
            {
                float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
                var h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
                var dx = pax - bax * h;
                var dy = pay - bay * h;
                return Mathf.Sqrt(dx * dx + dy * dy) - Stroke;
            };
        }
    }
}
