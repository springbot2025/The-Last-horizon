using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class startmanager : MonoBehaviour
{
    // Start界面管理脚本
    
    void Start()
    {
        // 初始化时可以在这里添加逻辑
        Debug.Log("Start界面已打开");
    }

    void Update()
    {
        // 可以在这里添加更新逻辑
    }
    
    // 关闭Start界面
    public void CloseStartPanel()
    {
        gameObject.SetActive(false);
    }
    
    // 进入地图界面
    public void EnterMapScene()
    {
        Debug.Log("[startmanager] EnterMapScene 被调用！");
        Debug.Log("[startmanager] 当前场景: " + SceneManager.GetActiveScene().name);
        Debug.Log("[startmanager] 准备加载场景: MapScene");
        
        // 检查场景是否存在
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/MapScene.unity");
        if (sceneIndex < 0)
        {
            Debug.LogError("[startmanager] 错误：MapScene 未添加到 Build Settings！");
            Debug.LogError("[startmanager] 请在 File > Build Settings 中添加 MapScene。");
            return;
        }
        else
        {
            Debug.Log($"[startmanager] 找到 MapScene，索引: {sceneIndex}");
        }
        
        SceneManager.LoadScene("MapScene");
        Debug.Log("[startmanager] 场景加载请求已发送");
    }
}
