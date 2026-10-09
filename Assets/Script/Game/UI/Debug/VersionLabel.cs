using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 右下角版本号角标（TextMeshPro 版，矢量字，任意分辨率都锐利）。
    ///
    /// 通常由 UIHub 自动创建到调试层，不需要手动摆放。
    /// 想改文字或颜色，在 UIHub 的检查器里调即可。
    /// </summary>
    [DisallowMultipleComponent]
    public class VersionLabel : MonoBehaviour
    {
        [SerializeField] private Vector2 margin = new Vector2(20f, 16f);
        [Tooltip("SDF 边缘锐度，0 关闭。数值越大越利落，缓解小字号发虚")]
        [SerializeField] private float sharpness = 0.5f;

        private TextMeshProUGUI label;

        public string Text
        {
            get => label != null ? label.text : string.Empty;
            set
            {
                if (label != null) label.text = value;
            }
        }

        /// <summary>在指定父节点下生成一个版本号标签</summary>
        public static VersionLabel Create(RectTransform parent, string text, Color color, int fontSize)
        {
            if (parent == null) return null;

            GameObject host = new GameObject("VersionLabel", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            VersionLabel instance = host.AddComponent<VersionLabel>();
            instance.Build(text, color, fontSize);
            return instance;
        }

        private void Build(string text, Color color, int fontSize)
        {
            RectTransform rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-margin.x, margin.y);
            rect.sizeDelta = new Vector2(640f, fontSize * 2.2f);

            label = GetComponent<TextMeshProUGUI>();
            if (label == null) label = gameObject.AddComponent<TextMeshProUGUI>();

            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.BottomRight;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.richText = false;

            if (label.font == null)
            {
                Debug.LogWarning(
                    "[VersionLabel] 没有可用的字体资源。请在编辑器执行一次：" +
                    "窗口 → TextMeshPro → Import TMP Essential Resources");
                return;
            }

            ApplySharpness();
        }

        /// <summary>单独给这个文本实例做一份材质，调高 SDF 锐度（不改动共享材质）</summary>
        private void ApplySharpness()
        {
            if (sharpness <= 0f) return;
            if (label.fontSharedMaterial == null) return;

            Material instance = new Material(label.fontSharedMaterial);
            if (instance.HasProperty(ShaderUtilities.ID_Sharpness))
                instance.SetFloat(ShaderUtilities.ID_Sharpness, sharpness);
            label.fontMaterial = instance;
        }
    }
}
