using Game.Core;
using Game.Weapon;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 相机组：负责俯仰、跟随、把各种视角效果落到真正的相机上。
    ///
    /// 本组件挂在**独立的** CameraRig 对象上（不要作为玩家的子物体），
    /// 在 LateUpdate 里合成最终位姿 —— 这样移动系统的任何抖动都不会传染给画面。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCamera : MonoBehaviour
    {
        [Header("绑定")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerView view;
        [SerializeField] private Camera ownedCamera;

        [Header("俯仰")]
        [SerializeField] private float pitchSensitivity = 1.72f;
        [SerializeField] private float pitchLimit = 80f;
        [SerializeField] private bool invertPitch = false;

        [Header("后坐力")]
        [Tooltip("留空则自动在场景里找 WeaponRecoil")]
        [SerializeField] private WeaponRecoil recoil;

        [Header("跟随")]
        [Tooltip("更小 = 贴得更紧；0 表示完全不平滑")]
        [SerializeField] private float positionSmooth = 0.03f;
        [SerializeField] private float fallbackHeight = 1.8f;
        [SerializeField] private float fallbackEyeHeight = 1.6f;

        private float pitch;
        private Vector3 followVelocity;
        private bool placed;

        void Awake()
        {
            if (ownedCamera == null)
                ownedCamera = GetComponentInChildren<Camera>();
            if (ownedCamera == null)
                ownedCamera = GetComponent<Camera>();

            if (target == null)
            {
                PlayerLocomotion foundLocomotion = FindObjectOfType<PlayerLocomotion>();
                if (foundLocomotion != null) target = foundLocomotion.transform;
            }
            if (motor == null && target != null)
                motor = target.GetComponent<PlayerMotor>();
            if (view == null && target != null)
                view = target.GetComponent<PlayerView>();

            if (recoil == null) recoil = GetComponent<WeaponRecoil>();
            if (recoil == null) recoil = FindObjectOfType<WeaponRecoil>();

            if (target == null)
                Debug.LogError("[PlayerCamera] 没有指定跟随目标，请把玩家对象拖到 target 栏。", this);
        }

        void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float delta = PlayerInput.LookDelta.y;
            if (Mathf.Abs(delta) < 1e-6f) return;

            // 乘上设置里的鼠标灵敏度倍率（与水平转向使用同一个值）
            pitch += (invertPitch ? delta : -delta) * pitchSensitivity * GameSettings.MouseSensitivity;
            pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
        }

        void LateUpdate()
        {
            if (target == null) return;

            Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);

            float footOffset = motor != null ? motor.FootOffsetY : -fallbackHeight * 0.5f;
            float eyeHeight = view != null ? view.EyeHeight : fallbackEyeHeight;
            Vector3 localOffset = view != null ? view.LocalOffset : Vector3.zero;

            Vector3 anchor = target.position + Vector3.up * (footOffset + eyeHeight);
            Vector3 desired = anchor + yaw * localOffset;

            if (!placed || positionSmooth <= 0f)
            {
                transform.position = desired;
                followVelocity = Vector3.zero;
                placed = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position, desired, ref followVelocity, positionSmooth);
            }

            Quaternion lean = view != null
                ? Quaternion.Euler(0f, view.Sway, view.Roll)
                : Quaternion.identity;

            // 后坐力：由 WeaponRecoil 提供数值，这里在「俯仰之后、倾斜之前」合成，
            // 保证后坐力只影响 pitch/yaw 两个轴，绝不给画面带来额外的滚转
            Quaternion recoilRotation = recoil != null
                ? Quaternion.Euler(recoil.AppliedPitch, recoil.YawOffset, 0f)
                : Quaternion.identity;

            transform.rotation = yaw * Quaternion.Euler(pitch, 0f, 0f) * recoilRotation * lean;

            if (ownedCamera != null && view != null)
                ownedCamera.fieldOfView = view.FieldOfView;
        }
    }
}
