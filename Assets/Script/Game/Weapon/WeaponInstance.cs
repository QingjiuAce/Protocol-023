using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// 武器实例标记：挂在武器挂点下的每个武器根对象上。
    ///
    /// WeaponHolder 靠它建立「武器 ID → 模型」的索引，
    /// 捡到对应物品时就把这把武器点亮。
    /// 一个挂点下可以预先摆好几把枪，全部隐藏，谁被捡到就显示谁。
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponInstance : MonoBehaviour
    {
        [Tooltip("武器 ID，必须和地上的拾取物 item Id 完全一致（例如 akm）")]
        [SerializeField] private string weaponId = "akm";

        [Tooltip("显示名，用于日志与提示")]
        [SerializeField] private string displayName = "AKM";

        [Header("武器参数")]
        [Tooltip("数值来源：设计文档「枪械系统 / 武器基础属性 / 突击步枪 / AKM.md」")]
        [SerializeField] private WeaponStats stats = new WeaponStats();

        public string WeaponId => weaponId;

        /// <summary>武器参数（射速、后坐力、散射、伤害）</summary>
        public WeaponStats Stats => stats;

        public string DisplayName =>
            string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
    }
}
