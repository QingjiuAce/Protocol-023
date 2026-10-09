using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 所有弹出面板的基类：背包、容器搜索、设置、暂停等继承它。
    ///
    /// 已内置：淡入淡出、入栈出栈、ESC 返回、鼠标与输入的自动接管。
    /// 子类只需要重写 OnOpened / OnClosed 关心自己的业务数据即可。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIPanelBase : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] protected UILayer layer = UILayer.Panel;

        [Tooltip("按下 ESC 是否关闭本面板")]
        [SerializeField] protected bool closeOnEscape = true;

        [Tooltip("淡入淡出时长，秒")]
        [SerializeField] protected float fadeDuration = 0.12f;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        public UILayer Layer => layer;
        public bool CloseOnEscape => closeOnEscape;
        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        protected virtual void OnEnable()
        {
            if (UIHub.Instance != null) UIHub.Instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (UIHub.Instance != null) UIHub.Instance.Unregister(this);
            IsOpen = false;
        }

        // ---------------- 开关 ----------------

        public virtual void Open()
        {
            if (IsOpen) return;
            IsOpen = true;

            gameObject.SetActive(true);
            if (UIHub.Instance != null) UIHub.Instance.Register(this);

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(1f, true));

            OnOpened();
        }

        public virtual void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(0f, false));

            OnClosed();
        }

        /// <summary>ESC 或返回键触发。返回 false 表示本面板不处理，交给更下层</summary>
        public virtual bool OnBack()
        {
            Close();
            return true;
        }

        private IEnumerator FadeTo(float target, bool keepAlive)
        {
            if (canvasGroup == null) yield break;

            canvasGroup.interactable = keepAlive;
            canvasGroup.blocksRaycasts = keepAlive;

            float start = canvasGroup.alpha;
            float time = 0f;

            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(time / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = target;

            if (!keepAlive)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                if (UIHub.Instance != null) UIHub.Instance.Unregister(this);
                gameObject.SetActive(false);
            }

            fadeRoutine = null;
        }

        // ---------------- 子类钩子 ----------------

        /// <summary>面板开始显示时调用（淡入的同时就会触发），用来刷新数据</summary>
        protected virtual void OnOpened() { }

        /// <summary>面板开始隐藏时调用，用来做清理或存档</summary>
        protected virtual void OnClosed() { }
    }
}
