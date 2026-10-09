using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 全局游戏设置。数值存进 PlayerPrefs，关掉游戏也保留。
    ///
    /// 目前只有鼠标灵敏度，后续音量、画质、按键都往这里加，
    /// 界面只管读写这里的属性，不关心存储细节。
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMouseSensitivity = "settings.mouseSensitivity";

        public const float MouseSensitivityMin = 0.10f;
        public const float MouseSensitivityMax = 3.00f;
        public const float MouseSensitivityDefault = 1.00f;

        private static float mouseSensitivity = MouseSensitivityDefault;
        private static bool loaded;

        /// <summary>灵敏度变化时触发（界面刷新、视角系统都靠它）</summary>
        public static event System.Action MouseSensitivityChanged;

        /// <summary>鼠标灵敏度倍率，1.00 为基准</summary>
        public static float MouseSensitivity
        {
            get
            {
                EnsureLoaded();
                return mouseSensitivity;
            }
            set
            {
                EnsureLoaded();

                float clamped = Mathf.Clamp(value, MouseSensitivityMin, MouseSensitivityMax);
                if (Mathf.Approximately(clamped, mouseSensitivity)) return;

                mouseSensitivity = clamped;
                PlayerPrefs.SetFloat(KeyMouseSensitivity, mouseSensitivity);
                MouseSensitivityChanged?.Invoke();
            }
        }

        // ---------------- 准星 ----------------

        private const string KeyCrosshairStyle = "settings.crosshairStyle";
        private const string KeyCrosshairThickness = "settings.crosshairThickness";

        public const float CrosshairThicknessMin = 0.5f;
        public const float CrosshairThicknessMax = 10f;
        public const float CrosshairThicknessDefault = 1.00f;

        /// <summary>准星样式</summary>
        public enum CrosshairStyle
        {
            /// <summary>中心小圆点（默认）</summary>
            Dot = 0,

            /// <summary>圆形准星（圆环）</summary>
            Circle = 1,

            /// <summary>十字准星</summary>
            Cross = 2,
        }

        private static CrosshairStyle crosshairStyle = CrosshairStyle.Dot;
        private static float crosshairThickness = CrosshairThicknessDefault;

        /// <summary>准星相关设置变化时触发（准星视图靠它刷新）</summary>
        public static event System.Action CrosshairChanged;

        /// <summary>准星样式：圆点 / 圆形 / 十字</summary>
        public static CrosshairStyle Crosshair
        {
            get
            {
                EnsureLoaded();
                return crosshairStyle;
            }
            set
            {
                EnsureLoaded();
                if (crosshairStyle == value) return;

                crosshairStyle = value;
                PlayerPrefs.SetInt(KeyCrosshairStyle, (int)crosshairStyle);
                CrosshairChanged?.Invoke();
            }
        }

        /// <summary>准星粗细，1.00 对应 1 像素</summary>
        public static float CrosshairThickness
        {
            get
            {
                EnsureLoaded();
                return crosshairThickness;
            }
            set
            {
                EnsureLoaded();

                float clamped = Mathf.Clamp(value, CrosshairThicknessMin, CrosshairThicknessMax);
                if (Mathf.Approximately(clamped, crosshairThickness)) return;

                crosshairThickness = clamped;
                PlayerPrefs.SetFloat(KeyCrosshairThickness, crosshairThickness);
                CrosshairChanged?.Invoke();
            }
        }

        public static void Save() => PlayerPrefs.Save();

        /// <summary>恢复出厂设置</summary>
        public static void ResetToDefault()
        {
            MouseSensitivity = MouseSensitivityDefault;
            Crosshair = CrosshairStyle.Dot;
            CrosshairThickness = CrosshairThicknessDefault;
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;

            mouseSensitivity = Mathf.Clamp(
                PlayerPrefs.GetFloat(KeyMouseSensitivity, MouseSensitivityDefault),
                MouseSensitivityMin, MouseSensitivityMax);

            int styleValue = PlayerPrefs.GetInt(KeyCrosshairStyle, (int)CrosshairStyle.Dot);
            crosshairStyle = (CrosshairStyle)Mathf.Clamp(styleValue, 0, 2);

            crosshairThickness = Mathf.Clamp(
                PlayerPrefs.GetFloat(KeyCrosshairThickness, CrosshairThicknessDefault),
                CrosshairThicknessMin, CrosshairThicknessMax);

            loaded = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => EnsureLoaded();
    }
}
