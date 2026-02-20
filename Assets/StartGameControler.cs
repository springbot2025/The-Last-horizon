using UnityEngine;
using UnityEngine.SceneManagement; // 必须引用场景管理命名空间

public class StartGameController : MonoBehaviour
{
    void Update()
    {
        // 玩家点击鼠标左键或按下任意键时跳转
        if (Input.anyKeyDown)
        {
            // 加载你 Build Settings 中索引为 1 的 MapScene
            SceneManager.LoadScene(1);
        }
    }
}