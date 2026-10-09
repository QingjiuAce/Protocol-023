using System;
using System.Collections.Generic;
using Game.Interaction;
using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// 武器槽管理：收集挂点下的所有武器模型，按 ID 索引，负责显示/隐藏与切换。
    ///
    /// 采用「预生成 + 显隐」策略 —— 武器模型全都提前摆好并隐藏，
    /// 捡到时只是点亮对应的那个，没有任何实例化开销，切枪也是瞬时的。
    ///
    /// 挂在武器挂点（WeaponSocket）上即可。
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponHolder : MonoBehaviour
    {
        [Header("武器槽")]
        [Tooltip("武器模型的父节点，留空则用本对象")]
        [SerializeField] private Transform socket;

        [Tooltip("启动时隐藏所有武器（开局手上空着）")]
        [SerializeField] private bool hideAllOnStart = true;

        [Header("调试")]
        [SerializeField] private bool debugLogging = true;

        private readonly Dictionary<string, WeaponInstance> weapons =
            new Dictionary<string, WeaponInstance>();

        private WeaponInstance current;

        // ---------------- 对外状态 ----------------

        /// <summary>当前装备的武器，没拿枪时为 null</summary>
        public WeaponInstance Current => current;

        public bool HasWeapon => current != null;

        /// <summary>切换武器时触发（卸下时参数为 null）</summary>
        public event Action<WeaponInstance> WeaponChanged;

        // ---------------- 生命周期 ----------------

        void Awake()
        {
            if (socket == null) socket = transform;

            CollectWeapons();

            if (hideAllOnStart)
                SetAllActive(false);
            else
                EquipFirst();
        }

        void OnEnable()
        {
            InteractionEvents.ItemPicked += OnItemPicked;
        }

        void OnDisable()
        {
            InteractionEvents.ItemPicked -= OnItemPicked;
        }

        // ---------------- 收集 ----------------

        private void CollectWeapons()
        {
            weapons.Clear();

            WeaponInstance[] found = socket.GetComponentsInChildren<WeaponInstance>(true);

            foreach (WeaponInstance weapon in found)
            {
                if (weapon == null) continue;

                string id = weapon.WeaponId;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[WeaponHolder] 武器「{weapon.name}」没有填 Weapon Id，已跳过。", weapon);
                    continue;
                }

                if (weapons.ContainsKey(id))
                {
                    Debug.LogWarning($"[WeaponHolder] 武器 ID「{id}」重复，只保留第一个。", weapon);
                    continue;
                }

                weapons.Add(id, weapon);
            }

            if (debugLogging)
                Debug.Log($"[WeaponHolder] 武器槽已就绪，共 {weapons.Count} 把：{DescribeWeapons()}");
        }

        // ---------------- 装备 ----------------

        private void OnItemPicked(string itemId, string displayName)
        {
            Equip(itemId);
        }

        /// <summary>按武器 ID 装备。槽里没有这把武器则返回 false（例如捡到的是弹药）</summary>
        public bool Equip(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return false;

            if (!weapons.TryGetValue(weaponId, out WeaponInstance weapon) || weapon == null)
            {
                if (debugLogging)
                {
                    Debug.Log($"[WeaponHolder] 拾取「{weaponId}」——但武器槽里没有对应的模型，" +
                        $"当前槽位：{DescribeWeapons()}");
                }
                return false;
            }

            if (current == weapon)
            {
                weapon.gameObject.SetActive(true);
                return true;
            }

            SetAllActive(false);
            weapon.gameObject.SetActive(true);
            current = weapon;

            if (debugLogging)
                Debug.Log($"[WeaponHolder] 已装备：{weapon.DisplayName}");

            WeaponChanged?.Invoke(weapon);
            return true;
        }

        /// <summary>收起武器（手上空着）</summary>
        public void Unequip()
        {
            if (current == null) return;

            SetAllActive(false);
            current = null;
            WeaponChanged?.Invoke(null);
        }

        /// <summary>在已有的武器之间循环切换，用于当前阶段的测试</summary>
        public void EquipNext()
        {
            if (weapons.Count == 0) return;

            List<string> ids = new List<string>(weapons.Keys);
            int index = current != null ? ids.IndexOf(current.WeaponId) : -1;
            index = (index + 1) % ids.Count;

            Equip(ids[index]);
        }

        // ---------------- 内部 ----------------

        private void SetAllActive(bool active)
        {
            foreach (WeaponInstance weapon in weapons.Values)
            {
                if (weapon != null)
                    weapon.gameObject.SetActive(active);
            }
        }

        private void EquipFirst()
        {
            foreach (WeaponInstance weapon in weapons.Values)
            {
                Equip(weapon.WeaponId);
                return;
            }
        }

        private string DescribeWeapons()
        {
            if (weapons.Count == 0) return "（空）";
            return string.Join("、", weapons.Keys);
        }
    }
}
