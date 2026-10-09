using System;
using System.Collections.Generic;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI 总枢纽：挂在带画布的 UI Root 上，全场景唯一。
    ///
    /// 它负责三件事：
    ///   1. 铺出五个分层子画布（World / Hud / Panel / Toast / Debug），各自配好排序值
    ///   2. 管理弹出面板的生命周期与栈顶互斥
    ///   3. 输入仲裁：有面板打开时释放鼠标、并让玩家移动与视角暂停响应
    ///
    /// 用法：
    ///   UIHub.Instance.GetLayer(UILayer.Hud)          取某一层的父节点
    ///   UIHub.Instance.Register(panel) / Unregister   面板入栈出栈
    ///   UIHub.Instance.CloseTop()                     关闭最上层（ESC 常用）
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class UIHub : MonoBehaviour
    {
        public static UIHub Instance { get; private set; }

        [Header("画布")]
        [SerializeField] private Canvas rootCanvas;

        [Header("常驻")]
        [Tooltip("勾选后跨场景保留（大厅与对局共用一套 UI 时建议勾选）")]
        [SerializeField] private bool persistent = true;

        [Header("调试角标")]
        [SerializeField] private bool showVersionLabel = true;
        [SerializeField] private string versionText = "PreDev 0.1.0 - Alpha";
        [SerializeField] private Color versionColor = new Color(0.66f, 0.86f, 0.91f, 0.92f);
        [SerializeField] private int versionFontSize = 22;

        [Header("设置菜单")]
        [Tooltip("游戏中按 ESC 是否唤出设置菜单")]
        [SerializeField] private bool openSettingsOnEscape = true;

        [Header("交互提示")]
        [Tooltip("准心对准可交互物时是否显示提示")]
        [SerializeField] private bool showInteractionPrompt = true;

        private readonly Dictionary<UILayer, RectTransform> layers = new Dictionary<UILayer, RectTransform>();
        private readonly List<UIPanelBase> panelStack = new List<UIPanelBase>();
        private SettingsPanel settingsPanel;
        private InteractionPromptView interactionPrompt;

        /// <summary>参数为「当前是否有面板打开」</summary>
        public event Action<bool> PanelStateChanged;

        public bool AnyPanelOpen => panelStack.Count > 0;
        public RectTransform Root => rootCanvas != null ? rootCanvas.transform as RectTransform : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistent && transform.parent == null)
                DontDestroyOnLoad(gameObject);

            if (rootCanvas == null) rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null) rootCanvas = GetComponentInChildren<Canvas>();

            if (rootCanvas == null)
            {
                Debug.LogError("[UIHub] 没有找到画布组件，请把本脚本挂在带 Canvas 的 UI Root 上。", this);
                enabled = false;
                return;
            }

            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 像素对齐：减少非整数缩放带来的半像素发虚，对文字观感有帮助
            rootCanvas.pixelPerfect = true;

            BuildAllLayers();

            // 准星常驻 HUD 层，跟随设置实时重绘
            CrosshairView.Create(GetLayer(UILayer.Hud));

            if (showVersionLabel)
                VersionLabel.Create(GetLayer(UILayer.Debug), versionText, versionColor, versionFontSize);
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // 有面板开着 → 关掉最上层（面板可自行拒绝，例如正在输入文字）
            if (panelStack.Count > 0)
            {
                UIPanelBase top = panelStack[panelStack.Count - 1];
                if (top != null && top.CloseOnEscape)
                    top.OnBack();
                return;
            }

            // 没有面板 → 唤出设置菜单（ESC 再按一次即可关闭）
            if (openSettingsOnEscape)
                OpenSettings();
        }

        // ---------------- 设置菜单 ----------------

        /// <summary>唤出设置菜单（首次调用时创建，之后复用同一个实例）</summary>
        public SettingsPanel OpenSettings()
        {
            if (settingsPanel == null)
                settingsPanel = SettingsPanel.Create(GetLayer(UILayer.Panel));

            if (!settingsPanel.IsOpen)
                settingsPanel.Open();

            return settingsPanel;
        }

        // ---------------- 交互提示 ----------------

        /// <summary>设置 HUD 交互提示文字；传 null 或空字符串则淡出隐藏</summary>
        public void SetInteractionPrompt(string text)
        {
            if (!showInteractionPrompt) return;

            if (interactionPrompt == null)
                interactionPrompt = InteractionPromptView.Create(GetLayer(UILayer.Hud));

            interactionPrompt.SetPrompt(text);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            PlayerInput.Blocked = false;
        }

        // ---------------- 分层画布 ----------------

        private void BuildAllLayers()
        {
            Array values = Enum.GetValues(typeof(UILayer));
            foreach (UILayer layer in values)
                EnsureLayer(layer);
        }

        /// <summary>取某一层的父节点，没有就现场补一个</summary>
        public RectTransform GetLayer(UILayer layer)
        {
            if (layers.TryGetValue(layer, out RectTransform cached) && cached != null)
                return cached;
            return EnsureLayer(layer);
        }

        private RectTransform EnsureLayer(UILayer layer)
        {
            string objectName = UILayerUtility.ObjectName(layer);

            Transform existing = rootCanvas.transform.Find(objectName);
            GameObject layerObject;

            if (existing != null)
            {
                layerObject = existing.gameObject;
            }
            else
            {
                layerObject = new GameObject(objectName, typeof(RectTransform));
                layerObject.transform.SetParent(rootCanvas.transform, false);
            }

            RectTransform rect = layerObject.GetComponent<RectTransform>();
            if (rect == null) rect = layerObject.AddComponent<RectTransform>();
            Stretch(rect);

            Canvas layerCanvas = layerObject.GetComponent<Canvas>();
            if (layerCanvas == null) layerCanvas = layerObject.AddComponent<Canvas>();
            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = (int)layer;

            GraphicRaycaster raycaster = layerObject.GetComponent<GraphicRaycaster>();
            if (UILayerUtility.NeedsRaycast(layer))
            {
                if (raycaster == null) raycaster = layerObject.AddComponent<GraphicRaycaster>();
                raycaster.enabled = true;
            }
            else if (raycaster != null)
            {
                raycaster.enabled = false;
            }

            layers[layer] = rect;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition3D = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        // ---------------- 面板管理 ----------------

        /// <summary>把面板压入栈（面板在自己的 OnEnable 里调用即可）</summary>
        public void Register(UIPanelBase panel)
        {
            if (panel == null || panelStack.Contains(panel)) return;

            bool wasEmpty = panelStack.Count == 0;
            panelStack.Add(panel);

            if (panel.transform.parent != GetLayer(panel.Layer))
                panel.transform.SetParent(GetLayer(panel.Layer), false);

            if (wasEmpty) RaisePanelState(true);
        }

        public void Unregister(UIPanelBase panel)
        {
            if (panel == null) return;
            if (!panelStack.Remove(panel)) return;

            if (panelStack.Count == 0) RaisePanelState(false);
        }

        /// <summary>关闭最上层的面板</summary>
        public bool CloseTop()
        {
            for (int i = panelStack.Count - 1; i >= 0; i--)
            {
                UIPanelBase panel = panelStack[i];
                if (panel == null)
                {
                    panelStack.RemoveAt(i);
                    continue;
                }
                panel.Close();
                return true;
            }
            return false;
        }

        public void CloseAll()
        {
            for (int i = panelStack.Count - 1; i >= 0; i--)
            {
                UIPanelBase panel = panelStack[i];
                if (panel != null) panel.Close();
            }
            panelStack.Clear();
            RaisePanelState(false);
        }

        private void RaisePanelState(bool opened)
        {
            // 有面板时：释放鼠标、并屏蔽玩家的移动与视角输入
            Cursor.lockState = opened ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = opened;
            PlayerInput.Blocked = opened;

            PanelStateChanged?.Invoke(opened);
        }

        // ---------------- 便捷创建 ----------------

        /// <summary>把面板预制件实例化到它自己声明的层里</summary>
        public T Create<T>(T prefab) where T : UIPanelBase
        {
            if (prefab == null) return null;
            T instance = Instantiate(prefab, GetLayer(prefab.Layer));
            instance.name = prefab.name;
            return instance;
        }

        /// <summary>直接开一个面板：实例化 + 打开</summary>
        public T Open<T>(T prefab) where T : UIPanelBase
        {
            T instance = Create(prefab);
            if (instance != null) instance.Open();
            return instance;
        }
    }
}
