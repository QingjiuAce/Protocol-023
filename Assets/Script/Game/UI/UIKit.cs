using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI 元素工厂 —— 用代码拼出符合当前 UITheme 的控件，
    /// 免去手动摆预制件的麻烦，也保证所有界面风格绝对一致。
    /// </summary>
    public static class UIKit
    {
        // ---------------- 基础图形 ----------------

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            Image image = host.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;

            // 带九宫格边框的精灵用切片模式，圆角才不会被拉变形
            if (sprite != null && sprite.border != Vector4.zero)
                image.type = Image.Type.Sliced;

            return image;
        }

        /// <summary>铺满父容器，inset 为四边内缩距离</summary>
        public static RectTransform Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        /// <summary>以顶部-左为基准摆放（用于纵向列表里的行）</summary>
        public static RectTransform AnchorTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>水平居中摆放</summary>
        public static RectTransform AnchorCenter(RectTransform rect, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        // ---------------- 面板 ----------------

        /// <summary>
        /// 亚克力面板：填充 + 1px 描边 + 圆角。
        /// 返回外层面板的 RectTransform，子元素往它里面放。
        /// </summary>
        public static RectTransform CreatePanel(Transform parent, string name, Vector2 size,
            Color fill, Color border, int radius = UITheme.RadiusLg)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            if (border.a > 0.001f)
            {
                Image borderImage = CreateImage(host.transform, "Border", UISpriteFactory.RoundedRect(radius), border);
                Stretch(borderImage.rectTransform, 0f);
            }

            Image fillImage = CreateImage(host.transform, "Fill", UISpriteFactory.RoundedRect(radius), fill);
            Stretch(fillImage.rectTransform, 1f);

            return rect;
        }

        // ---------------- 文字 ----------------

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content,
            float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            TextMeshProUGUI text = host.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        // ---------------- 按钮 ----------------

        /// <summary>强调按钮：冰蓝对角渐变 + 圆角</summary>
        public static Button CreateAccentButton(Transform parent, string name, string label,
            Vector2 size, System.Action onClick)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            int radius = UITheme.RadiusSm;
            Image image = host.AddComponent<Image>();
            image.sprite = UISpriteFactory.RoundedGradient(
                Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), radius,
                UITheme.AccentGradientStart, UITheme.AccentGradientEnd);
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = host.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            TextMeshProUGUI text = CreateText(host.transform, "Label", label,
                UITheme.FontBody, Color.white, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 0f);

            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            return button;
        }

        /// <summary>幽灵按钮：透明底 + 悬停高亮</summary>
        public static Button CreateGhostButton(Transform parent, string name, string label,
            Vector2 size, System.Action onClick)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image background = CreateImage(host.transform, "Bg",
                UISpriteFactory.RoundedRect(UITheme.RadiusSm), new Color(1f, 1f, 1f, 0.04f), true);
            Stretch(background.rectTransform, 0f);

            Button button = host.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.05f);
            colors.highlightedColor = new Color(120f / 255f, 180f / 255f, 1f, 0.16f);
            colors.pressedColor = new Color(120f / 255f, 180f / 255f, 1f, 0.22f);
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            TextMeshProUGUI text = CreateText(host.transform, "Label", label,
                UITheme.FontBody, UITheme.TextSecondary, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 0f);

            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            return button;
        }

        // ---------------- 滑动条 ----------------

        /// <summary>无文字的滑动条（轨道 + 冰蓝填充 + 圆形手柄）</summary>
        public static Slider CreateSlider(Transform parent, string name, Vector2 size,
            float min, float max, float value)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            // 轨道高度与圆角半径要匹配：九宫格切片的边框是 半径+2，条形高度需 ≥ 边框×2
            const float trackHeight = 8f;
            const int trackRadius = 2;
            const float handleSize = 20f;
            float padding = handleSize * 0.5f;

            // 轨道底
            Image track = CreateImage(host.transform, "Track",
                UISpriteFactory.RoundedRect(trackRadius), new Color(1f, 1f, 1f, 0.10f));
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.offsetMin = new Vector2(padding, -trackHeight * 0.5f);
            trackRect.offsetMax = new Vector2(-padding, trackHeight * 0.5f);

            // 已填充部分
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(host.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(padding, -trackHeight * 0.5f);
            fillAreaRect.offsetMax = new Vector2(-padding, trackHeight * 0.5f);

            Image fill = CreateImage(fillArea.transform, "Fill",
                UISpriteFactory.RoundedRect(trackRadius), UITheme.Accent);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 0f);

            // 手柄
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(host.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(padding, 0f);
            handleAreaRect.offsetMax = new Vector2(-padding, 0f);

            Image handle = CreateImage(handleArea.transform, "Handle",
                UISpriteFactory.Circle(64), Color.white, true);
            RectTransform handleRect = handle.rectTransform;
            handleRect.sizeDelta = new Vector2(handleSize, handleSize);

            Slider slider = host.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.value = Mathf.Clamp(value, min, max);
            slider.transition = Selectable.Transition.None;

            return slider;
        }

        // ---------------- 下拉选项卡 ----------------

        /// <summary>亚克力风格的下拉选项卡</summary>
        public static TMP_Dropdown CreateDropdown(Transform parent, string name, Vector2 size,
            List<string> options, int selectedIndex)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image background = CreateImage(host.transform, "Background",
                UISpriteFactory.RoundedRect(UITheme.RadiusSm), UITheme.Card, true);
            Stretch(background.rectTransform, 0f);

            TextMeshProUGUI caption = CreateText(host.transform, "Label", string.Empty,
                UITheme.FontBody, UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            RectTransform captionRect = caption.rectTransform;
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(12f, 0f);
            captionRect.offsetMax = new Vector2(-28f, 0f);

            Image arrow = CreateImage(host.transform, "Arrow",
                UISpriteFactory.Circle(32), UITheme.TextTertiary);
            RectTransform arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-11f, 0f);
            arrowRect.sizeDelta = new Vector2(8f, 8f);

            RectTransform template = BuildDropdownTemplate(host.transform);
            TextMeshProUGUI itemLabel = template.GetComponentInChildren<TextMeshProUGUI>(true);

            TMP_Dropdown dropdown = host.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = background;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.template = template;

            dropdown.options.Clear();
            for (int i = 0; i < options.Count; i++)
                dropdown.options.Add(new TMP_Dropdown.OptionData(options[i]));

            dropdown.value = Mathf.Clamp(selectedIndex, 0, Mathf.Max(options.Count - 1, 0));
            dropdown.RefreshShownValue();

            return dropdown;
        }

        private static RectTransform BuildDropdownTemplate(Transform parent)
        {
            GameObject templateHost = new GameObject("Template", typeof(RectTransform));
            templateHost.transform.SetParent(parent, false);

            RectTransform template = templateHost.GetComponent<RectTransform>();
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -2f);
            template.sizeDelta = new Vector2(0f, 132f);

            Image templateBackground = templateHost.AddComponent<Image>();
            templateBackground.sprite = UISpriteFactory.RoundedRect(UITheme.RadiusSm);
            templateBackground.type = Image.Type.Sliced;
            templateBackground.color = UITheme.Acrylic;

            // 下拉列表要有独立画布才能盖在其他内容之上
            Canvas templateCanvas = templateHost.AddComponent<Canvas>();
            templateCanvas.overrideSorting = true;
            templateCanvas.sortingOrder = 30000;
            templateHost.AddComponent<GraphicRaycaster>();

            ScrollRect scroll = templateHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportHost = new GameObject("Viewport", typeof(RectTransform));
            viewportHost.transform.SetParent(templateHost.transform, false);
            RectTransform viewport = viewportHost.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(4f, 4f);
            viewport.offsetMax = new Vector2(-4f, -4f);
            viewport.pivot = new Vector2(0f, 1f);
            viewportHost.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            GameObject contentHost = new GameObject("Content", typeof(RectTransform));
            contentHost.transform.SetParent(viewportHost.transform, false);
            RectTransform content = contentHost.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 28f);
            scroll.content = content;

            // 单个选项
            GameObject itemHost = new GameObject("Item", typeof(RectTransform));
            itemHost.transform.SetParent(contentHost.transform, false);
            RectTransform item = itemHost.GetComponent<RectTransform>();
            item.anchorMin = new Vector2(0f, 0.5f);
            item.anchorMax = new Vector2(1f, 0.5f);
            item.pivot = new Vector2(0.5f, 0.5f);
            item.sizeDelta = new Vector2(0f, 28f);

            Image itemBackground = CreateImage(itemHost.transform, "Item Background", null,
                new Color(1f, 1f, 1f, 0f), true);
            Stretch(itemBackground.rectTransform, 0f);

            Image itemCheckmark = CreateImage(itemHost.transform, "Item Checkmark",
                UISpriteFactory.Circle(32), UITheme.Accent);
            RectTransform checkRect = itemCheckmark.rectTransform;
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.anchoredPosition = new Vector2(10f, 0f);
            checkRect.sizeDelta = new Vector2(8f, 8f);

            TextMeshProUGUI itemLabel = CreateText(itemHost.transform, "Item Label", string.Empty,
                UITheme.FontBody, UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            RectTransform labelRect = itemLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(26f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);

            Toggle toggle = itemHost.AddComponent<Toggle>();
            toggle.targetGraphic = itemBackground;
            toggle.graphic = itemCheckmark;

            ColorBlock toggleColors = toggle.colors;
            toggleColors.normalColor = new Color(1f, 1f, 1f, 0f);
            toggleColors.highlightedColor = new Color(120f / 255f, 180f / 255f, 1f, 0.16f);
            toggleColors.pressedColor = new Color(120f / 255f, 180f / 255f, 1f, 0.22f);
            toggleColors.selectedColor = toggleColors.normalColor;
            toggleColors.fadeDuration = 0.08f;
            toggle.colors = toggleColors;

            templateHost.SetActive(false);
            return template;
        }

        // ---------------- 数值输入框 ----------------

        /// <summary>数字输入框（小数），回车或失焦时回调</summary>
        public static TMP_InputField CreateNumberInput(Transform parent, string name, Vector2 size,
            string value, System.Action<string> onSubmitted)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image background = CreateImage(host.transform, "Background",
                UISpriteFactory.RoundedRect(UITheme.RadiusSm), UITheme.Card, true);
            Stretch(background.rectTransform, 0f);

            GameObject viewportHost = new GameObject("Text Area", typeof(RectTransform));
            viewportHost.transform.SetParent(host.transform, false);
            RectTransform viewport = viewportHost.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(10f, 2f);
            viewport.offsetMax = new Vector2(-10f, -2f);
            viewportHost.AddComponent<RectMask2D>();

            TextMeshProUGUI placeholder = CreateText(viewportHost.transform, "Placeholder", "1.00",
                UITheme.FontBody, UITheme.TextTertiary, TextAlignmentOptions.MidlineLeft);
            Stretch(placeholder.rectTransform, 0f);

            TextMeshProUGUI content = CreateText(viewportHost.transform, "Text", string.Empty,
                UITheme.FontBody, UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            Stretch(content.rectTransform, 0f);

            TMP_InputField input = host.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = content;
            input.placeholder = placeholder;
            input.targetGraphic = background;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            input.restoreOriginalTextOnEscape = true;
            input.text = value;

            if (onSubmitted != null)
                input.onEndEdit.AddListener(v => onSubmitted(v));

            return input;
        }

        // ---------------- 设置项行 ----------------

        /// <summary>
        /// 一行设置项：左侧标签、右侧控件、最右数值。
        /// 返回这一行的 RectTransform，控件由调用方自行往里放。
        /// </summary>
        public static RectTransform CreateSettingRow(Transform parent, string name, string label,
            float y, float width, float height,
            out TextMeshProUGUI labelText, out TextMeshProUGUI valueText)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            RectTransform row = host.GetComponent<RectTransform>();
            AnchorTopLeft(row, UITheme.SpaceXl, y, width, height);

            labelText = CreateText(row, "Label", label, UITheme.FontBody, UITheme.TextPrimary);
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(220f, height);

            valueText = CreateText(row, "Value", string.Empty, UITheme.FontBody,
                UITheme.AccentText, TextAlignmentOptions.Right);
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(1f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(90f, height);

            return row;
        }
    }
}
