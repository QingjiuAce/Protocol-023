using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI 设计令牌 —— 移植自 LVMO_GAME 的 Win11 Mica / 亚克力设计系统。
    ///
    /// 所有界面统一从这里取值，改一处全局生效。
    /// 颜色采用深色主题（配游戏冷灰工业废墟的调性），
    /// 数值与源设计一一对应，方便以后和设计稿对齐。
    /// </summary>
    public static class UITheme
    {
        // ---------------- 背景层（带透明度，塑造亚克力层次） ----------------

        /// <summary>亚克力主面板 rgba(20,25,40,0.78)</summary>
        public static readonly Color Acrylic = new Color32(20, 25, 40, 199);

        /// <summary>内层容器 rgba(28,32,50,0.65)</summary>
        public static readonly Color Layer = new Color32(28, 32, 50, 166);

        /// <summary>卡片 rgba(40,45,65,0.55)</summary>
        public static readonly Color Card = new Color32(40, 45, 65, 140);

        /// <summary>悬停底色 rgba(120,180,255,0.08)</summary>
        public static readonly Color Hover = new Color32(120, 180, 255, 20);

        /// <summary>细边框 rgba(120,180,255,0.10)</summary>
        public static readonly Color Border = new Color32(120, 180, 255, 26);

        /// <summary>弹出面板背后的遮罩 rgba(0,0,0,0.4)</summary>
        public static readonly Color Overlay = new Color32(0, 0, 0, 102);

        // ---------------- 文字 ----------------

        public static readonly Color TextPrimary = new Color32(232, 237, 245, 255);   // #e8edf5
        public static readonly Color TextSecondary = new Color32(200, 215, 240, 166);
        public static readonly Color TextTertiary = new Color32(180, 200, 230, 102);

        // ---------------- 强调色 ----------------

        public static readonly Color Accent = new Color32(96, 205, 255, 255);         // #60cdff
        public static readonly Color AccentHover = new Color32(59, 183, 240, 255);    // #3bb7f0
        public static readonly Color AccentDeep = new Color32(59, 130, 246, 255);     // #3b82f6
        public static readonly Color AccentText = new Color32(138, 212, 255, 255);    // #8ad4ff

        public static readonly Color Success = new Color32(54, 196, 61, 255);         // #36c43d
        public static readonly Color Danger = new Color32(232, 17, 35, 255);          // #e81123
        public static readonly Color Warning = new Color32(253, 188, 64, 255);        // #fdbc40

        // ---------------- 圆角 ----------------

        public const int RadiusSm = 4;
        public const int RadiusMd = 7;
        public const int RadiusLg = 12;

        // ---------------- 间距 ----------------

        public const float SpaceXs = 4f;
        public const float SpaceSm = 8f;
        public const float SpaceMd = 12f;
        public const float SpaceLg = 20f;
        public const float SpaceXl = 28f;

        // ---------------- 字号 ----------------

        public const float FontTitle = 22f;
        public const float FontHeading = 16f;
        public const float FontBody = 14f;
        public const float FontCaption = 12f;

        // ---------------- 常用尺寸 ----------------

        /// <summary>设置面板整体尺寸</summary>
        public static readonly Vector2 SettingsPanelSize = new Vector2(640f, 560f);

        /// <summary>按钮尺寸（固定尺寸以保证渐变不被拉伸）</summary>
        public static readonly Vector2 ButtonSize = new Vector2(160f, 40f);

        /// <summary>滑动条尺寸</summary>
        public static readonly Vector2 SliderSize = new Vector2(260f, 22f);

        /// <summary>下拉选项卡尺寸</summary>
        public static readonly Vector2 DropdownSize = new Vector2(180f, 32f);

        /// <summary>数值输入框尺寸</summary>
        public static readonly Vector2 InputFieldSize = new Vector2(90f, 32f);

        // ---------------- 准星基准尺寸（像素，均以 1080p 画布为基准） ----------------

        /// <summary>圆点基准直径：粗细为 1.00 时正好 5 像素</summary>
        public const float CrosshairDotDiameter = 5f;

        /// <summary>圆形准星的外径</summary>
        public const float CrosshairRingDiameter = 22f;

        /// <summary>十字准星的总跨度（从一端到另一端）</summary>
        public const float CrosshairCrossSpan = 22f;

        /// <summary>十字中心留空的半径</summary>
        public const float CrosshairGap = 3f;

        /// <summary>强调整按钮的对角渐变（左上 → 右下）</summary>
        public static Color AccentGradientStart => Accent;

        public static Color AccentGradientEnd => AccentDeep;
    }
}
