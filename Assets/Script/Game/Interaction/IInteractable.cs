using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// 可交互物统一接口。
    ///
    /// 玩家的准心射线打到带有本接口的物体时，会显示提示文字，按 F 触发 Interact。
    /// 拾取物、容器、门、开关、撤离点…… 全部实现这个接口即可接入准心交互。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>提示文字，例如「拾取 AKM」。返回空字符串表示不显示提示</summary>
        string InteractionPrompt { get; }

        /// <summary>当前是否允许交互（例如门已锁、物品已被拿走）</summary>
        bool CanInteract(GameObject interactor);

        /// <summary>执行交互</summary>
        void Interact(GameObject interactor);
    }
}
