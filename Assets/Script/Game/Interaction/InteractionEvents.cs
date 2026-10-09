using System;

namespace Game.Interaction
{
    /// <summary>
    /// 交互事件中心 —— 让「捡到了什么」和「捡到之后干什么」彻底解耦。
    ///
    /// 拾取物只负责广播结果；武器槽、库存、任务系统各自订阅自己关心的部分，
    /// 互不认识、互不依赖。
    /// </summary>
    public static class InteractionEvents
    {
        /// <summary>
        /// 拾取完成。参数：物品 ID、显示名。
        /// itemId 为空字符串时不会广播。
        /// </summary>
        public static event Action<string, string> ItemPicked;

        public static void RaiseItemPicked(string itemId, string displayName)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            ItemPicked?.Invoke(itemId, displayName);
        }
    }
}
