using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI 分层定义。枚举值同时用作 Canvas 的排序值（越大越靠前）。
    /// 想调整前后关系，改这里的数值即可，全局生效。
    /// </summary>
    public enum UILayer
    {
        /// <summary>跟随三维坐标的世界提示：伤害数字、交互点标签</summary>
        World = 10,

        /// <summary>常驻 HUD：准心、血量、弹药、体力</summary>
        Hud = 20,

        /// <summary>弹出面板：背包、容器搜索、设置、暂停</summary>
        Panel = 30,

        /// <summary>短时通知：拾取提示、撤离倒计时、系统消息</summary>
        Toast = 40,

        /// <summary>调试信息：版本号、性能面板</summary>
        Debug = 50,
    }

    public static class UILayerUtility
    {
        /// <summary>分层画布在层级窗口里显示的名字</summary>
        public static string ObjectName(UILayer layer)
        {
            switch (layer)
            {
                case UILayer.World: return "Layer_World";
                case UILayer.Hud:   return "Layer_Hud";
                case UILayer.Panel: return "Layer_Panel";
                case UILayer.Toast: return "Layer_Toast";
                default:            return "Layer_Debug";
            }
        }

        /// <summary>该层是否需要接收鼠标点击（只有面板层需要，其余关掉省性能）</summary>
        public static bool NeedsRaycast(UILayer layer)
        {
            return layer == UILayer.Panel;
        }
    }
}
