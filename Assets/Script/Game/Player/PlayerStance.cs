using System;
using UnityEngine;

namespace Game.Player
{
    public enum StanceType
    {
        Stand,
        Crouch,
        Prone
    }

    /// <summary>
    /// 姿态状态机：站 / 蹲 / 趴。
    ///
    /// 职责：决定当前姿态、向 Motor 提交碰撞体尺寸、提供视高与移动速度上限、驱动模型翻转。
    /// 蹲键 C（按住），趴键 Z（按住），优先级：趴 &gt; 蹲 &gt; 站。
    /// 仅在地面可切换姿态；站立前会做顶头检测，头顶被压住时维持低姿态。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStance : MonoBehaviour
    {
        [System.Serializable]
        public struct StanceProfile
        {
            [Tooltip("碰撞体总高")] public float height;
            [Tooltip("碰撞体半径")] public float radius;
            [Tooltip("眼睛相对脚底的高度")] public float eyeHeight;
            [Tooltip("姿态限速，填 0 表示不限速（站立由移动逻辑决定走/跑）")] public float maxSpeed;

            public StanceProfile(float h, float r, float e, float s)
            {
                height = h; radius = r; eyeHeight = e; maxSpeed = s;
            }
        }

        [Header("姿态配置")]
        [SerializeField] private StanceProfile standProfile  = new StanceProfile(1.8f, 0.35f, 1.6f, 0f);
        [SerializeField] private StanceProfile crouchProfile = new StanceProfile(1.0f, 0.35f, 0.8f, 3.0f);
        [SerializeField] private StanceProfile proneProfile  = new StanceProfile(0.45f, 0.20f, 0.25f, 1.5f);

        [Header("视觉过渡")]
        [SerializeField] private float eyeTransition = 0.35f;
        [SerializeField] private float modelRotationSpeed = 8f;

        [Header("绑定")]
        [SerializeField] private Transform model;

        public StanceType Current { get; private set; } = StanceType.Stand;

        /// <summary>当前平滑后的视高（相对脚底）</summary>
        public float EyeHeight { get; private set; }

        public bool CanJump => Current == StanceType.Stand;
        public bool CanSprint => Current == StanceType.Stand;

        public event Action<StanceType> OnStanceChanged;

        private PlayerMotor motor;
        private Quaternion targetModelRotation = Quaternion.identity;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            if (model == null)
            {
                Transform found = transform.Find("Model");
                if (found != null) model = found;
            }

            EyeHeight = standProfile.eyeHeight;
        }

        // 延迟到 Start：确保 Motor 的 Awake 已经完成，脚底基准才算得准
        void Start()
        {
            ApplyDimensions(standProfile, true);
        }

        void Update()
        {
            UpdateCurrentStance();
            UpdateEye();
            UpdateModel();
        }

        private void UpdateCurrentStance()
        {
            StanceType desired = Current;

            // 只有站在地面上才能切换姿态
            if (motor == null || motor.IsGrounded)
            {
                if (PlayerInput.ProneHeld) desired = StanceType.Prone;
                else if (PlayerInput.CrouchHeld) desired = StanceType.Crouch;
                else desired = StanceType.Stand;
            }

            // 想要长高时必须确认头顶留得出空间
            if (desired != Current && HeightRank(desired) < HeightRank(Current))
            {
                StanceProfile target = ProfileOf(desired);
                if (motor != null && !motor.CanExpandTo(target.height, target.radius))
                    return;         // 被天花板压住，维持现状
            }

            if (desired != Current)
            {
                Current = desired;
                ApplyDimensions(ProfileOf(desired), false);
                OnStanceChanged?.Invoke(Current);
            }
        }

        private void UpdateEye()
        {
            float target = ProfileOf(Current).eyeHeight;
            if (eyeTransition <= 0f)
            {
                EyeHeight = target;
                return;
            }
            EyeHeight = Mathf.Lerp(EyeHeight, target, 1f - Mathf.Exp(-Time.deltaTime / eyeTransition));
        }

        private void UpdateModel()
        {
            if (model == null) return;

            // 趴下：模型整体前倾倒地，而镜头角度保持不动
            targetModelRotation = Current == StanceType.Prone
                ? Quaternion.Euler(-90f, 0f, 0f)
                : Quaternion.identity;

            model.localRotation = Quaternion.Slerp(
                model.localRotation, targetModelRotation,
                1f - Mathf.Exp(-Time.deltaTime * modelRotationSpeed));
        }

        /// <summary>把姿态的速度上限施加到上层算出来的目标速度上</summary>
        public float ClampSpeed(float desiredSpeed)
        {
            float cap = ProfileOf(Current).maxSpeed;
            return cap > 0f ? Mathf.Min(desiredSpeed, cap) : desiredSpeed;
        }

        private void ApplyDimensions(StanceProfile profile, bool immediate)
        {
            if (immediate)
                EyeHeight = profile.eyeHeight;

            if (motor == null) return;
            motor.SetDimensions(profile.height, profile.radius);
        }

        private StanceProfile ProfileOf(StanceType type)
        {
            switch (type)
            {
                case StanceType.Crouch: return crouchProfile;
                case StanceType.Prone:  return proneProfile;
                default:                return standProfile;
            }
        }

        private static int HeightRank(StanceType type)
        {
            switch (type)
            {
                case StanceType.Prone:  return 0;
                case StanceType.Crouch: return 1;
                default:                return 2;
            }
        }
    }
}
