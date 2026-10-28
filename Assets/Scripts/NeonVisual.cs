using System.Collections.Generic;
using UnityEngine;

namespace Askich
{
    /// <summary>Original procedural sprites. No external art or imported texture dependencies.</summary>
    public static class NeonVisual
    {
        public static readonly Color Cyan = new Color(0.22f, 0.96f, 0.91f);
        public static readonly Color Coral = new Color(1f, 0.29f, 0.38f);
        public static readonly Color Gold = new Color(1f, 0.75f, 0.3f);
        public static readonly Color Violet = new Color(0.64f, 0.47f, 1f);
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Sprite Get(string shape)
        {
            if (sprites.TryGetValue(shape, out Sprite cached) && cached != null) return cached;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Generated " + shape, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f) / size * 2 - 1, py = (y + 0.5f) / size * 2 - 1;
                float r = Mathf.Sqrt(px * px + py * py);
                float d = r;
                if (shape == "square") d = Mathf.Max(Mathf.Abs(px), Mathf.Abs(py));
                if (shape == "diamond") d = Mathf.Abs(px) + Mathf.Abs(py);
                if (shape == "hex") d = Mathf.Max(Mathf.Abs(py), Mathf.Abs(px) * 0.8660254f + Mathf.Abs(py) * 0.5f);
                if (shape == "ship") d = Mathf.Max(-px * 1.7f, px + Mathf.Abs(py) * 1.8f);
                if (shape == "ship_guard")
                {
                    float nose = Mathf.Max(-px * 1.7f, px + Mathf.Abs(py) * 1.8f);
                    float wings = Mathf.Max(Mathf.Abs(px + 0.28f) * 3.8f, Mathf.Abs(py) * 1.18f);
                    d = Mathf.Min(nose, wings);
                }
                if (shape == "ship_ghost") d = Mathf.Max(-px * 1.35f, px + Mathf.Abs(py) * 2.8f);
                float a = Mathf.Clamp01((0.94f - d) * 80);
                if (shape == "ring") a *= Mathf.Clamp01((r - 0.83f) * 80);
                if (shape == "glow") a = Mathf.Pow(Mathf.Clamp01(1 - r), 2) * 0.65f;
                pixels[y * size + x] = new Color(1, 1, 1, a);
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
            sprite.name = shape; sprites[shape] = sprite; return sprite;
        }

        public static SpriteRenderer Make(string name, string shape, Color color, Vector2 position, Vector2 scale, int order, Transform parent)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = Get(shape); sr.color = color; sr.sortingOrder = order;
            return sr;
        }

        public static SpriteRenderer Body(string name, string shape, Color color, Vector2 position, float size, Transform parent)
        {
            var body = Make(name, shape, color, position, Vector2.one * size, 10, parent);
            Make("Glow", "glow", new Color(color.r, color.g, color.b, 0.65f), Vector2.zero, Vector2.one * 3.4f, 7, body.transform);
            Make("Inset", shape, new Color(0.035f, 0.075f, 0.12f), Vector2.zero, Vector2.one * 0.72f, 11, body.transform);
            Make("Core", shape, color, Vector2.zero, Vector2.one * 0.35f, 12, body.transform);
            return body;
        }
    }
}
