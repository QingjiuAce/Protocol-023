using Game.Interaction;
using Game.UI;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 玩家准心射线：每帧从屏幕正中向前发射一条射线，
    /// 命中带 IInteractable 的物体时显示提示，按 F 触发交互。
    ///
    /// 距离默认 3 米。射线从相机发出（与准心完全一致），
    /// 被打到的物体沿父链向上找接口，所以命中子物体也能识别。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("检测")]
        [Tooltip("交互判定距离（米）")]
        [SerializeField] private float range = 3f;

        [Tooltip("参与交互检测的层")]
        [SerializeField] private LayerMask interactMask = ~0;

        [Tooltip("留空则自动找玩家相机")]
        [SerializeField] private Camera viewCamera;

        [Tooltip("每秒在控制台打印一次射线命中情况，排查问题时打开")]
        [SerializeField] private bool debugLogging = false;

        private IInteractable current;
        private string lastPrompt;
        private readonly RaycastHit[] buffer = new RaycastHit[16];
        private float debugTimer;

        void Awake()
        {
            if (viewCamera == null)
            {
                PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
                if (playerCamera != null)
                    viewCamera = playerCamera.GetComponentInChildren<Camera>();
            }

            if (viewCamera == null)
                viewCamera = Camera.main;

            if (viewCamera == null)
                Debug.LogWarning("[PlayerInteractor] 没有找到相机，交互射线无法工作。", this);
        }

        void Update()
        {
            Scan();

            if (current != null && PlayerInput.InteractKey)
                current.Interact(gameObject);
        }

        // ---------------- 扫描 ----------------

        private void Scan()
        {
            IInteractable found = PlayerInput.Blocked ? null : FindTarget();

            if (!IsAlive(found)) found = null;

            current = found;
            RefreshPrompt();
        }

        /// <summary>
        /// 发射线并挑出最近的可交互目标。
        ///
        /// 这里必须用 NonAlloc 一次拿到【所有】命中：玩家自己的触发胶囊把相机包在里面，
        /// 用单次 Raycast 会先打到自己就停了，后面的物体永远检测不到。
        /// 拿到全部命中后，跳过自己的碰撞器，再从剩下的里挑最近的那个可交互物。
        /// </summary>
        private IInteractable FindTarget()
        {
            if (viewCamera == null) return null;

            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            int count = Physics.RaycastNonAlloc(ray, buffer, range, interactMask, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = buffer[i];
                if (hit.collider == null) continue;
                if (hit.transform.IsChildOf(transform)) continue;       // 自己的碰撞器
                if (hit.distance >= bestDistance) continue;

                IInteractable candidate = hit.collider.GetComponentInParent<IInteractable>();
                if (candidate == null) continue;

                bestDistance = hit.distance;
                best = candidate;
            }

            if (debugLogging)
            {
                debugTimer -= Time.deltaTime;
                if (debugTimer <= 0f)
                {
                    debugTimer = 1f;
                    Debug.Log($"[交互射线] 命中 {count} 个碰撞器；可交互目标 = " +
                        (best != null ? best.InteractionPrompt : "无"));
                }
            }

            return best;
        }

        private void RefreshPrompt()
        {
            string prompt = null;

            if (IsAlive(current) && current.CanInteract(gameObject))
                prompt = current.InteractionPrompt;

            if (string.IsNullOrEmpty(prompt)) prompt = null;

            // 文本没变就不打扰 UI
            if (prompt == lastPrompt) return;
            lastPrompt = prompt;

            if (UIHub.Instance != null)
                UIHub.Instance.SetInteractionPrompt(prompt);
        }

        /// <summary>Unity 的「假空」检查：对象被销毁后引用仍在，但这里要当作 null</summary>
        private static bool IsAlive(IInteractable target)
        {
            if (target == null) return false;
            Object unityObject = target as Object;
            return unityObject != null;
        }

        // ---------------- 外部查询 ----------------

        /// <summary>当前对准的可交互物（没有则为 null）</summary>
        public IInteractable CurrentTarget => IsAlive(current) ? current : null;

        /// <summary>手动触发一次交互（给按键重绑定或触屏用）</summary>
        public void TriggerInteract()
        {
            if (IsAlive(current)) current.Interact(gameObject);
        }

        void OnDisable()
        {
            current = null;
            lastPrompt = null;
            if (UIHub.Instance != null) UIHub.Instance.SetInteractionPrompt(null);
        }
    }
}
