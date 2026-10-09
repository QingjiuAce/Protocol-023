using System.Collections.Generic;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 设置菜单 —— ESC 唤出，再次 ESC 关闭。
    ///
    /// 界面完全由代码拼装，风格照搬 LVMO_GAME 的 Win11 亚克力设计：
    /// 半透明磨砂面板、冰蓝渐变强调色、圆角与细边框。
    /// 目前只有鼠标灵敏度一项，后续加音量、画质只需再加一行。
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsPanel : UIPanelBase
    {
        private const float FirstRowY = 100f;
        private const float RowStep = 48f;
        private const float RowHeight = 34f;

        private Slider sensitivitySlider;
        private TextMeshProUGUI sensitivityValueText;
        private TMP_Dropdown crosshairStyleDropdown;
        private TMP_InputField crosshairThicknessInput;
        private bool built;

        /// <summary>创建到指定父节点下（通常是 UIHub 的面板层）</summary>
        public static SettingsPanel Create(RectTransform parent)
        {
            GameObject host = new GameObject("SettingsPanel", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            UIKit.Stretch(rect, 0f);

            // AddComponent 会立刻执行 Awake，界面这时就搭好了；随后先隐藏，等 Open() 再显示
            SettingsPanel panel = host.AddComponent<SettingsPanel>();
            host.SetActive(false);
            return panel;
        }

        protected override void Awake()
        {
            base.Awake();

            // 本面板固定待在弹出面板层
            layer = UILayer.Panel;
            BuildUI();
        }

        protected override void OnOpened()
        {
            SyncFromSettings();
        }

        // ---------------- 界面搭建 ----------------

        private void BuildUI()
        {
            if (built) return;
            built = true;

            BuildOverlay();
            RectTransform panel = BuildPanel();
            BuildTitle(panel);
            BuildSensitivityRow(panel);
            BuildCrosshairStyleRow(panel);
            BuildCrosshairThicknessRow(panel);
            BuildFooter(panel);
        }

        private void BuildOverlay()
        {
            GameObject host = new GameObject("Overlay", typeof(RectTransform));
            host.transform.SetParent(transform, false);

            Image overlay = host.AddComponent<Image>();
            overlay.color = UITheme.Overlay;
            overlay.raycastTarget = true;      // 挡住背后的一切点击

            UIKit.Stretch(overlay.rectTransform, 0f);
        }

        private RectTransform BuildPanel()
        {
            return UIKit.CreatePanel(transform, "Panel", UITheme.SettingsPanelSize,
                UITheme.Acrylic, UITheme.Border, UITheme.RadiusLg);
        }

        private void BuildTitle(RectTransform panel)
        {
            Vector2 size = UITheme.SettingsPanelSize;
            float innerWidth = size.x - UITheme.SpaceXl * 2f;

            TextMeshProUGUI title = UIKit.CreateText(panel, "Title", "设置",
                UITheme.FontTitle, UITheme.TextPrimary, TextAlignmentOptions.Left);
            UIKit.AnchorTopLeft(title.rectTransform, UITheme.SpaceXl, UITheme.SpaceLg, innerWidth, 32f);

            // 分隔线用纯色矩形即可，不必做圆角
            Image divider = UIKit.CreateImage(panel, "Divider", null, UITheme.Border);
            UIKit.AnchorTopLeft(divider.rectTransform, UITheme.SpaceXl, UITheme.SpaceLg + 44f, innerWidth, 1f);
        }

        private void BuildSensitivityRow(RectTransform panel)
        {
            Vector2 size = UITheme.SettingsPanelSize;
            float innerWidth = size.x - UITheme.SpaceXl * 2f;

            RectTransform row = UIKit.CreateSettingRow(panel, "MouseSensitivityRow", "鼠标灵敏度",
                FirstRowY, innerWidth, RowHeight,
                out TextMeshProUGUI _, out sensitivityValueText);

            sensitivitySlider = UIKit.CreateSlider(row, "SensitivitySlider", UITheme.SliderSize,
                GameSettings.MouseSensitivityMin, GameSettings.MouseSensitivityMax,
                GameSettings.MouseSensitivity);

            RectTransform sliderRect = sensitivitySlider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(1f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(1f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(-98f, 0f);

            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            SyncFromSettings();
        }

        private void BuildFooter(RectTransform panel)
        {
            Vector2 size = UITheme.SettingsPanelSize;
            float buttonWidth = UITheme.ButtonSize.x;
            float buttonHeight = UITheme.ButtonSize.y;
            float y = size.y - UITheme.SpaceXl - buttonHeight;
            float rightX = size.x - UITheme.SpaceXl - buttonWidth;
            float leftX = rightX - UITheme.SpaceSm - buttonWidth;

            Button reset = UIKit.CreateGhostButton(panel, "ResetButton", "恢复默认",
                UITheme.ButtonSize, OnResetClicked);
            UIKit.AnchorTopLeft(reset.GetComponent<RectTransform>(), leftX, y, buttonWidth, buttonHeight);

            Button close = UIKit.CreateAccentButton(panel, "CloseButton", "关闭",
                UITheme.ButtonSize, Close);
            UIKit.AnchorTopLeft(close.GetComponent<RectTransform>(), rightX, y, buttonWidth, buttonHeight);
        }

        private void BuildCrosshairStyleRow(RectTransform panel)
        {
            float innerWidth = UITheme.SettingsPanelSize.x - UITheme.SpaceXl * 2f;

            RectTransform row = UIKit.CreateSettingRow(panel, "CrosshairStyleRow", "准星样式",
                FirstRowY + RowStep, innerWidth, RowHeight,
                out TextMeshProUGUI _, out TextMeshProUGUI _);

            List<string> options = new List<string> { "圆点", "圆形", "十字" };
            crosshairStyleDropdown = UIKit.CreateDropdown(row, "StyleDropdown",
                UITheme.DropdownSize, options, (int)GameSettings.Crosshair);

            RectTransform rect = crosshairStyleDropdown.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            crosshairStyleDropdown.onValueChanged.AddListener(OnCrosshairStyleChanged);
        }

        private void BuildCrosshairThicknessRow(RectTransform panel)
        {
            float innerWidth = UITheme.SettingsPanelSize.x - UITheme.SpaceXl * 2f;

            RectTransform row = UIKit.CreateSettingRow(panel, "CrosshairThicknessRow", "准星粗细",
                FirstRowY + RowStep * 2f, innerWidth, RowHeight,
                out TextMeshProUGUI _, out TextMeshProUGUI _);

            // 换算说明，放在输入框左侧
            TextMeshProUGUI hint = UIKit.CreateText(row, "Hint", "1.00 = 1 像素",
                UITheme.FontCaption, UITheme.TextTertiary, TextAlignmentOptions.Right);
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(1f, 0.5f);
            hintRect.anchorMax = new Vector2(1f, 0.5f);
            hintRect.pivot = new Vector2(1f, 0.5f);
            hintRect.anchoredPosition = new Vector2(-(UITheme.InputFieldSize.x + UITheme.SpaceSm), 0f);
            hintRect.sizeDelta = new Vector2(140f, RowHeight);

            crosshairThicknessInput = UIKit.CreateNumberInput(row, "ThicknessInput",
                UITheme.InputFieldSize, GameSettings.CrosshairThickness.ToString("0.00"),
                OnCrosshairThicknessSubmitted);

            RectTransform rect = crosshairThicknessInput.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        // ---------------- 交互 ----------------

        private void OnCrosshairStyleChanged(int index)
        {
            GameSettings.Crosshair = (GameSettings.CrosshairStyle)Mathf.Clamp(index, 0, 2);
        }

        private void OnCrosshairThicknessSubmitted(string raw)
        {
            if (float.TryParse(raw, out float value))
                GameSettings.CrosshairThickness = value;

            SyncFromSettings();
        }

        private void OnSensitivityChanged(float value)
        {
            GameSettings.MouseSensitivity = value;
            RefreshValueText();
        }

        private void OnResetClicked()
        {
            GameSettings.ResetToDefault();
            SyncFromSettings();
        }

        private void SyncFromSettings()
        {
            float value = GameSettings.MouseSensitivity;

            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(value);

            RefreshValueText();

            if (crosshairStyleDropdown != null)
                crosshairStyleDropdown.SetValueWithoutNotify((int)GameSettings.Crosshair);

            if (crosshairThicknessInput != null)
                crosshairThicknessInput.text = GameSettings.CrosshairThickness.ToString("0.00");
        }

        private void RefreshValueText()
        {
            if (sensitivityValueText != null)
                sensitivityValueText.text = GameSettings.MouseSensitivity.ToString("0.00");
        }
    }
}
