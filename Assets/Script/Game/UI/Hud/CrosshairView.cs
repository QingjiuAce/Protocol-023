using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD 准星：根据设置里的样式与粗细实时重绘。
    ///
    /// 三种样式 —— 圆点（默认，直径 5 像素）/ 圆形（圆环）/ 十字。
    /// 粗细值与像素 1:1 对应（1.00 = 1 像素），改设置后立即生效。
    /// </summary>
    [DisallowMultipleComponent]
    public class CrosshairView : MonoBehaviour
    {
        private Image dot;
        private Image ring;
        private RectTransform crossRoot;
        private Image crossTop;
        private Image crossBottom;
        private Image crossLeft;
        private Image crossRight;

        public static CrosshairView Create(RectTransform parent)
        {
            GameObject host = new GameObject("Crosshair", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(64f, 64f);

            CrosshairView view = host.AddComponent<CrosshairView>();
            view.Build();
            view.Rebuild();
            return view;
        }

        void OnEnable() => GameSettings.CrosshairChanged += Rebuild;

        void OnDisable() => GameSettings.CrosshairChanged -= Rebuild;

        // ---------------- 搭建 ----------------

        private void Build()
        {
            if (dot != null) return;

            Color color = UITheme.TextPrimary;

            dot = CreatePart("Dot", UISpriteFactory.Circle(64), color, transform);
            ring = CreatePart("Ring", null, color, transform);

            GameObject crossHost = new GameObject("Cross", typeof(RectTransform));
            crossHost.transform.SetParent(transform, false);
            crossRoot = crossHost.GetComponent<RectTransform>();
            crossRoot.anchorMin = new Vector2(0.5f, 0.5f);
            crossRoot.anchorMax = new Vector2(0.5f, 0.5f);
            crossRoot.pivot = new Vector2(0.5f, 0.5f);
            crossRoot.anchoredPosition = Vector2.zero;
            crossRoot.sizeDelta = Vector2.zero;

            crossTop = CreatePart("CrossTop", null, color, crossRoot);
            crossBottom = CreatePart("CrossBottom", null, color, crossRoot);
            crossLeft = CreatePart("CrossLeft", null, color, crossRoot);
            crossRight = CreatePart("CrossRight", null, color, crossRoot);
        }

        private static Image CreatePart(string name, Sprite sprite, Color color, Transform parent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            Image image = host.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return image;
        }

        // ---------------- 按设置重绘 ----------------

        private void Rebuild()
        {
            if (dot == null) Build();

            GameSettings.CrosshairStyle style = GameSettings.Crosshair;
            float thickness = Mathf.Max(GameSettings.CrosshairThickness, 0.1f);

            dot.gameObject.SetActive(style == GameSettings.CrosshairStyle.Dot);
            ring.gameObject.SetActive(style == GameSettings.CrosshairStyle.Circle);
            crossRoot.gameObject.SetActive(style == GameSettings.CrosshairStyle.Cross);

            switch (style)
            {
                case GameSettings.CrosshairStyle.Circle:
                    ApplyRing(thickness);
                    break;
                case GameSettings.CrosshairStyle.Cross:
                    ApplyCross(thickness);
                    break;
                default:
                    ApplyDot(thickness);
                    break;
            }
        }

        /// <summary>圆点：基准直径 5 像素 × 粗细</summary>
        private void ApplyDot(float thickness)
        {
            float diameter = UITheme.CrosshairDotDiameter * thickness;
            dot.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        }

        /// <summary>圆形：外径固定，线宽跟随粗细</summary>
        private void ApplyRing(float thickness)
        {
            float outer = UITheme.CrosshairRingDiameter;
            ring.sprite = UISpriteFactory.Ring(outer, thickness);
            ring.rectTransform.sizeDelta = new Vector2(outer, outer);
        }

        /// <summary>十字：四条臂围绕中心留空摆放，线宽跟随粗细</summary>
        private void ApplyCross(float thickness)
        {
            float span = UITheme.CrosshairCrossSpan;
            float gap = UITheme.CrosshairGap;
            float armLength = Mathf.Max(span * 0.5f - gap, 1f);
            float line = Mathf.Max(thickness, 0.5f);

            float offset = gap + armLength * 0.5f;

            SetArm(crossTop, new Vector2(line, armLength), new Vector2(0f, offset));
            SetArm(crossBottom, new Vector2(line, armLength), new Vector2(0f, -offset));
            SetArm(crossLeft, new Vector2(armLength, line), new Vector2(-offset, 0f));
            SetArm(crossRight, new Vector2(armLength, line), new Vector2(offset, 0f));
        }

        private static void SetArm(Image arm, Vector2 size, Vector2 position)
        {
            RectTransform rect = arm.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
