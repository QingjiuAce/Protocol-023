using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// 后坐力状态：累积每次射击带来的视角偏移，并在停火后自动回正。
    ///
    /// 挂在 CameraRig 上。本组件**只维护数值、不碰相机** ——
    /// 真正的旋转由 PlayerCamera 在合成视角时读取并应用，
    /// 这样不依赖脚本执行顺序，也不会出现旋转被重复叠加导致的歪斜。
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponRecoil : MonoBehaviour
    {
        [Header("上限")]
        [Tooltip("垂直上跳的最大角度（度）")]
        [SerializeField] private float maxPitch = 24f;

        [Tooltip("水平偏移的最大角度（度）")]
        [SerializeField] private float maxYaw = 14f;

        [Tooltip("如果发现枪口往下跳（方向反了），勾选这个翻转垂直方向")]
        [SerializeField] private bool invertVertical = false;

        [Header("状态（只读）")]
        [SerializeField] private float pitchOffset;

        [SerializeField] private float yawOffset;

        private float recoverySpeed = 8f;
        private float horizontalDirection;

        /// <summary>当前垂直后坐力偏移（度，正值为向上）</summary>
        public float PitchOffset => pitchOffset;

        /// <summary>当前水平后坐力偏移（度，正值为向右）</summary>
        public float YawOffset => yawOffset;

        /// <summary>供相机合成用的垂直角度（已含方向修正）</summary>
        public float AppliedPitch => invertVertical ? pitchOffset : -pitchOffset;

        /// <summary>是否还有后坐力没回正</summary>
        public bool IsActive => Mathf.Abs(pitchOffset) > 0.001f || Mathf.Abs(yawOffset) > 0.001f;

        /// <summary>
        /// 累加一次后坐力。
        /// </summary>
        /// <param name="kickDegrees">总后坐力（度），调用方已按姿态乘过系数</param>
        /// <param name="verticalShare">垂直占比 0~1，余下分给水平随机抖动</param>
        /// <param name="wander">水平方向的变化速度，越小越连贯</param>
        /// <param name="recovery">回正速度（度/秒）</param>
        public void AddRecoil(float kickDegrees, float verticalShare, float wander, float recovery)
        {
            if (kickDegrees <= 0f) return;

            recoverySpeed = Mathf.Max(recovery, 0.1f);
            verticalShare = Mathf.Clamp01(verticalShare);

            // 垂直：以向上为主，带一点自然起伏
            float vertical = kickDegrees * verticalShare * Random.Range(0.9f, 1.15f);
            pitchOffset = Mathf.Min(pitchOffset + vertical, maxPitch);

            // 水平：方向小幅游走，避免每发左右乱跳
            horizontalDirection = Mathf.Lerp(horizontalDirection, Random.Range(-1f, 1f), Mathf.Clamp01(wander));
            float horizontal = kickDegrees * (1f - verticalShare) * horizontalDirection;
            yawOffset = Mathf.Clamp(yawOffset + horizontal, -maxYaw, maxYaw);
        }

        /// <summary>立刻清空后坐力（切枪、重生时用）</summary>
        public void ResetRecoil()
        {
            pitchOffset = 0f;
            yawOffset = 0f;
            horizontalDirection = 0f;
        }

        void Update()
        {
            if (!IsActive) return;

            // 自动回正
            float step = recoverySpeed * Time.deltaTime;
            pitchOffset = Mathf.MoveTowards(pitchOffset, 0f, step);
            yawOffset = Mathf.MoveTowards(yawOffset, 0f, step);
        }
    }
}
