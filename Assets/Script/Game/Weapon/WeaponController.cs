using Game.Player;
using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// 武器开火逻辑：射速节奏、弹药消耗、后坐力触发。
    ///
    /// 挂在武器模型上（与 WeaponInstance 同一对象）。
    /// 弹道射线、伤害结算、枪口特效留到下一步 —— 现在只把「开火」这件事本身做扎实。
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponController : MonoBehaviour
    {
        [Header("绑定")]
        [SerializeField] private WeaponInstance weapon;

        [Tooltip("留空则自动在场景里找 WeaponRecoil")]
        [SerializeField] private WeaponRecoil recoil;

        [Header("射击模式")]
        [Tooltip("勾选 = 全自动，取消 = 半自动")]
        [SerializeField] private bool automatic = true;

        [Header("调试")]
        [Tooltip("按 R 直接装满弹匣（换弹动画做好之前先用这个）")]
        [SerializeField] private bool allowTestReload = true;

        [Tooltip("每次开火在控制台打印弹药与后坐力")]
        [SerializeField] private bool debugLogging = false;

        private PlayerStance stance;
        private float nextFireTime;
        private int currentAmmo;

        // ---------------- 对外状态 ----------------

        public int CurrentAmmo => currentAmmo;
        public int MagazineSize => Stats != null ? Stats.magazineSize : 0;
        public WeaponStats Stats => weapon != null ? weapon.Stats : null;

        /// <summary>射击模式，切枪或按 B 键可改</summary>
        public bool Automatic
        {
            get => automatic;
            set => automatic = value;
        }

        // ---------------- 生命周期 ----------------

        void Awake()
        {
            if (weapon == null) weapon = GetComponent<WeaponInstance>();
            if (recoil == null) recoil = FindObjectOfType<WeaponRecoil>();

            stance = FindObjectOfType<PlayerStance>();

            currentAmmo = MagazineSize;

            if (weapon == null)
                Debug.LogWarning("[WeaponController] 同对象上没有 Weapon Instance 组件，无法读取武器参数。", this);
        }

        /// <summary>装备这把武器时把弹匣补满（切枪回满，暂不记持久弹药）</summary>
        void OnEnable()
        {
            if (currentAmmo <= 0) currentAmmo = MagazineSize;
        }

        void Update()
        {
            if (weapon == null) return;
            if (!gameObject.activeInHierarchy) return;
            if (PlayerInput.Blocked) return;

            if (allowTestReload && Input.GetKeyDown(KeyCode.R))
            {
                currentAmmo = MagazineSize;
                if (debugLogging) Debug.Log($"[武器] 弹匣已装满：{currentAmmo}/{MagazineSize}");
                return;
            }

            bool wantsFire = automatic ? PlayerInput.FireHeld : PlayerInput.FirePressed;
            if (wantsFire && Time.time >= nextFireTime)
                Fire();
        }

        // ---------------- 开火 ----------------

        private void Fire()
        {
            WeaponStats stats = Stats;
            if (stats == null) return;

            if (currentAmmo <= 0)
            {
                nextFireTime = Time.time + 0.25f;   // 空仓时的轻微顿挫
                return;
            }

            currentAmmo--;

            float interval = automatic ? stats.autoInterval : stats.semiInterval;
            nextFireTime = Time.time + Mathf.Max(interval, 0.01f);

            ApplyRecoil(stats);

            if (debugLogging)
                Debug.Log($"[开火] 剩余 {currentAmmo}/{stats.magazineSize}；" +
                    $"后坐力累积 上跳 {recoil?.PitchOffset:F2}° 水平 {recoil?.YawOffset:F2}°");
        }

        /// <summary>按姿态把后坐力施加到相机上</summary>
        private void ApplyRecoil(WeaponStats stats)
        {
            if (recoil == null) return;

            float multiplier = 1f;

            // 蹲下 / 趴下都算低姿态，后坐力减半
            bool lowStance = stance != null && stance.Current != StanceType.Stand;
            if (lowStance) multiplier *= stats.crouchRecoilMult;

            // 开镜再降 30%
            if (PlayerInput.Aiming) multiplier *= stats.adsRecoilMult;

            recoil.AddRecoil(
                stats.recoilKick * multiplier,
                stats.verticalShare,
                stats.horizontalWander,
                stats.recoilRecovery);
        }

        // ---------------- 外部接口 ----------------

        /// <summary>切换全自动 / 半自动</summary>
        public void ToggleFireMode()
        {
            automatic = !automatic;
        }
    }
}
