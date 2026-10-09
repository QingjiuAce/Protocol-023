using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 玩家输入的唯一入口。所有键位集中在这里定义，
    /// 将来切换到 Input System（新版输入）时只改本文件，其他脚本不受影响。
    /// </summary>
    public static class PlayerInput
    {
        /// <summary>跳跃输入缓冲时长：落地前提早按跳也能生效，让手感更宽容</summary>
        public const float JumpBufferTime = 0.15f;

        private static float jumpBuffer;

        /// <summary>
        /// 由 UI 层接管：打开面板或菜单时置为 true，所有游戏输入立即失效
        /// （角色停下、视角冻结、按键无响应），关闭后自动恢复。
        /// </summary>
        public static bool Blocked { get; set; }

        // ---------------- 持续型输入 ----------------

        /// <summary>移动轴（x=左右，y=前后），未做归一化</summary>
        public static Vector2 MoveAxis => Blocked
            ? Vector2.zero
            : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        /// <summary>鼠标位移（每帧一次，直接来自 Unity 鼠标轴）</summary>
        public static Vector2 LookDelta => Blocked
            ? Vector2.zero
            : new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        public static bool Sprinting    => !Blocked && Input.GetKey(KeyCode.LeftShift);
        public static bool CrouchHeld   => !Blocked && Input.GetKey(KeyCode.C);
        public static bool ProneHeld    => !Blocked && Input.GetKey(KeyCode.Z);
        public static bool Aiming       => !Blocked && Input.GetMouseButton(1);
        public static bool PeekLeft     => !Blocked && Input.GetKey(KeyCode.Q);
        public static bool PeekRight    => !Blocked && Input.GetKey(KeyCode.E);
        public static bool InteractKey  => !Blocked && Input.GetKeyDown(KeyCode.F);

        /// <summary>开火键按住（左键），全自动用</summary>
        public static bool FireHeld => !Blocked && Input.GetMouseButton(0);

        /// <summary>开火键按下瞬间（左键），半自动用</summary>
        public static bool FirePressed => !Blocked && Input.GetMouseButtonDown(0);

        // ---------------- 边沿型输入（需每帧 Tick 驱动） ----------------

        /// <summary>
        /// 在 Update 中调用以刷新一次性的输入状态。
        /// 由 PlayerLocomotion 每帧驱动，无需额外挂载任何组件。
        /// </summary>
        public static void Tick()
        {
            if (Blocked)
            {
                jumpBuffer = 0f;        // UI 接管期间清空输入缓冲，避免关面板瞬间蹿一下
                return;
            }

            if (Input.GetButtonDown("Jump"))
                jumpBuffer = JumpBufferTime;
            else if (jumpBuffer > 0f)
                jumpBuffer = Mathf.Max(jumpBuffer - Time.deltaTime, 0f);
        }

        /// <summary>取出一次跳跃请求（取走即清空）</summary>
        public static bool ConsumeJump()
        {
            if (jumpBuffer <= 0f) return false;
            jumpBuffer = 0f;
            return true;
        }
    }
}
