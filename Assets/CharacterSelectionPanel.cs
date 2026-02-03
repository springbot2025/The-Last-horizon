using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 角色选择界面管理脚本
/// 在地图界面上显示选角色面板
/// </summary>
public class CharacterSelectionPanel : MonoBehaviour
{
    [Header("UI组件引用")]
    public GameObject panel; // 主面板
    public TextMeshProUGUI player1Text; // 玩家1文本
    public TextMeshProUGUI player2Text; // 玩家2文本
    public TextMeshProUGUI phaseText; // 当前阶段提示文本
    public Button confirmButton; // 确定按钮
    public Button cancelButton; // 退出按钮
    
    [Header("角色选择按钮容器")]
    public Transform characterButtonContainer; // 角色按钮容器
    public GameObject characterButtonPrefab; // 角色按钮预制体（可选）
    
    [Header("可选角色列表")]
    public List<CharacterData> availableCharacters = new List<CharacterData>();
    
    // 当前选择的角色（每个玩家选择两个角色）
    private CharacterData selectedPlayer1Char1; // 玩家1的第1个角色
    private CharacterData selectedPlayer1Char2; // 玩家1的第2个角色
    private CharacterData selectedPlayer2Char1; // 玩家2的第1个角色
    private CharacterData selectedPlayer2Char2; // 玩家2的第2个角色
    
    // 选择阶段枚举
    private enum SelectionPhase
    {
        Player1_Char1,  // 玩家1选择第1个角色
        Player1_Char2,  // 玩家1选择第2个角色
        Player2_Char1,  // 玩家2选择第1个角色
        Player2_Char2   // 玩家2选择第2个角色
    }
    
    private SelectionPhase currentPhase = SelectionPhase.Player1_Char1;
    
    void Start()
    {
        // 提前查找并缓存中文字体资源（如果未手动指定）
        if (chineseFontAsset == null && cachedChineseFontAsset == null)
        {
            cachedChineseFontAsset = FindChineseFontAsset();
            if (cachedChineseFontAsset != null)
            {
                Debug.Log($"[CharacterSelectionPanel] Found and cached Chinese font at Start: {cachedChineseFontAsset.name}");
            }
        }
        
        // 如果panel未设置，自动创建UI
        if (panel == null)
        {
            CreateUI();
        }
        
        // 初始化时显示面板
        if (panel != null)
        {
            panel.SetActive(true); // 显示面板供选择
        }
        
        // 绑定按钮事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        // 如果没有设置可选角色列表，尝试从CharacterList获取
        if (availableCharacters.Count == 0)
        {
            LoadAvailableCharacters();
        }
        
        // 如果没有设置容器和预制体，自动创建角色选择按钮
        if (characterButtonContainer == null && availableCharacters.Count > 0)
        {
            CreateCharacterButtonContainer();
        }
        
        // 创建角色选择按钮（如果有容器和预制体）
        if (characterButtonContainer != null && characterButtonPrefab != null)
        {
            CreateCharacterButtons();
        }
        // 如果没有预制体，创建简单的按钮
        else if (characterButtonContainer != null && characterButtonPrefab == null && availableCharacters.Count > 0)
        {
            CreateSimpleCharacterButtons();
        }
        
        // 如果没有容器，但需要创建按钮
        if (characterButtonContainer == null && availableCharacters.Count > 0)
        {
            Debug.LogWarning("[CharacterSelectionPanel] characterButtonContainer is null, attempting auto-create");
            CreateCharacterButtonContainer();
            if (characterButtonContainer != null)
            {
                CreateSimpleCharacterButtons();
            }
        }
        
        // 初始化文本（会应用字体）
        UpdateDisplay();
        
        // 验证UI状态
        ValidateUI();
        
        // 延迟一帧后再次强制刷新所有文本（确保字体已加载完成）
        StartCoroutine(DelayedFontRefresh());
    }
    
    /// <summary>
    /// 验证UI状态
    /// </summary>
    void ValidateUI()
    {
        Debug.Log("[CharacterSelectionPanel] ========== UI State Validation ==========");
        
        // 验证EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[CharacterSelectionPanel] EventSystem missing! UI interaction will not work!");
        }
        else
        {
            Debug.Log($"[CharacterSelectionPanel] EventSystem exists: {eventSystem.name}");
        }
        
