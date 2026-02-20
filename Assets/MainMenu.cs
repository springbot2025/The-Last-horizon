using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // 设置界面（在 Inspector 中拖上去）
    public GameObject settingsCanvas;
    
    // Start界面（在 Inspector 中拖上去）
    public GameObject startCanvas;

    // 点击Settings按钮时调用
    public void OpenSettings()
    {
        settingsCanvas.SetActive(true);
    }

    // 点击关闭按钮时调用
    public void CloseSettings()
    {
        settingsCanvas.SetActive(false);
    }
    
    // 点击Start按钮时调用
    public void StartGame()
    {
        Debug.Log("[MainMenu] StartGame 被调用！");
        Debug.Log("[MainMenu] 当前场景: " + SceneManager.GetActiveScene().name);
        
        // 直接跳转到 MapScene（跳过 StartScene）
        Debug.Log("[MainMenu] 准备加载场景: MapScene");
        
        // 检查 MapScene 是否在 Build Settings 中
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/MapScene.unity");
        if (sceneIndex < 0)
        {
            Debug.LogError("[MainMenu] 错误：MapScene 未添加到 Build Settings！");
            Debug.LogError("[MainMenu] 请按以下步骤操作：");
            Debug.LogError("[MainMenu] 1. 在 Unity 中点击 File > Build Settings");
            Debug.LogError("[MainMenu] 2. 点击 Add Open Scenes 或手动拖拽 MapScene.unity 到列表");
            Debug.LogError("[MainMenu] 3. 确保 MapScene 前面的复选框已勾选");
            return;
        }
        
        Debug.Log($"[MainMenu] 找到 MapScene，索引: {sceneIndex}");
        Debug.Log("[MainMenu] 开始加载 MapScene...");
        
        try
        {
            SceneManager.LoadScene("MapScene", LoadSceneMode.Single);
            Debug.Log("[MainMenu] MapScene 加载请求已发送");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MainMenu] 场景加载失败: {e.Message}");
            Debug.LogError($"[MainMenu] 堆栈跟踪: {e.StackTrace}");
        }
    }
}
