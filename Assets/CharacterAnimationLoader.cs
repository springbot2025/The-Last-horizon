using UnityEngine;

/// <summary>
/// 角色动画加载器：根据角色名称自动加载对应的动画控制器
/// </summary>
public static class CharacterAnimationLoader
{
    /// <summary>
    /// 根据角色名称加载动画控制器
    /// </summary>
    public static void LoadAnimationsForCharacter(Character character)
    {
        if (character == null || character.visualManager == null)
        {
            Debug.LogWarning("[CharacterAnimationLoader] Character or VisualManager is null");
            return;
        }

        string charName = character.characterName;
        
        // 加载动画控制器
        RuntimeAnimatorController idleController = LoadAnimatorController(charName, "wait");
        RuntimeAnimatorController moveController = LoadAnimatorController(charName, "move");
        RuntimeAnimatorController attackController = LoadAnimatorController(charName, "fight");
        RuntimeAnimatorController skillController = LoadAnimatorController(charName, "special");

        // 记录加载结果
        Debug.Log($"[CharacterAnimationLoader] Loading animations for {charName}:");
        Debug.Log($"  - Idle: {(idleController != null ? idleController.name : "NULL")}");
        Debug.Log($"  - Move: {(moveController != null ? moveController.name : "NULL")}");
        Debug.Log($"  - Attack: {(attackController != null ? attackController.name : "NULL")}");
        Debug.Log($"  - Skill: {(skillController != null ? skillController.name : "NULL")}");

        // 初始化视觉管理器
        character.visualManager.Initialize(idleController, moveController, attackController, skillController);
        
        Debug.Log($"[CharacterAnimationLoader] ✅ Animation controllers loaded for {charName}");
    }

