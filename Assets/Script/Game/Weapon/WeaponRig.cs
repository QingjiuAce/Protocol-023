using Game.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Weapon
{
    /// <summary>
    /// 第一人称武器系统根节点，挂在主相机上。
    ///
    /// 它负责三件事：
    ///   1. 建一台专用的武器相机，让武器永远不会插进墙里、也不受开镜 FOV 影响而变形
    ///   2. 把主相机剔除武器层，武器相机只渲染武器层
    ///   3. 驱动武器挂点在「腰射」与「开镜」两个姿态之间平滑过渡
    ///
    /// 武器模型放在 WeaponSocket 下，位置和角度都能在检查器里实时微调。
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponRig : MonoBehaviour
    {
        [Header("绑定")]
        [Tooltip("武器挂点。留空则自动在本对象下找名为 WeaponSocket 的子物体")]
        [SerializeField] private Transform socket;

        [Tooltip("武器相机。留空则自动创建")]
        [SerializeField] private Camera weaponCamera;

        [Header("武器相机")]
        [SerializeField] private string viewmodelLayerName = "Viewmodel";

        [Tooltip("武器相机固定视野，不随开镜变化，枪形才不会被拉伸")]
        [SerializeField] private float weaponFov = 60f;

        [Tooltip("极小的近裁剪面，枪贴到脸上也不会被裁掉")]
        [SerializeField] private float weaponNear = 0.01f;

        [SerializeField] private float weaponFar = 5f;

        [Header("腰射姿态")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.22f, -0.20f, 0.35f);
        [SerializeField] private Vector3 hipEuler = Vector3.zero;

        [Header("开镜姿态")]
        [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.10f, 0.25f);
        [SerializeField] private Vector3 adsEuler = Vector3.zero;

        [Header("过渡")]
        [Tooltip("腰射 ↔ 开镜的过渡时长，秒")]
        [SerializeField] private float adsTransition = 0.12f;

        [Header("调试")]
        [Tooltip("勾选后把武器相机与图层配置的细节打到控制台")]
        [SerializeField] private bool debugLogging = false;

        private Camera mainCamera;
        private int viewmodelLayer = -1;
        private bool ready;

        // ---------------- 生命周期 ----------------

        void Awake()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("[WeaponRig] 本组件要挂在主相机（Main Camera）上。", this);
                enabled = false;
                return;
            }

            ResolveSocket();
            EnsureWeaponCamera();
            ConfigureCameras();
            ApplyLayerToWeapons();

            ready = socket != null;
        }

        void Start()
        {
            if (socket == null) return;

            socket.localPosition = hipPosition;
            socket.localRotation = Quaternion.Euler(hipEuler);
        }

        void Update()
        {
            if (ready) UpdateSocketPose();
        }

        // ---------------- 挂点姿态 ----------------

        private void UpdateSocketPose()
        {
            bool aiming = PlayerInput.Aiming;

            Vector3 targetPosition = aiming ? adsPosition : hipPosition;
            Quaternion targetRotation = Quaternion.Euler(aiming ? adsEuler : hipEuler);

            float t = adsTransition > 0.0001f
                ? 1f - Mathf.Exp(-Time.deltaTime / adsTransition)
                : 1f;

            socket.localPosition = Vector3.Lerp(socket.localPosition, targetPosition, t);
            socket.localRotation = Quaternion.Slerp(socket.localRotation, targetRotation, t);
        }

        // ---------------- 装配 ----------------

        private void ResolveSocket()
        {
            if (socket == null)
            {
                Transform found = transform.Find("WeaponSocket");
                if (found != null) socket = found;
            }

            if (socket == null)
            {
                Debug.LogWarning(
                    "[WeaponRig] 没找到武器挂点。请在本对象下创建空对象「WeaponSocket」，" +
                    "并把武器模型放进去。", this);
            }
        }

        private void EnsureWeaponCamera()
        {
            if (weaponCamera != null)
            {
                if (weaponCamera.transform.parent != transform)
                    weaponCamera.transform.SetParent(transform, false);
                return;
            }

            GameObject host = new GameObject("WeaponCamera");
            host.transform.SetParent(transform, false);
            host.transform.localPosition = Vector3.zero;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one;

            weaponCamera = host.AddComponent<Camera>();
            // 注意：AddComponent<Camera> 不会附带音频监听器，这是我们要的（场景只需一个）
        }

        private void ConfigureCameras()
        {
            viewmodelLayer = LayerMask.NameToLayer(viewmodelLayerName);
            if (viewmodelLayer < 0)
            {
                Debug.LogWarning(
                    $"[WeaponRig] 找不到图层「{viewmodelLayerName}」，武器会插进墙里。\n" +
                    "请执行菜单「禁区协议 → 创建 Viewmodel 图层」。", this);
                return;
            }

            int mask = 1 << viewmodelLayer;

            // 主相机不画武器，武器相机只画武器
            mainCamera.cullingMask &= ~mask;
            weaponCamera.cullingMask = mask;

            weaponCamera.fieldOfView = weaponFov;
            weaponCamera.nearClipPlane = weaponNear;
            weaponCamera.farClipPlane = weaponFar;
            weaponCamera.clearFlags = CameraClearFlags.Depth;
            weaponCamera.depth = mainCamera.depth + 1;
            weaponCamera.allowHDR = mainCamera.allowHDR;
            weaponCamera.allowMSAA = mainCamera.allowMSAA;
            weaponCamera.useOcclusionCulling = false;

            // URP：武器相机设为叠加相机，并挂进主相机的渲染堆叠
            UniversalAdditionalCameraData mainData = mainCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData weaponData = weaponCamera.GetUniversalAdditionalCameraData();

            if (mainData != null && weaponData != null)
            {
                weaponData.renderType = CameraRenderType.Overlay;

                if (!mainData.cameraStack.Contains(weaponCamera))
                    mainData.cameraStack.Add(weaponCamera);
            }

            if (debugLogging)
            {
                Debug.Log(
                    $"[WeaponRig] 配置完成 —— 武器层：{viewmodelLayerName}({viewmodelLayer})，" +
                    $"武器相机 FOV：{weaponFov}，主相机剔除掩码已更新。");
            }
        }

        private void ApplyLayerToWeapons()
        {
            if (viewmodelLayer < 0 || socket == null) return;

            foreach (Transform child in socket)
                SetLayerRecursive(child.gameObject, viewmodelLayer);
        }

        private static void SetLayerRecursive(GameObject target, int layer)
        {
            target.layer = layer;

            foreach (Transform child in target.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        // ---------------- 外部接口 ----------------

        /// <summary>武器挂点（武器模型的父节点）</summary>
        public Transform Socket => socket;

        /// <summary>武器相机</summary>
        public Camera WeaponCamera => weaponCamera;

        /// <summary>立刻跳到腰射姿态，不做过渡（出生、切枪时用）</summary>
        public void SnapToHip()
        {
            if (socket == null) return;
            socket.localPosition = hipPosition;
            socket.localRotation = Quaternion.Euler(hipEuler);
        }
    }
}
