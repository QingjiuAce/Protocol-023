using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 运行时生成 UI 贴图与精灵（圆角矩形、圆形、对角渐变），并做缓存。
    ///
    /// 目的是让界面统一保持亚克力风格的圆角与观感，且不依赖任何美术资源 —— 全部由代码画出来。
    /// </summary>
    public static class UISpriteFactory
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 白色圆角矩形（九宫格）。挂到 Image 上后用 color 上色即可，
        /// Image 的类型要设为「九宫格切片 Sliced」，圆角才不会被拉伸变形。
        /// </summary>
        public static Sprite RoundedRect(int radius)
        {
            string key = $"rr_{radius}";
            if (cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            int width = radius * 2 + 4;     // 中间留 4px 可拉伸区
            int border = radius + 2;

            Texture2D texture = new Texture2D(width, width, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[width * width];
            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = RoundedAlpha(x, y, width, radius);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, width),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));

            sprite.name = key;
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>圆形（滑动条手柄、头像等）</summary>
        public static Sprite Circle(int size = 64)
        {
            string key = $"circle_{size}";
            if (cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// 圆环（用于圆形准星）。
        /// 贴图按 4 倍超采样绘制再缩小显示，曲线才够平滑。
        /// </summary>
        /// <param name="outerDiameterPixels">最终显示的像素外径</param>
        /// <param name="thicknessPixels">最终显示的像素线宽</param>
        public static Sprite Ring(float outerDiameterPixels, float thicknessPixels)
        {
            const int superSample = 4;
            int size = Mathf.Max(Mathf.RoundToInt(outerDiameterPixels * superSample), 8);

            float ratio = size / Mathf.Max(outerDiameterPixels, 0.001f);
            float thickness = Mathf.Max(thicknessPixels * ratio, 1f);

            string key = $"ring_{size}_{thickness:F2}";
            if (cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float outer = size * 0.5f - 0.5f;
            float inner = Mathf.Max(outer - thickness, 0f);

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float outerAlpha = Mathf.Clamp01(outer - distance + 0.5f);
                    float innerAlpha = Mathf.Clamp01(distance - inner + 0.5f);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// 圆角 + 对角渐变（左上 → 右下），用于强调按钮。
        /// 因为渐变不能被九宫格拉伸，这里生成固定尺寸的精灵，按钮就用原始尺寸摆放。
        /// </summary>
        public static Sprite RoundedGradient(int width, int height, int radius, Color start, Color end)
        {
            string key = $"rg_{width}_{height}_{radius}_{ColorUtility.ToHtmlStringRGBA(start)}_{ColorUtility.ToHtmlStringRGBA(end)}";
            if (cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[width * height];
            float denom = Mathf.Max(width + height - 2, 1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 135° 对角渐变：用 (x + (height-1-y)) 作为进度，等价 CSS 的 135deg
                    float t = (x + (height - 1 - y)) / denom;
                    Color rgb = Color.Lerp(start, end, Mathf.Clamp01(t));

                    float alpha = RoundedAlpha(x, y, width, height, radius);
                    pixels[y * width + x] = new Color(rgb.r, rgb.g, rgb.b, rgb.a * alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>竖直渐变（用于滑动条的填充条）</summary>
        public static Sprite VerticalGradient(int height, Color top, Color bottom)
        {
            string key = $"vg_{height}_{ColorUtility.ToHtmlStringRGBA(top)}_{ColorUtility.ToHtmlStringRGBA(bottom)}";
            if (cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            const int width = 4;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                Color rgb = Color.Lerp(bottom, top, height <= 1 ? 1f : y / (float)(height - 1));
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = rgb;
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            cache[key] = sprite;
            return sprite;
        }

        // ---------------- 抗锯齿圆角计算 ----------------

        private static float RoundedAlpha(int x, int y, int size, int radius)
            => RoundedAlpha(x, y, size, size, radius);

        private static float RoundedAlpha(int x, int y, int width, int height, int radius)
        {
            if (radius <= 0) return 1f;

            // 距离最近那个圆角区域中心的偏移量；中间区域两轴偏移都是 0
            float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (width - 1 - radius)));
            float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (height - 1 - radius)));

            float distance = Mathf.Sqrt(dx * dx + dy * dy);

            // 留 1px 过渡带做抗锯齿
            return Mathf.Clamp01(radius + 0.5f - distance);
        }

        /// <summary>清空缓存（组件销毁或切换主题时用）</summary>
        public static void ClearCache()
        {
            cache.Clear();
        }
    }
}