    /// <summary>
    /// 根据角色名称和动画类型加载动画控制器
    /// </summary>
    private static RuntimeAnimatorController LoadAnimatorController(string characterName, string animType)
    {
        string controllerName = GetControllerName(characterName, animType);
        
        if (string.IsNullOrEmpty(controllerName))
        {
            Debug.LogWarning($"[CharacterAnimationLoader] Cannot generate controller name for {characterName} {animType} animation");
            return null;
        }

        // Method 1: Try Resources folder
        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>($"Animations/{controllerName}");
        if (controller != null)
        {
            return controller;
        }

        // Method 2: Search in Resources
        RuntimeAnimatorController[] allControllers = Resources.LoadAll<RuntimeAnimatorController>("");
        foreach (var ctrl in allControllers)
        {
            if (ctrl != null && ctrl.name.Contains(controllerName))
            {
                return ctrl;
            }
        }

        // Method 3: Search in Assets/animation folder (Editor only)
        #if UNITY_EDITOR
        try
        {
            // Try exact name match first
            string[] guids = null;
            try
            {
                guids = UnityEditor.AssetDatabase.FindAssets($"{controllerName} t:AnimatorController");
            }
            catch (System.Exception findEx)
            {
                // Ignore PlasticSCM or version control related errors
                Debug.LogWarning($"[CharacterAnimationLoader] FindAssets failed (may be version control plugin issue): {findEx.Message}");
                guids = null;
            }
            
            if (guids != null && guids.Length > 0)
            {
                foreach (string guid in guids)
                {
                    try
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path) && path.Contains("animation"))
                        {
                            controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                            if (controller != null)
                            {
                                Debug.Log($"[CharacterAnimationLoader] Loaded from Assets/animation: {path}");
                                return controller;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[CharacterAnimationLoader] Error loading {guid}: {e.Message}");
                        continue;
                    }
                }
            }
            
            // Try partial name match (in case filename is slightly different)
            try
            {
                guids = UnityEditor.AssetDatabase.FindAssets($"t:AnimatorController", new[] { "Assets/animation" });
            }
            catch (System.Exception findEx2)
            {
                // Ignore PlasticSCM or version control related errors
                Debug.LogWarning($"[CharacterAnimationLoader] FindAssets (partial) failed (may be version control plugin issue): {findEx2.Message}");
                guids = null;
            }
            
            if (guids != null && guids.Length > 0)
            {
                foreach (string guid in guids)
                {
                    try
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        
                        // Check if filename contains the controller name or matches key parts
                        // Handle special cases like "gngcheng" vs "gongcheng"
                        bool nameMatches = false;
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            // Exact match
                            if (fileName.Contains(controllerName))
                            {
                                nameMatches = true;
                            }
                            // Special handling for partial matches
                            else if (controllerName.Contains("工程师") && fileName.Contains("工程师"))
                            {
                                nameMatches = true;
                            }
                            else if (controllerName.Contains("游侠") && fileName.Contains("游侠"))
                            {
                                nameMatches = true;
                            }
                            else if (controllerName.Contains("暗影") && fileName.Contains("暗影"))
                            {
                                nameMatches = true;
                            }
                            else if (controllerName.Contains("铁腕") && fileName.Contains("铁腕"))
                            {
                                nameMatches = true;
                            }
                            else if (controllerName.Contains("圣光") && fileName.Contains("圣光"))
                            {
                                nameMatches = true;
                            }
                            else if (controllerName.Contains("至忠") && fileName.Contains("至忠"))
                            {
                                nameMatches = true;
                            }
                            else if (controllerName.Contains("炸弹") && fileName.Contains("炸弹"))
                            {
                                nameMatches = true;
                            }
                            else if (controllerName.Contains("弓箭手") && fileName.Contains("弓箭手"))
                            {
                                nameMatches = true;
                            }
                        }
                        
                        if (nameMatches)
                        {
                            controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                            if (controller != null)
                            {
                                Debug.Log($"[CharacterAnimationLoader] Loaded from Assets/animation (partial match): {path}");
                                return controller;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        continue;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            // Ignore version control plugin errors (like PlasticSCM) - they don't affect gameplay
            if (e != null && e.Message != null && !e.Message.Contains("PlasticSCM"))
            {
                Debug.LogWarning($"[CharacterAnimationLoader] AssetDatabase search failed: {e.Message}");
            }
        }
        #endif

        Debug.LogWarning($"[CharacterAnimationLoader] Animation controller not found: {controllerName}");
        return null;
    }

    /// <summary>
    /// 根据角色名称和动画类型生成控制器文件名
    /// </summary>
    private static string GetControllerName(string characterName, string animType)
    {
        // 角色名称到文件夹名称的映射
        string folderName = "";
        string controllerPrefix = "";

        switch (characterName)
        {
            case "钢腕":
                folderName = "iron";
                controllerPrefix = "铁腕";
                break;
            case "半魔游侠":
                folderName = "youxia";
                controllerPrefix = "游侠";
                break;
            case "弓箭手":
                folderName = "shooter";
                controllerPrefix = "弓箭手";
                break;
            case "暗夜猎手":
                folderName = "anying";
                controllerPrefix = "暗影";
                break;
            case "工程师":
                folderName = "gongcheng";
                controllerPrefix = "工程师";
                break;
            case "投弹手":
                folderName = "zhadan";
                controllerPrefix = "炸弹兵";
                break;
            case "圣光卫士":
                folderName = "shengguang";
                controllerPrefix = "圣光卫士";
                break;
            case "至忠圣卫":
                folderName = "zhizhong";
                controllerPrefix = "至忠圣卫";
                break;
            default:
                Debug.LogWarning($"[CharacterAnimationLoader] Unknown character name: {characterName}");
                return null;
        }

        // Animation type to controller suffix mapping
        string controllerSuffix = "";
        switch (animType)
        {
            case "wait":
                controllerSuffix = "待机_01";
                break;
            case "move":
                if (characterName == "半魔游侠")
                    controllerSuffix = "行走_01";
                else if (characterName == "圣光卫士")
                    controllerSuffix = "移动_06";  // Special case: 圣光卫士移动_06
                else if (characterName == "暗夜猎手")
                    return "暗影移动(瞬间移动，在一格消失后再倒放出现在另一格)_01";  // Special case
                else
                    controllerSuffix = "移动_01";
                break;
            case "fight":
                controllerSuffix = "攻击_01";
                break;
            case "special":
                controllerSuffix = "技能_01";
                break;
            default:
                Debug.LogWarning($"[CharacterAnimationLoader] Unknown animation type: {animType}");
                return null;
        }

        string fullName = $"{controllerPrefix}{controllerSuffix}";
        Debug.Log($"[CharacterAnimationLoader] Generated controller name: {fullName} for {characterName} {animType}");
        return fullName;
    }
}

