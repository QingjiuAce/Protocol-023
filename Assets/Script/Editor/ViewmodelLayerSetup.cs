#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 自动创建第一人称武器所需的「Viewmodel」图层。
    ///
    /// 图层属于项目设置，代码没法在运行时新建，所以这里在编辑器加载时补一个，
    /// 省得手动去「项目设置 → 标签和图层」里加。
    /// </summary>
    [InitializeOnLoad]
    public static class ViewmodelLayerSetup
    {
        public const string LayerName = "Viewmodel";

        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        static ViewmodelLayerSetup()
        {
            EditorApplication.delayCall += EnsureLayer;
        }

        [MenuItem("禁区协议/创建 Viewmodel 图层", priority = 120)]
        private static void EnsureLayerFromMenu() => EnsureLayer(true);

        private static void EnsureLayer() => EnsureLayer(false);

        private static void EnsureLayer(bool verbose)
        {
            if (LayerMask.NameToLayer(LayerName) >= 0)
            {
                if (verbose) Debug.Log($"[图层工具] 图层「{LayerName}」已经存在，无需创建。");
                return;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[图层工具] 读不到 TagManager，请手动创建 Viewmodel 图层。");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning("[图层工具] TagManager 结构异常，请手动创建 Viewmodel 图层。");
                return;
            }

            // 0~7 是 Unity 内置层，用户自定义从 8 开始
            int freeIndex = -1;
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(element.stringValue))
                {
                    freeIndex = i;
                    break;
                }
            }

            if (freeIndex < 0)
            {
                Debug.LogWarning("[图层工具] 32 个图层已用完，请手动腾一个出来并命名为 Viewmodel。");
                return;
            }

            layers.GetArrayElementAtIndex(freeIndex).stringValue = LayerName;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[图层工具] 已在第 {freeIndex} 层创建图层「{LayerName}」，第一人称武器可以正常分离渲染了。");
        }
    }
}
#endif
