using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 移动业务逻辑层：把输入翻译成速度，交给 PlayerMotor 去走完位移。
    ///
    /// 游戏设定的惯性时序：
    ///   1) 移动键按下后立刻起步，0.5 秒内爬到走路满速
    ///   2) 冲刺键 Shift 按住满 0.5 秒，才把目标速度抬到奔跑
    ///   3) 进入奔跑后再用 0.5 秒冲到奔跑满速
    ///   4) 松开方向键则在 0.25 秒内利索刹停
    ///   5) 离地后水平和加速度全部失效，水平速度锁定为起跳瞬间的数值
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [DisallowMultipleComponent]
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("移动速度（游戏设定）")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float adsSpeed = 4f;
        [SerializeField] private float peekSpeed = 3f;

        [Header("惯性时序")]
        [Tooltip("走路加速度：0 → 走路满速用 0.5 秒")]
        [SerializeField] private float walkAccel = 10f;
        [Tooltip("冲刺启动延迟：Shift 按住满这么久才开始加速到奔跑")]
        [SerializeField] private float sprintDelay = 0.5f;
        [Tooltip("奔跑加速度：从走路满速到奔跑满速用 0.5 秒")]
        [SerializeField] private float sprintAccel = 6f;
        [Tooltip("制动加速度：从奔跑满速到静止用 0.25 秒")]
        [SerializeField] private float brakeDecel = 32f;

        [Header("跳跃与重力")]
        [SerializeField] private float jumpSpeed = 5f;
        [Tooltip("奔跑中起跳的初速倍率（游戏设定：跑跳更有冲力）")]
        [SerializeField] private float sprintJumpBoost = 1.5f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float maxFallSpeed = 40f;

        [Header("视角转向")]
        [SerializeField] private float yawSensitivity = 1.72f;
        [SerializeField] private bool lockCursorOnStart = true;

        private PlayerMotor motor;
        private PlayerStance stance;

        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool wasGrounded;
        private float sprintHeldTime;

        // ---------------- 对外状态 ----------------

        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public float VerticalSpeed => verticalVelocity;
        public bool Sprinting { get; private set; }
        public bool Grounded => motor != null && motor.IsGrounded;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            stance = GetComponent<PlayerStance>();

            // 起始锁定光标。后续的光标释放/锁定统一由 UI 层（UIHub）接管，
            // 玩家侧不再自行处理，避免两边抢着改光标状态。
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Update()
        {
            PlayerInput.Tick();
            TurnBody();
        }

        void FixedUpdate()
        {
            if (motor == null) return;

            float dt = Time.fixedDeltaTime;
            motor.EvaluateGround();
            bool grounded = motor.IsGrounded;

            if (grounded)
            {
                if (!wasGrounded) verticalVelocity = Mathf.Min(verticalVelocity, 0f);
                StepOnGround(dt);
            }
            else
            {
                StepInAir(dt);
            }

            wasGrounded = grounded;

            Vector3 step = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z) * dt;
            motor.Move(step);
        }

        // ---------------- 转向 ----------------

        /// <summary>鼠标水平移动驱动身体转动；因为是纯运动学，转向不受任何物理干预</summary>
        private void TurnBody()
        {
            float delta = PlayerInput.LookDelta.x;
            if (Mathf.Abs(delta) < 1e-6f) return;

            // 乘上设置里的鼠标灵敏度倍率（默认 1.00，可在设置菜单里调）
            transform.Rotate(0f, delta * yawSensitivity * GameSettings.MouseSensitivity, 0f, Space.World);
        }

        // ---------------- 地面 ----------------

        private void StepOnGround(float dt)
        {
            Vector2 axis = PlayerInput.MoveAxis;
            Vector3 wish = transform.right * axis.x + transform.forward * axis.y;
            bool hasInput = wish.sqrMagnitude > 0.01f;
            if (hasInput) wish.Normalize();

            bool aiming = PlayerInput.Aiming;
            bool peeking = PlayerInput.PeekLeft || PlayerInput.PeekRight;

            float targetSpeed = aiming ? adsSpeed : walkSpeed;
            if (peeking) targetSpeed = Mathf.Min(targetSpeed, peekSpeed);

            // 冲刺：Shift 需要按住满 sprintDelay 才真正开始冲
            bool allowedToSprint = stance == null || stance.CanSprint;
            bool sprintHeld = PlayerInput.Sprinting && hasInput && allowedToSprint && !aiming && !peeking;

            sprintHeldTime = sprintHeld ? sprintHeldTime + dt : 0f;
            Sprinting = sprintHeldTime >= sprintDelay;
            if (Sprinting) targetSpeed = sprintSpeed;

            if (stance != null) targetSpeed = stance.ClampSpeed(targetSpeed);

            if (hasInput)
            {
                float accel = Sprinting ? sprintAccel : walkAccel;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wish * targetSpeed, accel * dt);
            }
            else
            {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, brakeDecel * dt);
            }

            // 起跳：跳不了时不消费输入缓冲，让玩家松姿后仍能补上这一跳
            bool canJump = stance == null || stance.CanJump;
            if (canJump && PlayerInput.ConsumeJump())
            {
                verticalVelocity = jumpSpeed * (Sprinting ? sprintJumpBoost : 1f);
            }
            else if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;     // 持续轻压，配合地面吸附防止抖动悬空
            }
        }

        // ---------------- 空中 ----------------

        private void StepInAir(float dt)
        {
            // 水平速度保持离地瞬间的数值，输入不参与任何改变
            sprintHeldTime = 0f;
            Sprinting = false;

            verticalVelocity -= gravity * dt;
            if (verticalVelocity < -maxFallSpeed)
                verticalVelocity = -maxFallSpeed;
        }

        // ---------------- 外部作用 ----------------

        /// <summary>
        /// 注入外部冲量（爆炸击退、坠落缓冲等）。
        /// 运动学方案不被物理引擎推动，需要外力时必须显式调用这里。
        /// </summary>
        public void AddImpulse(Vector3 impulse)
        {
            horizontalVelocity += new Vector3(impulse.x, 0f, impulse.z);
            verticalVelocity += impulse.y;
        }
    }
}
