using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD 交互提示：准星下方那个「[F] 拾取 AKM」小胶囊。
    ///
    /// 由 UIHub 统一创建，PlayerInteractor 只负责往里塞文字（传 null 即隐藏）。
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractionPromptView : MonoBehaviour
    {
        private const float VerticalOffset = -70f;
        private const float Height = 38f;
        private const float HorizontalPadding = 36f;

        private TextMeshProUGUI label;
        private CanvasGroup canvasGroup;

        private string currentText;

        public static InteractionPromptView Create(RectTransform parent)
        {
            GameObject host = new GameObject("InteractionPrompt", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, VerticalOffset);
            rect.sizeDelta = new Vector2(220f, Height);

            InteractionPromptView view = host.AddComponent<InteractionPromptView>();
            view.Build();
            return view;
        }

        private void Build()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // 描边层铺满，填充层内缩 1 像素，合起来就是 1px 细边框
            Image border = UIKit.CreateImage(transform, "Border",
                UISpriteFactory.RoundedRect(UITheme.RadiusMd), UITheme.Border);
            UIKit.Stretch(border.rectTransform, 0f);

            Image background = UIKit.CreateImage(transform, "Background",
                UISpriteFactory.RoundedRect(UITheme.RadiusMd), UITheme.Acrylic);
            UIKit.Stretch(background.rectTransform, 1f);

            label = UIKit.CreateText(transform, "Label", string.Empty,
                UITheme.FontBody, UITheme.TextPrimary, TextAlignmentOptions.Center);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(HorizontalPadding * 0.5f, 0f);
            labelRect.offsetMax = new Vector2(-HorizontalPadding * 0.5f, 0f);

            gameObject.SetActive(false);
        }

        /// <summary>设置提示文字；传 null 或空字符串则淡出隐藏</summary>
        public void SetPrompt(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                currentText = null;
                return;
            }

            string formatted = $"[F] {text}";
            if (formatted == currentText) return;

            currentText = formatted;
            label.text = formatted;
            ResizeToFit();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                canvasGroup.alpha = 0f;
            }
        }

        private void ResizeToFit()
        {
            if (label == null) return;

            label.ForceMeshUpdate();
            float width = Mathf.Max(label.preferredWidth + HorizontalPadding, 150f);

            // 只改外层尺寸，背景与描边是拉伸锚点，会自动跟随
            RectTransform self = GetComponent<RectTransform>();
            self.sizeDelta = new Vector2(width, Height);
        }

        void Update()
        {
            if (canvasGroup == null) return;

            float target = string.IsNullOrEmpty(currentText) ? 0f : 1f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.unscaledDeltaTime * 8f);

            if (canvasGroup.alpha <= 0.001f && target <= 0f && gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
