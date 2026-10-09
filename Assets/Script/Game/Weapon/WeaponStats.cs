using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// 武器参数表。
    ///
    /// 数值来源：设计文档「枪械系统 / 武器基础属性 / 突击步枪 / AKM.md」。
    /// 后坐力部分原本写作 0.03（弧度），这里统一换算成**度**方便在检查器里直观调整
    /// （0.03 rad ≈ 1.72°）。
    /// </summary>
    [System.Serializable]
    public class WeaponStats
    {
        [Header("弹药")]
        [Tooltip("口径，仅作展示")]
        public string caliber = "7.62×39mm";

        public int magazineSize = 30;
        public int spareAmmo = 90;

        [Header("射速")]
        [Tooltip("全自动射击间隔（秒），0.08s = 750 RPM")]
        public float autoInterval = 0.08f;

        [Tooltip("半自动射击间隔（秒），0.15s = 400 RPM")]
        public float semiInterval = 0.15f;

        [Header("后坐力")]
        [Tooltip("每发基础后坐力（度）。设计文档 0.03 rad ≈ 1.72°")]
        public float recoilKick = 1.72f;

        [Tooltip("垂直方向占比，其余分给水平随机抖动")]
        [Range(0f, 1f)] public float verticalShare = 0.72f;

        [Tooltip("水平抖动方向的变化速度，越小越连贯、越大越乱")]
        [Range(0f, 1f)] public float horizontalWander = 0.25f;

        [Tooltip("蹲下时后坐力乘数")]
        public float crouchRecoilMult = 0.5f;

        [Tooltip("开镜时后坐力乘数")]
        public float adsRecoilMult = 0.7f;

        [Tooltip("后坐力恢复速度（度/秒），越大回正越快")]
        public float recoilRecovery = 8f;

        [Header("散射（下一步接入）")]
        public float standSpreadDeg = 3.0f;
        public float crouchSpreadMult = 0.35f;
        public float adsSpreadMult = 0.35f;
        public float moveSpreadMult = 1.6f;
        public float vertSpreadMult = 0.8f;
        public float bloomPerShot = 0.15f;
        public float bloomRecovery = 0.6f;
        public float bloomMax = 10f;

        [Header("伤害（下一步接入）")]
        public float baseDamage = 25f;
        public float headshotMultStanding = 1.75f;
        public float headshotMultCrouching = 2.0f;
    }
}
