#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 编辑器脚本：用于从NotoSansSC字体文件创建TMP Font Asset
/// 使用方法：在Unity编辑器中，点击菜单 Assets > Create > TMP Font Asset from NotoSansSC
/// </summary>
public class NotoSansSCFontCreator
{
    [MenuItem("Assets/Create/TMP Font Asset from NotoSansSC", false, 100)]
    static void CreateFontAssetFromNoto()
    {
        try
        {
            // 查找NotoSansSC字体文件
            string[] guids = null;
            try
            {
                guids = AssetDatabase.FindAssets("NotoSansSC-VariableFont_wght t:Font");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", 
                    $"查找字体文件失败：{ex.Message}\n\n" +
                    "如果这是版本控制插件的错误，请禁用 PlasticSCM 插件或稍后再试。", 
                    "确定");
                Debug.LogError($"[NotoSansSCFontCreator] FindAssets 失败: {ex}");
                return;
            }
            
            if (guids == null || guids.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", 
                    "未找到 NotoSansSC-VariableFont_wght 字体文件！\n\n" +
                    "请确保字体文件已导入到项目中。\n" +
                    "如果文件名不同，请使用 'TMP Font Asset from Selected Font' 选项。", 
                    "确定");
                return;
            }
            
            string fontPath = null;
            try
            {
                fontPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", 
                    $"获取字体路径失败：{ex.Message}", 
                    "确定");
                Debug.LogError($"[NotoSansSCFontCreator] GUIDToAssetPath 失败: {ex}");
                return;
            }
            
            if (string.IsNullOrEmpty(fontPath))
            {
                EditorUtility.DisplayDialog("错误", "字体路径为空！", "确定");
                return;
            }
            
            Font sourceFont = null;
            try
            {
                sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", 
                    $"加载字体文件失败：{ex.Message}\n\n路径：{fontPath}", 
                    "确定");
                Debug.LogError($"[NotoSansSCFontCreator] LoadAssetAtPath 失败: {ex}");
                return;
            }
            
            if (sourceFont == null)
            {
                EditorUtility.DisplayDialog("错误", $"无法加载字体文件: {fontPath}", "确定");
                return;
            }
            
            // 使用TMP的Font Asset Creator窗口
            ShowFontCreatorWindow(sourceFont);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", 
                $"创建字体资源时发生未预期的错误：{e.Message}\n\n" +
                "如果这是 PlasticSCM 插件的错误，请禁用该插件或忽略此错误。", 
                "确定");
            Debug.LogError($"[NotoSansSCFontCreator] 未预期的错误: {e}");
        }
    }
    
    [MenuItem("Assets/Create/TMP Font Asset from Selected Font", false, 101)]
    static void CreateFontAssetFromSelection()
    {
        Object selected = Selection.activeObject;
        
        if (selected == null || !(selected is Font))
        {
            EditorUtility.DisplayDialog("错误", 
                "请先选择一个字体文件（.ttf）！\n\n" +
                "在 Project 窗口中选中字体文件，然后再次选择此菜单项。", 
                "确定");
            return;
        }
        
        Font sourceFont = selected as Font;
        ShowFontCreatorWindow(sourceFont);
    }
    
    /// <summary>
    /// 显示TMP Font Asset Creator窗口
    /// </summary>
    static void ShowFontCreatorWindow(Font sourceFont)
    {
        if (sourceFont == null)
        {
            Debug.LogError("字体文件为空！");
            return;
        }
        
        // 方法1: 尝试直接打开TMP的Font Asset Creator窗口（最可靠的方法）
        try
        {
            // 使用TMP的内部窗口类
            System.Type windowType = System.Type.GetType("TMPro.EditorUtilities.TMPro_FontAssetCreatorWindow, Unity.TextMeshPro.Editor");
            if (windowType != null)
            {
                System.Reflection.MethodInfo showMethod = windowType.GetMethod("ShowFontAtlasCreatorWindow", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null,
                    new System.Type[] { typeof(Font) },
                    null);
                
                if (showMethod != null)
                {
                    showMethod.Invoke(null, new object[] { sourceFont });
                    Debug.Log($"[NotoSansSCFontCreator] 已打开TMP字体资源创建窗口: {sourceFont.name}");
                    EditorUtility.DisplayDialog("🚨 重要提示：必须选择中文字符集！", 
                        "已打开 TMP Font Asset Creator 窗口！\n\n" +
                        "⚠️ 关键步骤（请严格按照顺序）：\n\n" +
                        "1. Source Font File: 选择 NotoSansSC-VariableFont_wght\n" +
                        "2. Sampling Point Size: 输入 90\n" +
                        "3. Padding: 输入 9\n" +
                        "4. Atlas Resolution: 选择 1024 x 1024\n\n" +
                        "⭐ 5. Character Set（最重要！）：\n" +
                        "   - 第一个下拉菜单：选择 'Unicode Characters'\n" +
                        "   - 第二个下拉菜单（会出现）：必须选择 'Chinese'（中文）\n" +
                        "   - ❌ 不要选 ASCII，不要选 Extended ASCII\n" +
                        "   - ✅ 必须选 Chinese\n\n" +
                        "6. 点击 'Generate Font Atlas'（等待完成，能看到中文字符预览）\n" +
                        "7. 点击 'Save' 保存到 Assets 文件夹\n\n" +
                        "如果选错了字符集，字体资源只会包含英文！\n" +
                        "详细步骤请查看：创建中文字体-关键步骤.md", 
                        "我知道了，我会选择 Chinese");
                    return;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NotoSansSCFontCreator] 无法打开TMP窗口: {e.Message}");
        }
        
        // 方法2: 使用简化的CreateFontAsset方法（使用默认参数）
        try
        {
            // 使用默认参数的简化版本
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            
            if (fontAsset != null)
            {
                fontAsset.name = sourceFont.name + " SDF";
                
                // 显示保存对话框
                string savePath = EditorUtility.SaveFilePanelInProject(
                    "保存字体资源",
                    fontAsset.name,
                    "asset",
                    "请选择保存位置（推荐保存到 Assets/Resources 文件夹）"
                );
                
                if (!string.IsNullOrEmpty(savePath))
                {
                    try
                    {
                        AssetDatabase.CreateAsset(fontAsset, savePath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    catch (System.Exception saveEx)
                    {
                        Debug.LogError($"[NotoSansSCFontCreator] 保存字体资源失败: {saveEx.Message}");
                        EditorUtility.DisplayDialog("错误", 
                            $"保存字体资源失败：{saveEx.Message}\n\n" +
                            "如果这是版本控制插件的错误，请禁用 PlasticSCM 插件或稍后再试。", 
                            "确定");
                        return;
                    }
                    
                    string message = $"字体资源已创建并保存到:\n{savePath}";
                    if (savePath.Contains("Resources"))
                    {
                        message += "\n\n✅ 已保存到Resources文件夹，脚本会自动找到并使用！";
                    }
                    else
                    {
                        message += "\n\n⚠️ 请在CharacterSelectionPanel组件的Inspector中，手动将字体资源拖到chineseFontAsset字段。";
                    }
                    
                    EditorUtility.DisplayDialog("成功", message, "确定");
                    Debug.Log($"[NotoSansSCFontCreator] {message}");
                }
            }
            else
            {
                // 如果CreateFontAsset返回null，引导用户使用Unity内置工具
                EditorUtility.DisplayDialog("提示", 
                    "自动创建失败，请使用 Unity 内置工具：\n\n" +
                    "1. Window → TextMeshPro → Font Asset Creator\n" +
                    "2. Source Font File: 选择 " + sourceFont.name + "\n" +
                    "3. Sampling: 90, Padding: 9, Atlas: 1024x1024\n\n" +
                    "⭐ 4. Character Set（最关键！）：\n" +
                    "   - 第一个菜单：Unicode Characters\n" +
                    "   - 第二个菜单（出现后）：必须选择 Chinese\n" +
                    "   - ❌ 不要选 ASCII！\n\n" +
                    "5. 点击 Generate Font Atlas（等待完成）\n" +
                    "6. 点击 Save 保存\n\n" +
                    "详细步骤请查看：创建中文字体-关键步骤.md", 
                    "我知道了");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", 
                $"创建字体资源时出错：\n{e.Message}\n\n" +
                "建议使用 Unity 内置方法：\n" +
                "Window > TextMeshPro > Font Asset Creator\n\n" +
                "详细步骤请查看：中文字体导入具体步骤.md", 
                "确定");
            Debug.LogError($"[NotoSansSCFontCreator] 错误: {e}");
        }
    }
}
#endif

