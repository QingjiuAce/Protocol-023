using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 视角表现层：探头、开镜视野、走路晃动。
    ///
    /// 本脚本只「算」数据，不直接写任何 Transform —— 相机的位置与朝向统一由 PlayerCamera 落笔，
    /// 避免多个脚本抢着改同一个 Transform 而产生的抖动。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerView : MonoBehaviour
    {
        [Header("探头 Q/E")]
        [SerializeField] private float peekTiltDegrees = 15f;
        [SerializeField] private float peekDistance = 0.3f;
        [SerializeField] private float peekTransition = 0.25f;

        [Header("视野")]
        [SerializeField] private float hipFov = 75f;
        [SerializeField] private float adsFov = 50f;
        [SerializeField] private float adsTransition = 0.12f;

        [Header("走路晃动")]
        [SerializeField] private float referenceSpeed = 5f;
        [Tooltip("步频：每秒上下的次数")]
        [SerializeField] private float bobFrequency = 1.4f;
        [SerializeField] private float bobVertical = 0.015f;
        [SerializeField] private float bobHorizontal = 0.0075f;
        [SerializeField] private float bobDepth = 0.005f;
        [SerializeField] private float swayAmount = 0.03f;
        [SerializeField] private float sprintBobMultiplier = 1.5f;
        [SerializeField] private float adsBobMultiplier = 0.75f;
        [SerializeField] private float bobFade = 0.15f;

        private PlayerStance stance;
        private PlayerLocomotion locomotion;

        private float peekOffset;
        private float peekRoll;
        private float bobTimer;
        private float bobWeight;
        private float fieldOfView;

        // ---------------- 输出数据（供 PlayerCamera 使用） ----------------

        /// <summary>相机相对玩家脚底的眼高</summary>
        public float EyeHeight { get; private set; }

        /// <summary>相机本地空间偏移（探头位移 + 走路晃动），需按 yaw 旋转后使用</summary>
        public Vector3 LocalOffset { get; private set; }

        /// <summary>屏幕倾斜角（探头），单位度</summary>
        public float Roll { get; private set; }

        /// <summary>走路摇摆角，单位度</summary>
        public float Sway { get; private set; }

        /// <summary>目标视野</summary>
        public float FieldOfView => fieldOfView;

        void Awake()
        {
            stance = GetComponent<PlayerStance>();
            locomotion = GetComponent<PlayerLocomotion>();
            fieldOfView = hipFov;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateEyeHeight();
            UpdatePeek(dt);
            UpdateBob(dt);
            UpdateFieldOfView(dt);
        }

        private void UpdateEyeHeight()
        {
            EyeHeight = stance != null ? stance.EyeHeight : 1.6f;
        }

        private void UpdatePeek(float dt)
        {
            bool left = PlayerInput.PeekLeft;
            bool right = PlayerInput.PeekRight;

            float targetOffset = 0f;
            float targetRoll = 0f;

            if (left && !right)
            {
                targetOffset = -peekDistance;
                targetRoll = peekTiltDegrees;
            }
            else if (right && !left)
            {
                targetOffset = peekDistance;
                targetRoll = -peekTiltDegrees;
            }

            float t = peekTransition > 0f
                ? 1f - Mathf.Exp(-dt / peekTransition)
                : 1f;

            peekOffset = Mathf.Lerp(peekOffset, targetOffset, t);
            peekRoll = Mathf.Lerp(peekRoll, targetRoll, t);
        }

        private void UpdateBob(float dt)
        {
            float speed = locomotion != null ? locomotion.HorizontalSpeed : 0f;
            bool grounded = locomotion == null || locomotion.Grounded;
            bool aiming = PlayerInput.Aiming;
            bool sprinting = locomotion != null && locomotion.Sprinting;

            float factor = referenceSpeed > 0f ? Mathf.Clamp01(speed / referenceSpeed) : 0f;
            float active = grounded && factor > 0.05f ? factor : 0f;

            float fade = bobFade > 0f ? 1f - Mathf.Exp(-dt / bobFade) : 1f;
            bobWeight = Mathf.Lerp(bobWeight, active, fade);

            // 步频随速度递增：走得越快，上下越快
            bobTimer += dt * bobFrequency * bobWeight;

            float multiplier = bobWeight;
            if (aiming) multiplier *= adsBobMultiplier;
            else if (sprinting) multiplier *= sprintBobMultiplier;

            float phase = bobTimer * Mathf.PI * 2f;
            float halfPhase = phase * 0.5f;

            float vertical = Mathf.Sin(phase) * bobVertical * multiplier;
            float horizontal = Mathf.Sin(halfPhase) * bobHorizontal * multiplier;
            float depth = Mathf.Cos(phase) * bobDepth * multiplier;

            LocalOffset = new Vector3(peekOffset + horizontal, vertical, depth);
            Roll = peekRoll;
            // swayAmount 按弧度标定（与游戏设定一致），转成度交给 Quaternion.Euler
            Sway = Mathf.Sin(halfPhase) * swayAmount * multiplier * Mathf.Rad2Deg;
        }

        private void UpdateFieldOfView(float dt)
        {
            float target = PlayerInput.Aiming ? adsFov : hipFov;
            float t = adsTransition > 0f ? 1f - Mathf.Exp(-dt / adsTransition) : 1f;
            fieldOfView = Mathf.Lerp(fieldOfView, target, t);
        }
    }
}
