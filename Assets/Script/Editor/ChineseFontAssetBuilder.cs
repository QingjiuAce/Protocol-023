#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.EditorTools
{
    /// <summary>
    /// 中文字体资源生成工具。
    ///
    /// 用途：TMP 默认字体不含中文，这里把项目里的中文字体文件转成 TMP 字体资源，
    /// 并设为全局默认字体，之后所有 UI 文字都能正常显示中文。
    ///
    /// 使用：菜单栏「禁区协议 → 生成中文字体资源」
    /// 换字体时：把新的 ttf/otf 放到 Assets/素材/字体/，改一下下面的路径常量再点一次即可。
    /// </summary>
    public static class ChineseFontAssetBuilder
    {
        private const string SourceFontPath = "Assets/素材/字体/NotoSansSC-VF.ttf";
        private const string OutputFolder = "Assets/素材/字体";
        private const string OutputFileName = "NotoSansSC SDF.asset";

        /// <summary>SDF 采样点大小：越大越精细，图集占用也越大</summary>
        private const int SamplingPointSize = 90;

        /// <summary>图集内边距，防止字形互相渗色</summary>
        private const int AtlasPadding = 9;

        /// <summary>单张图集尺寸，中文字形多，靠多图集自动扩容</summary>
        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;

        [MenuItem("禁区协议/生成中文字体资源", priority = 100)]
        public static void BuildChineseFontAsset()
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError(
                    $"[字体工具] 找不到字体文件：{SourceFontPath}\n" +
                    "请把中文字体（ttf/otf）放到 Assets/素材/字体/ 下，并修改本脚本里的 SourceFontPath。");
                return;
            }

            EnsureFolder(OutputFolder);
            string outputPath = $"{OutputFolder}/{OutputFileName}";

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
                AssetDatabase.DeleteAsset(outputPath);

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasWidth,
                AtlasHeight,
                AtlasPopulationMode.Dynamic,     // 中文字形太多，必须动态按需生成
                true);                           // 允许图集自动扩容

            if (fontAsset == null)
            {
                Debug.LogError(
                    "[字体工具] 字体资源生成失败。\n" +
                    "如果反复失败，可能是可变字体（VF）不被支持，" +
                    "建议换用静态版字体文件（例如 NotoSansSC-Regular.otf）。");
                return;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(OutputFileName);
            AssetDatabase.CreateAsset(fontAsset, outputPath);
            SaveSubAssets(fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            TMP_FontAsset saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            bool applied = SetAsDefaultFont(saved);

            Debug.Log(
                $"[字体工具] 中文字体资源已生成：{outputPath}\n" +
                (applied
                    ? "已设为 TMP 全局默认字体，所有 UI 文字现在可以显示中文了。"
                    : "但设置默认字体失败，请手动到「项目设置 → TextMeshPro → 默认字体资源」里指定它。"));
        }

        [MenuItem("禁区协议/把中文字体设为默认", priority = 101)]
        public static void ApplyDefaultFont()
        {
            string outputPath = $"{OutputFolder}/{OutputFileName}";
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);

            if (fontAsset == null)
            {
                Debug.LogError($"[字体工具] 还没生成字体资源：{outputPath}\n请先执行「禁区协议 → 生成中文字体资源」。");
                return;
            }

            if (SetAsDefaultFont(fontAsset))
                Debug.Log("[字体工具] 已设为 TMP 全局默认字体。");
        }

        // ---------------- 内部实现 ----------------

        private static void SaveSubAssets(TMP_FontAsset fontAsset)
        {
            // 图集贴图必须作为子资产保存，否则重启后会丢失
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D texture = fontAsset.atlasTextures[i];
                    if (texture == null) continue;

                    texture.name = $"{fontAsset.name} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }

            // 材质同样要作为子资产保存
            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontAsset.name} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }

        internal static bool SetAsDefaultFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return false;

            TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                Debug.LogWarning(
                    "[字体工具] 找不到 TMP 设置资源（TMP Settings）。\n" +
                    "请先执行「窗口 → TextMeshPro → Import TMP Essential Resources」。");
                return false;
            }

            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty property = serialized.FindProperty("m_defaultFontAsset");
            if (property == null) return false;

            property.objectReferenceValue = fontAsset;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    /// <summary>
    /// 编辑器启动时自动检查中文字体配置：缺失就自动生成一次并设为默认字体，
    /// 免得每次都要手动点菜单。用 EditorPrefs 记录，保证只尝试一次，不会反复执行。
    /// </summary>
    [InitializeOnLoad]
    public static class ChineseFontAutoSetup
    {
        private const string CheckedKey = "Protocol023.ChineseFontChecked";
        private const string GeneratedAssetPath = "Assets/素材/字体/NotoSansSC SDF.asset";

        static ChineseFontAutoSetup()
        {
            EditorApplication.delayCall += Check;
        }

        private static void Check()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetBool(CheckedKey, false)) return;

            EditorPrefs.SetBool(CheckedKey, true);

            // 已经生成过：只补一次「设为默认」，防止之前那步没生效
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GeneratedAssetPath);
            if (existing != null)
            {
                if (ChineseFontAssetBuilder.SetAsDefaultFont(existing))
                    Debug.Log("[字体工具] 已把中文字体设为 TMP 默认字体。");
                return;
            }

            Debug.Log("[字体工具] 检测到尚未配置中文字体，正在自动生成 TMP 中文资源…");
            ChineseFontAssetBuilder.BuildChineseFontAsset();
        }
    }
}
#endif
