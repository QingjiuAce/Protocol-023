using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// 可拾取物：准心对准、按 F 拾取，拾取后从场景中消失。
    ///
    /// 当前阶段只做「拿起来就没了」，后续接入库存系统时，
    /// 在 OnPicked 里把物品数据塞进背包即可，其他逻辑不用动。
    /// </summary>
    [DisallowMultipleComponent]
    public class PickupItem : MonoBehaviour, IInteractable
    {
        [Header("物品信息")]
        [Tooltip("物品名，显示在交互提示里。留空则直接使用物体名")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("动作词，例如「拾取」「打开」「查看」")]
        [SerializeField] private string actionName = "拾取";

        [Header("行为")]
        [Tooltip("拾取后是否直接销毁物体（取消勾选则只是隐藏）")]
        [SerializeField] private bool destroyOnPickup = true;

        /// <summary>物品唯一标识，后续接库存时用来查表</summary>
        [SerializeField] private string itemId = string.Empty;

        public string ItemId => itemId;

        /// <summary>显示名：没手动填就用物体名，省得每个都配</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;

        public string InteractionPrompt
        {
            get
            {
                if (string.IsNullOrEmpty(actionName)) return DisplayName;
                return $"{actionName} {DisplayName}";
            }
        }

        public bool CanInteract(GameObject interactor) => isActiveAndEnabled;

        public virtual void Interact(GameObject interactor)
        {
            OnPicked(interactor);

            if (destroyOnPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        /// <summary>子类可重写：把物品写进库存、播放音效等</summary>
        protected virtual void OnPicked(GameObject interactor)
        {
            Debug.Log($"[拾取] {DisplayName}" +
                (string.IsNullOrEmpty(itemId) ? string.Empty : $"（ID: {itemId}）"));

            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning(
                    $"[拾取]「{DisplayName}」没有填写 Item Id —— 武器槽、库存等系统无法识别这次拾取。\n" +
                    "请在该物体的 Pickup Item 组件里把 Item Id 补上（例如 akm）。", this);
            }

            // 广播拾取结果：武器槽、库存等系统各自订阅，互不依赖
            InteractionEvents.RaiseItemPicked(itemId, DisplayName);
        }
    }
}