        // 验证Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CharacterSelectionPanel] Canvas missing!");
        }
        else
        {
            Debug.Log($"[CharacterSelectionPanel] Canvas exists: {canvas.name}");
        }
        
        // 验证面板
        if (panel == null)
        {
            Debug.LogError("[CharacterSelectionPanel] Panel is null!");
        }
        else
        {
            Debug.Log($"[CharacterSelectionPanel] Panel exists: {panel.name}, Active: {panel.activeSelf}");
        }
        
        // 验证按钮容器
        if (characterButtonContainer == null)
        {
            Debug.LogWarning("[CharacterSelectionPanel] characterButtonContainer is null!");
        }
        else
        {
            int buttonCount = characterButtonContainer.childCount;
            Debug.Log($"[CharacterSelectionPanel] characterButtonContainer exists, child count: {buttonCount}");
            
            Button[] buttons = characterButtonContainer.GetComponentsInChildren<Button>();
            Debug.Log($"[CharacterSelectionPanel] Button components count: {buttons.Length}");
            foreach (Button btn in buttons)
            {
                if (btn != null)
                {
                    Debug.Log($"  - {btn.name}: Interactable={btn.interactable}, Active={btn.gameObject.activeSelf}");
                }
            }
        }
        
        // 验证可选角色列表
        Debug.Log($"[CharacterSelectionPanel] Available characters count: {availableCharacters.Count}");
        foreach (var charData in availableCharacters)
        {
            if (charData != null)
            {
                Debug.Log($"  - {charData.name}");
            }
        }
        
        Debug.Log("[CharacterSelectionPanel] ==================================");
    }
    
    /// <summary>
    /// 延迟刷新字体（确保字体资源已完全加载）
    /// </summary>
    System.Collections.IEnumerator DelayedFontRefresh()
    {
        yield return null; // 等待一帧
        
        // 刷新所有文本组件
        if (player1Text != null)
        {
            SetChineseFont(player1Text);
            player1Text.ForceMeshUpdate();
        }
        
        if (player2Text != null)
        {
            SetChineseFont(player2Text);
            player2Text.ForceMeshUpdate();
        }
        
        // 刷新所有角色按钮文本
        if (characterButtonContainer != null)
        {
            TextMeshProUGUI[] allTexts = characterButtonContainer.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in allTexts)
            {
                if (text != null)
                {
                    SetChineseFont(text);
                    text.ForceMeshUpdate();
                }
            }
        }
        
        Debug.Log("[CharacterSelectionPanel] Delayed font refresh completed");
    }
    
    /// <summary>
    /// 强制更新UI（确保显示正确）
    /// </summary>
    System.Collections.IEnumerator ForceUpdateUI()
    {
        yield return null; // 等待一帧
        
        // 再次更新显示
        UpdateDisplay();
        
        // 确保文本可见
        if (player1Text != null)
        {
            player1Text.enabled = true;
            player1Text.ForceMeshUpdate();
        }
        if (player2Text != null)
        {
            player2Text.enabled = true;
            player2Text.ForceMeshUpdate();
        }
        if (phaseText != null)
        {
            phaseText.enabled = true;
            phaseText.ForceMeshUpdate();
        }
    }
    
    /// <summary>
    /// 自动创建UI元素
    /// </summary>
    void CreateUI()
    {
        // 查找或创建Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 确保在最上层
            
            // 添加CanvasScaler
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // 添加GraphicRaycaster（用于UI交互）
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            Debug.Log("[CharacterSelectionPanel] Canvas created");
        }
        
        // 确保EventSystem存在（UI交互必需）
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[CharacterSelectionPanel] EventSystem created");
        }
        else
        {
            Debug.Log("[CharacterSelectionPanel] EventSystem already exists");
        }
        
        // 创建主面板（透明背景框）
        GameObject panelObj = new GameObject("CharacterSelectionPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        panel = panelObj;
        
        // 添加RectTransform
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.3f);
        panelRect.anchorMax = new Vector2(0.8f, 0.7f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;
        
        // 添加透明背景Image
        UnityEngine.UI.Image panelBg = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 半透明深灰色背景
        
        // 创建当前阶段提示文本
        GameObject phaseObj = new GameObject("PhaseText");
        phaseObj.transform.SetParent(panelObj.transform, false);
        RectTransform phaseRect = phaseObj.AddComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(0.1f, 0.75f);
        phaseRect.anchorMax = new Vector2(0.9f, 0.85f);
        phaseRect.anchoredPosition = Vector2.zero;
        phaseRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI phaseTextComponent = phaseObj.AddComponent<TextMeshProUGUI>();
        this.phaseText = phaseTextComponent; // 赋值给类的字段
        if (this.phaseText != null)
        {
            this.phaseText.text = "当前：玩家1选择第1个角色";
            this.phaseText.fontSize = 20;
            this.phaseText.color = Color.yellow;
            this.phaseText.alignment = TextAlignmentOptions.Center;
            SetChineseFont(this.phaseText);
        }
        
        // 创建玩家1文本（显示两个角色）
        GameObject player1Obj = new GameObject("Player1Text");
        player1Obj.transform.SetParent(panelObj.transform, false);
        RectTransform player1Rect = player1Obj.AddComponent<RectTransform>();
        player1Rect.anchorMin = new Vector2(0.1f, 0.60f);
        player1Rect.anchorMax = new Vector2(0.9f, 0.70f);
        player1Rect.anchoredPosition = Vector2.zero;
        player1Rect.sizeDelta = Vector2.zero;
        player1Text = player1Obj.AddComponent<TextMeshProUGUI>();
        if (player1Text != null)
        {
            player1Text.text = "玩家1：角色1（未选择） 角色2（未选择）";
            player1Text.fontSize = 22;
            player1Text.color = Color.white;
            player1Text.alignment = TextAlignmentOptions.Left;
            SetChineseFont(player1Text);
        }
        
        // 创建玩家2文本（显示两个角色）
        GameObject player2Obj = new GameObject("Player2Text");
        player2Obj.transform.SetParent(panelObj.transform, false);
        RectTransform player2Rect = player2Obj.AddComponent<RectTransform>();
        player2Rect.anchorMin = new Vector2(0.1f, 0.45f);
        player2Rect.anchorMax = new Vector2(0.9f, 0.55f);
        player2Rect.anchoredPosition = Vector2.zero;
        player2Rect.sizeDelta = Vector2.zero;
        player2Text = player2Obj.AddComponent<TextMeshProUGUI>();
        if (player2Text != null)
        {
            player2Text.text = "玩家2：角色1（未选择） 角色2（未选择）";
            player2Text.fontSize = 22;
            player2Text.color = Color.white;
            player2Text.alignment = TextAlignmentOptions.Left;
            SetChineseFont(player2Text);
        }
        
        // 创建确定按钮
        GameObject confirmObj = new GameObject("ConfirmButton");
        confirmObj.transform.SetParent(panelObj.transform, false);
        RectTransform confirmRect = confirmObj.AddComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.1f, 0.15f);
        confirmRect.anchorMax = new Vector2(0.45f, 0.3f);
        confirmRect.anchoredPosition = Vector2.zero;
        confirmRect.sizeDelta = Vector2.zero;
        UnityEngine.UI.Image confirmBg = confirmObj.AddComponent<UnityEngine.UI.Image>();
        confirmBg.color = new Color(0.3f, 0.7f, 0.3f, 1f); // 绿色按钮
        confirmButton = confirmObj.AddComponent<Button>();
        
        GameObject confirmTextObj = new GameObject("Text");
        confirmTextObj.transform.SetParent(confirmObj.transform, false);
        RectTransform confirmTextRect = confirmTextObj.AddComponent<RectTransform>();
        confirmTextRect.anchorMin = Vector2.zero;
        confirmTextRect.anchorMax = Vector2.one;
        confirmTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI confirmText = confirmTextObj.AddComponent<TextMeshProUGUI>();
        if (confirmText != null)
        {
            confirmText.text = "确定";
            confirmText.fontSize = 20;
            confirmText.color = Color.white;
            confirmText.alignment = TextAlignmentOptions.Center;
            SetChineseFont(confirmText);
        }
        
        // 创建退出按钮
        GameObject cancelObj = new GameObject("CancelButton");
        cancelObj.transform.SetParent(panelObj.transform, false);
        RectTransform cancelRect = cancelObj.AddComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.55f, 0.15f);
        cancelRect.anchorMax = new Vector2(0.9f, 0.3f);
        cancelRect.anchoredPosition = Vector2.zero;
        cancelRect.sizeDelta = Vector2.zero;
        UnityEngine.UI.Image cancelBg = cancelObj.AddComponent<UnityEngine.UI.Image>();
        cancelBg.color = new Color(0.7f, 0.3f, 0.3f, 1f); // 红色按钮
        cancelButton = cancelObj.AddComponent<Button>();
        
        GameObject cancelTextObj = new GameObject("Text");
        cancelTextObj.transform.SetParent(cancelObj.transform, false);
        RectTransform cancelTextRect = cancelTextObj.AddComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        cancelTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI cancelText = cancelTextObj.AddComponent<TextMeshProUGUI>();
        if (cancelText != null)
        {
            cancelText.text = "回到主界面";
            cancelText.fontSize = 20;
            cancelText.color = Color.white;
            cancelText.alignment = TextAlignmentOptions.Center;
            SetChineseFont(cancelText);
        }
        
        Debug.Log("[CharacterSelectionPanel] 角色选择面板UI已自动创建");
    }
    
    [Header("中文字体设置")]
    public TMP_FontAsset chineseFontAsset; // 手动指定的中文字体资源（可选）
    
    private static TMP_FontAsset cachedChineseFontAsset; // 缓存的字体资源，避免重复查找
    
    /// <summary>
    /// 设置中文字体（自动查找并使用NotoSansSC字体资源）
    /// </summary>
    void SetChineseFont(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;
        
        // 如果手动指定了字体资源，直接使用
        if (chineseFontAsset != null)
        {
            textComponent.font = chineseFontAsset;
            // 强制刷新文本渲染
            textComponent.ForceMeshUpdate();
            return;
        }
        
        // 如果已缓存字体资源，直接使用
        if (cachedChineseFontAsset != null)
        {
            textComponent.font = cachedChineseFontAsset;
            textComponent.ForceMeshUpdate();
            return;
        }
        
        // 尝试自动查找中文字体资源
        TMP_FontAsset fontAsset = FindChineseFontAsset();
        
        if (fontAsset != null)
        {
            cachedChineseFontAsset = fontAsset; // 缓存字体资源
            textComponent.font = fontAsset;
            textComponent.ForceMeshUpdate(); // 强制刷新文本渲染
            Debug.Log($"[CharacterSelectionPanel] ✅ 已应用中文字体: {fontAsset.name}");
            
            // 验证字体是否包含中文字符
            if (fontAsset.characterTable != null && fontAsset.characterTable.Count > 100)
            {
                Debug.Log($"[CharacterSelectionPanel] ✅ 字体资源包含 {fontAsset.characterTable.Count} 个字符，应该支持中文");
            }
            else
            {
                Debug.LogWarning($"[CharacterSelectionPanel] ⚠️ 字体资源可能不包含中文字符（仅 {fontAsset.characterTable?.Count ?? 0} 个字符）");
            }
        }
        else
        {
            // 如果找不到，尝试使用TMP默认设置中的字体
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                textComponent.font = TMPro.TMP_Settings.defaultFontAsset;
                textComponent.ForceMeshUpdate();
                Debug.LogWarning("[CharacterSelectionPanel] ⚠️ 未找到中文字体，使用TMP默认字体（可能不支持中文）");
            }
            else
            {
                Debug.LogError("[CharacterSelectionPanel] ❌ 未找到任何字体资源，文本可能显示为方块。");
                Debug.LogError("[CharacterSelectionPanel] 请确保字体资源已创建并正确配置。");
            }
        }
    }
    
    /// <summary>
    /// 查找项目中的中文字体资源（改进版，支持多种查找方式）
    /// </summary>
    TMP_FontAsset FindChineseFontAsset()
    {
        // 尝试多个可能的字体资源名称
        string[] possibleNames = {
            "NotoSansSC-VariableFont_wght SDF", // 完整文件名（最可能）
            "NotoSansSC SDF",
            "Noto Sans SC SDF",
            "NotoSansSC",
            "Noto Sans SC",
            "NotoSansSC-VariableFont_wght",
            "Chinese Font",
            "中文字体"
        };
        
        // 方法1: 从Resources加载（按优先级）
        foreach (string name in possibleNames)
        {
            try
            {
                // 尝试直接加载
                TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(name);
                if (fontAsset != null)
                {
                    Debug.Log($"[CharacterSelectionPanel] ✅ 从Resources找到字体: {name}");
                    return fontAsset;
                }
                
                // 尝试在常见路径中查找
                fontAsset = Resources.Load<TMP_FontAsset>($"Fonts & Materials/{name}");
                if (fontAsset != null)
                {
                    Debug.Log($"[CharacterSelectionPanel] ✅ 从Fonts & Materials找到字体: {name}");
                    return fontAsset;
                }
            }
            catch (System.Exception e)
            {
                // 忽略加载失败
            }
        }
        
        // 方法2: 运行时查找所有TMP字体资源
        try
        {
            TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (TMP_FontAsset font in allFonts)
            {
                if (font != null && (font.name.Contains("Noto") || font.name.Contains("SansSC") || 
                                     font.name.Contains("Chinese") || font.name.Contains("中文") ||
                                     font.name.Contains("SDF")))
                {
                    Debug.Log($"[CharacterSelectionPanel] ✅ 运行时找到字体: {font.name}");
                    return font;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CharacterSelectionPanel] 运行时查找字体失败: {e.Message}");
        }
        
        // 方法3: 编辑器模式下使用AssetDatabase（更准确）
        // 注意：添加了额外的安全检查以避免触发 PlasticSCM 插件的 NullReferenceException
        #if UNITY_EDITOR
        if (Application.isEditor && !Application.isPlaying)
        {
            try
            {
                // 查找所有TMP字体资源
                string[] guids = null;
                try
                {
                    guids = UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset");
                }
                catch (System.Exception guidEx)
                {
                    // Ignore version control plugin errors (like PlasticSCM)
                    if (guidEx != null && guidEx.Message != null && !guidEx.Message.Contains("PlasticSCM"))
                    {
                        Debug.LogWarning($"[CharacterSelectionPanel] FindAssets failed: {guidEx.Message}");
                    }
                    return null;
                }
                
                if (guids != null && guids.Length > 0)
                {
                    foreach (string guid in guids)
                    {
                        if (string.IsNullOrEmpty(guid)) continue;
                        
                        try
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                            if (string.IsNullOrEmpty(path)) continue;
                            
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                            
                            // 检查文件名是否包含关键词
                            if (fileName.Contains("Noto") || fileName.Contains("SansSC") || 
                                fileName.Contains("Chinese") || fileName.Contains("中文") ||
                                path.Contains("NotoSansSC"))
                            {
                                TMP_FontAsset fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                                if (fontAsset != null)
                                {
                                    Debug.Log($"[CharacterSelectionPanel] ✅ 从Assets找到字体: {path}");
                                    return fontAsset;
                                }
                            }
                        }
                        catch (System.Exception pathEx)
                        {
                            // 忽略单个路径的处理错误，继续查找
                            continue;
                        }
                    }
                }
                
                // 如果还是找不到，尝试查找Assets根目录下的字体资源
                try
                {
                    string[] allAssetPaths = UnityEditor.AssetDatabase.GetAllAssetPaths();
                    if (allAssetPaths != null)
                    {
                        foreach (string assetPath in allAssetPaths)
                        {
                            if (string.IsNullOrEmpty(assetPath)) continue;
                            
                            try
                            {
                                if (assetPath.Contains("NotoSansSC") && assetPath.EndsWith(".asset"))
                                {
                                    TMP_FontAsset fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                                    if (fontAsset != null)
                                    {
                                        Debug.Log($"[CharacterSelectionPanel] ✅ 从Assets根目录找到字体: {assetPath}");
                                        return fontAsset;
                                    }
                                }
                            }
                            catch (System.Exception assetEx)
                            {
                                // 忽略单个资源路径的处理错误
                                continue;
                            }
                        }
                    }
                }
                catch (System.Exception allPathsEx)
                {
                    // Ignore version control plugin errors (like PlasticSCM)
                    if (allPathsEx != null && allPathsEx.Message != null && !allPathsEx.Message.Contains("PlasticSCM"))
                    {
                        Debug.LogWarning($"[CharacterSelectionPanel] GetAllAssetPaths failed: {allPathsEx.Message}");
                    }
                }
            }
            catch (System.Exception e)
            {
                // Ignore version control plugin errors (like PlasticSCM) - they don't affect gameplay
                if (e != null && e.Message != null && !e.Message.Contains("PlasticSCM"))
                {
                    Debug.LogWarning($"[CharacterSelectionPanel] Editor font search failed: {e.Message}");
                }
            }
        }
        #endif
        
        Debug.LogWarning("[CharacterSelectionPanel] ❌ 未找到中文字体资源，尝试使用默认字体");
        return null;
    }
    
    /// <summary>
    /// 创建角色按钮容器（如果未手动设置）
    /// </summary>
    void CreateCharacterButtonContainer()
    {
        if (panel == null) return;
        
        // 创建滚动视图容器
        GameObject containerObj = new GameObject("CharacterButtonContainer");
        containerObj.transform.SetParent(panel.transform, false);
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.35f);
        containerRect.anchorMax = new Vector2(0.9f, 0.58f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = Vector2.zero;
        
        characterButtonContainer = containerObj.transform;
    }
    
    /// <summary>
    /// 创建简单的角色选择按钮（不需要预制体）
    /// </summary>
    void CreateSimpleCharacterButtons()
    {
        if (characterButtonContainer == null || availableCharacters.Count == 0) return;
        
        // 清空容器
        foreach (Transform child in characterButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        float buttonHeight = 40f;
        float spacing = 5f;
        float startY = -(buttonHeight + spacing) * (availableCharacters.Count - 1) / 2f;
        
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            var charData = availableCharacters[i];
            
            // 创建按钮
            GameObject btn = new GameObject($"CharacterButton_{charData.name}");
            btn.transform.SetParent(characterButtonContainer, false);
            RectTransform btnRect = btn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.sizeDelta = new Vector2(0, buttonHeight);
            btnRect.anchoredPosition = new Vector2(0, startY + i * (buttonHeight + spacing));
            
            // 添加背景
            Image btnBg = btn.AddComponent<Image>();
            btnBg.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            Button button = btn.AddComponent<Button>();
            button.interactable = true; // 确保按钮可交互
            
            // 添加文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = charData.name;
                btnText.fontSize = 18;
                btnText.color = Color.white;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.raycastTarget = false; // 文本不阻挡按钮点击
                SetChineseFont(btnText);
                // 强制刷新文本渲染，确保中文字符正确显示
                btnText.ForceMeshUpdate();
            }
            
            // 绑定点击事件（使用闭包捕获正确的角色数据）
            CharacterData capturedChar = charData;
            button.onClick.AddListener(() => 
            {
                Debug.Log($"[CharacterSelectionPanel] 按钮被点击：{capturedChar.name}");
                OnCharacterSelected(capturedChar);
            });
            
            // 设置按钮颜色变化和交互状态
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.7f, 1f);
            colors.pressedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            colors.selectedColor = new Color(0.5f, 0.5f, 0.7f, 1f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            
            Debug.Log($"[CharacterSelectionPanel] 已创建角色按钮：{charData.name}，按钮可交互：{button.interactable}");
        }
        
        Debug.Log($"[CharacterSelectionPanel] ✅ 成功创建了 {availableCharacters.Count} 个角色选择按钮，所有按钮已绑定点击事件");
        
        // 验证按钮创建
        Button[] buttons = characterButtonContainer.GetComponentsInChildren<Button>();
        Debug.Log($"[CharacterSelectionPanel] 验证：容器中共有 {buttons.Length} 个按钮组件");
        foreach (Button btn in buttons)
        {
            if (btn != null)
            {
                Debug.Log($"  - 按钮 '{btn.name}' 可交互：{btn.interactable}");
            }
        }
    }
    
    /// <summary>
    /// 从CharacterList加载可用角色
    /// </summary>
    void LoadAvailableCharacters()
    {
        // 使用默认角色列表
        CreateDefaultCharacterList();
    }
    
    /// <summary>
    /// 创建默认角色列表（作为备选）
    /// </summary>
    void CreateDefaultCharacterList()
    {
        availableCharacters = new List<CharacterData>
        {
            new CharacterData { name = "钢腕" },
            new CharacterData { name = "半魔游侠" },
            new CharacterData { name = "弓箭手" },
            new CharacterData { name = "暗夜猎手" },
            new CharacterData { name = "工程师" },
            new CharacterData { name = "投弹手" },
            new CharacterData { name = "圣光卫士" },
            new CharacterData { name = "至忠圣卫" }
        };
    }
    
    /// <summary>
    /// 创建角色选择按钮
    /// </summary>
    void CreateCharacterButtons()
    {
        // 清空容器
        foreach (Transform child in characterButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 为每个可用角色创建按钮
        foreach (var charData in availableCharacters)
        {
            GameObject btn = Instantiate(characterButtonPrefab, characterButtonContainer);
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = charData.name;
            }
            
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                CharacterData capturedChar = charData; // 闭包捕获
                button.onClick.AddListener(() => OnCharacterSelected(capturedChar));
            }
        }
    }
    
    /// <summary>
    /// 当选择角色时调用（新的逻辑：每个玩家选择两个角色）
    /// </summary>
    public void OnCharacterSelected(CharacterData character)
    {
        if (character == null)
        {
            Debug.LogError("[CharacterSelectionPanel] ❌ 选择的角色数据为空！");
            return;
        }
        
        Debug.Log($"[CharacterSelectionPanel] ⚡ 收到角色选择：{character.name}，当前阶段：{currentPhase}");
        
        // 根据当前阶段分配角色
        switch (currentPhase)
        {
            case SelectionPhase.Player1_Char1:
                selectedPlayer1Char1 = character;
                currentPhase = SelectionPhase.Player1_Char2;
                Debug.Log($"✅ 玩家1选择了第1个角色：{character.name}");
                Debug.Log($"➡️ 下一阶段：玩家1选择第2个角色");
                break;
                
            case SelectionPhase.Player1_Char2:
                selectedPlayer1Char2 = character;
                currentPhase = SelectionPhase.Player2_Char1;
                Debug.Log($"✅ 玩家1选择了第2个角色：{character.name}");
                Debug.Log($"➡️ 下一阶段：玩家2选择第1个角色");
                break;
                
            case SelectionPhase.Player2_Char1:
                selectedPlayer2Char1 = character;
                currentPhase = SelectionPhase.Player2_Char2;
                Debug.Log($"✅ 玩家2选择了第1个角色：{character.name}");
                Debug.Log($"➡️ 下一阶段：玩家2选择第2个角色");
                break;
                
            case SelectionPhase.Player2_Char2:
                selectedPlayer2Char2 = character;
                Debug.Log($"✅ 玩家2选择了第2个角色：{character.name}");
                Debug.Log("🎉 所有角色选择完成！确定按钮已启用，可以点击确定开始游戏。");
                // 选择完成后，确认按钮会自动启用
                break;
        }
        
        // 强制立即更新显示
        UpdateDisplay();
        
        // 使用协程确保UI更新
        StartCoroutine(ForceUpdateUI());
        
        // 添加音效反馈（可选）
        // AudioSource.PlayClipAtPoint(selectSound, Camera.main.transform.position);
    }
    
    /// <summary>
    /// 获取玩家1的队伍（两个角色）
    /// </summary>
    public List<CharacterData> GetPlayer1Team()
    {
        List<CharacterData> team = new List<CharacterData>();
        if (selectedPlayer1Char1 != null) team.Add(selectedPlayer1Char1);
        if (selectedPlayer1Char2 != null) team.Add(selectedPlayer1Char2);
        return team;
    }
    
    /// <summary>
    /// 获取玩家2的队伍（两个角色）
    /// </summary>
    public List<CharacterData> GetPlayer2Team()
    {
        List<CharacterData> team = new List<CharacterData>();
        if (selectedPlayer2Char1 != null) team.Add(selectedPlayer2Char1);
        if (selectedPlayer2Char2 != null) team.Add(selectedPlayer2Char2);
        return team;
    }
    
    /// <summary>
    /// 重置选择状态（用于重新选择）
    /// </summary>
    public void ResetSelection()
    {
        selectedPlayer1Char1 = null;
        selectedPlayer1Char2 = null;
        selectedPlayer2Char1 = null;
        selectedPlayer2Char2 = null;
        currentPhase = SelectionPhase.Player1_Char1;
        UpdateDisplay();
    }
    
    /// <summary>
    /// 更新显示文本
    /// </summary>
    void UpdateDisplay()
    {
        Debug.Log($"[CharacterSelectionPanel] UpdateDisplay called. P1Char1: {selectedPlayer1Char1?.name}, P1Char2: {selectedPlayer1Char2?.name}, P2Char1: {selectedPlayer2Char1?.name}, P2Char2: {selectedPlayer2Char2?.name}");
        
        // 更新阶段提示文本
        if (phaseText != null)
        {
            string phaseMessage = "";
            switch (currentPhase)
            {
                case SelectionPhase.Player1_Char1:
                    phaseMessage = "当前：玩家1选择第1个角色";
                    break;
                case SelectionPhase.Player1_Char2:
                    phaseMessage = "当前：玩家1选择第2个角色";
                    break;
                case SelectionPhase.Player2_Char1:
                    phaseMessage = "当前：玩家2选择第1个角色";
                    break;
                case SelectionPhase.Player2_Char2:
                    phaseMessage = "当前：玩家2选择第2个角色";
                    break;
            }
            phaseText.text = phaseMessage;
            SetChineseFont(phaseText);
            phaseText.ForceMeshUpdate();
            Debug.Log($"[CharacterSelectionPanel] Phase text updated: {phaseMessage}");
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionPanel] phaseText is null!");
        }
        
        // 更新玩家1文本
        if (player1Text != null)
        {
            string char1Name = selectedPlayer1Char1 != null ? selectedPlayer1Char1.name : "（未选择）";
            string char2Name = selectedPlayer1Char2 != null ? selectedPlayer1Char2.name : "（未选择）";
            player1Text.text = $"玩家1：角色1({char1Name})  角色2({char2Name})";
            SetChineseFont(player1Text);
            player1Text.ForceMeshUpdate();
            Debug.Log($"[CharacterSelectionPanel] Player1 text updated: {player1Text.text}");
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionPanel] player1Text is null!");
        }
        
        // 更新玩家2文本
        if (player2Text != null)
        {
            string char1Name = selectedPlayer2Char1 != null ? selectedPlayer2Char1.name : "（未选择）";
            string char2Name = selectedPlayer2Char2 != null ? selectedPlayer2Char2.name : "（未选择）";
            player2Text.text = $"玩家2：角色1({char1Name})  角色2({char2Name})";
            SetChineseFont(player2Text);
            player2Text.ForceMeshUpdate();
            Debug.Log($"[CharacterSelectionPanel] Player2 text updated: {player2Text.text}");
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionPanel] player2Text is null!");
        }
        
        // 更新确定按钮状态（两个玩家都选择了两个角色后才能确定）
        if (confirmButton != null)
        {
            bool allSelected = selectedPlayer1Char1 != null && selectedPlayer1Char2 != null &&
                             selectedPlayer2Char1 != null && selectedPlayer2Char2 != null;
            confirmButton.interactable = allSelected;
            Debug.Log($"[CharacterSelectionPanel] Confirm button interactable: {allSelected}");
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionPanel] confirmButton is null!");
        }
    }
    
    /// <summary>
    /// 确定按钮点击事件（传递4个角色给GameManager）
    /// </summary>
    void OnConfirmClicked()
    {
        // 验证所有角色都已选择
        if (selectedPlayer1Char1 == null || selectedPlayer1Char2 == null ||
            selectedPlayer2Char1 == null || selectedPlayer2Char2 == null)
        {
            Debug.LogWarning("[CharacterSelectionPanel] 请先为两个玩家各选择两个角色！");
            return;
        }
        
        Debug.Log($"[CharacterSelectionPanel] 确认选择完成");
        Debug.Log($"玩家1队伍：{selectedPlayer1Char1.name}, {selectedPlayer1Char2.name}");
        Debug.Log($"玩家2队伍：{selectedPlayer2Char1.name}, {selectedPlayer2Char2.name}");
        
        // 将CharacterData转换为Character对象列表（需要根据角色名称查找实际的Character对象）
        List<Character> player1Team = ConvertToCharacterList(selectedPlayer1Char1, selectedPlayer1Char2);
        List<Character> player2Team = ConvertToCharacterList(selectedPlayer2Char1, selectedPlayer2Char2);
        
        // 通知GameManager角色选择完成
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCharactersSelected(player1Team, player2Team);
            Debug.Log("[CharacterSelectionPanel] 已通知GameManager角色选择完成");
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionPanel] GameManager.Instance 为空，无法传递角色选择信息！");
        }
        
        // 隐藏面板
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 将CharacterData转换为Character对象列表（根据名称创建角色实例）
    /// </summary>
    List<Character> ConvertToCharacterList(CharacterData char1, CharacterData char2)
    {
        List<Character> team = new List<Character>();
        
        if (char1 != null)
        {
            Character character1 = CreateCharacterByName(char1.name);
            if (character1 != null)
            {
                team.Add(character1);
                Debug.Log($"[CharacterSelectionPanel] Created character: {char1.name}");
            }
            else
            {
                Debug.LogError($"[CharacterSelectionPanel] Failed to create character: {char1.name}");
            }
        }
        
        if (char2 != null)
        {
            Character character2 = CreateCharacterByName(char2.name);
            if (character2 != null)
            {
                team.Add(character2);
                Debug.Log($"[CharacterSelectionPanel] Created character: {char2.name}");
            }
            else
            {
                Debug.LogError($"[CharacterSelectionPanel] Failed to create character: {char2.name}");
            }
        }
        
        if (team.Count < 2)
        {
            Debug.LogError($"[CharacterSelectionPanel] Character creation failed! Only {team.Count}/2 created.");
        }
        
        return team;
    }
    
    /// <summary>
    /// 根据角色名称创建Character实例（快速创建，不依赖预制体）
    /// </summary>
    Character CreateCharacterByName(string characterName)
    {
        // Try to find from GameManager first
        Character found = FindCharacterByName(characterName);
        if (found != null)
        {
            return found;
        }
        
        // If not found, create a temporary GameObject with the character component
        GameObject charObj = new GameObject($"TempCharacter_{characterName}");
        
        Character character = null;
        
        // Map character name to character type
        switch (characterName)
        {
            case "钢腕":
                character = charObj.AddComponent<SteelArm>();
                break;
            case "半魔游侠":
                character = charObj.AddComponent<HalfDemonRanger>();
                break;
            case "弓箭手":
                character = charObj.AddComponent<Archer>();
                break;
            case "暗夜猎手":
                character = charObj.AddComponent<NightHunter>();
                break;
            case "工程师":
                character = charObj.AddComponent<Engineer>();
                break;
            case "投弹手":
                character = charObj.AddComponent<Bomber>();
                break;
            case "圣光卫士":
                character = charObj.AddComponent<HolyGuardian>();
                break;
            case "至忠圣卫":
                character = charObj.AddComponent<LoyalGuardian>();
                break;
            default:
                Debug.LogError($"[CharacterSelectionPanel] Unknown character name: {characterName}");
                Destroy(charObj);
                return null;
        }
        
        if (character != null)
        {
            character.Initialize();
            Debug.Log($"[CharacterSelectionPanel] Created character instance: {characterName}");
        }
        
        return character;
    }
    
    /// <summary>
    /// 根据角色名称查找Character对象（从GameManager或场景中查找）
    /// </summary>
    Character FindCharacterByName(string characterName)
    {
        // 方法1: 从GameManager查找
        if (GameManager.Instance != null && GameManager.Instance.allCharacters != null)
        {
            foreach (Character c in GameManager.Instance.allCharacters)
            {
                if (c != null && c.characterName == characterName)
                {
                    return c;
                }
            }
        }
        
        // 方法2: 从场景中查找所有Character对象
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character c in allCharacters)
        {
            if (c != null && c.characterName == characterName)
            {
                return c;
            }
        }
        
        Debug.LogWarning($"[CharacterSelectionPanel] 无法找到角色对象：{characterName}");
        return null;
    }
    
    /// <summary>
    /// 退出按钮点击事件
    /// </summary>
    void OnCancelClicked()
    {
        Debug.Log("[CharacterSelectionPanel] 取消角色选择，返回主菜单");
        
        // 尝试加载主菜单场景
        LoadMainMenuScene();
    }
    
    /// <summary>
    /// 加载主菜单场景
    /// </summary>
    void LoadMainMenuScene()
    {
        // 尝试多个可能的场景名称
        string[] possibleSceneNames = { "StartScene", "MainMenu", "SampleScene" };
        
        foreach (string sceneName in possibleSceneNames)
        {
            try
            {
                // 检查场景是否存在
                int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{sceneName}.unity");
                if (sceneIndex >= 0)
                {
                    Debug.Log($"[CharacterSelectionPanel] 加载场景: {sceneName}");
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CharacterSelectionPanel] 尝试加载场景 {sceneName} 失败: {e.Message}");
            }
        }
        
        // 如果都失败，尝试通过场景索引加载
        try
        {
            // 主菜单通常是第一个场景
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                Debug.Log("[CharacterSelectionPanel] 尝试加载场景索引 0");
                SceneManager.LoadScene(0);
            }
            else
            {
                Debug.LogError("[CharacterSelectionPanel] 没有找到可用的场景！请确保场景已添加到 Build Settings。");
                Debug.LogError("[CharacterSelectionPanel] File > Build Settings > 添加场景到列表");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterSelectionPanel] 场景加载失败: {e.Message}");
            Debug.LogError("[CharacterSelectionPanel] 请手动在 Unity 编辑器中配置场景加载");
        }
    }
    
    /// <summary>
    /// 显示选择面板
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            // 重置选择状态
            ResetSelection();
        }
    }
    
    /// <summary>
    /// 隐藏选择面板
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}

/// <summary>
/// 角色数据类（用于选择界面）
/// </summary>
[System.Serializable]
public class CharacterData
{
    public string name;
    public string faction;
    public string ability;
    
    public CharacterData()
    {
        name = "";
        faction = "";
        ability = "";
    }
    
    public CharacterData(string characterName)
    {
        name = characterName;
        faction = "";
        ability = "";
    }
}

