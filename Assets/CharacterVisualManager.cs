using UnityEngine;

/// <summary>
/// 角色视觉管理器：负责角色动画、精灵显示和交互
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterVisualManager : MonoBehaviour
{
    [Header("动画控制器引用")]
    public RuntimeAnimatorController idleController;     // 待机动画控制器
    public RuntimeAnimatorController moveController;     // 移动动画控制器
    public RuntimeAnimatorController attackController;   // 攻击动画控制器
    public RuntimeAnimatorController skillController;    // 技能动画控制器

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Character character;

    // 动画状态哈希（用于优化性能）
    private int idleStateHash = Animator.StringToHash("Base Layer.Idle");
    private int moveStateHash = Animator.StringToHash("Base Layer.Move");
    private int attackStateHash = Animator.StringToHash("Base Layer.Attack");
    private int skillStateHash = Animator.StringToHash("Base Layer.Skill");

    // 当前动画状态
    private enum AnimationState
    {
        Idle,
        Move,
        Attack,
        Skill
    }
    private AnimationState currentState = AnimationState.Idle;

    void Awake()
    {
        // 获取组件
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        character = GetComponent<Character>();

        // 如果没有Animator，创建一个
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
            Debug.Log($"[CharacterVisualManager] {gameObject.name} added Animator component");
        }

        // 如果没有SpriteRenderer，创建一个
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 10; // 确保角色显示在地图上方
            Debug.Log($"[CharacterVisualManager] {gameObject.name} added SpriteRenderer component");
        }
        
        // 确保SpriteRenderer已启用且可见
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = 10;
            spriteRenderer.color = Color.white;
        }
    }

    void Start()
    {
        // Wait a frame for Initialize to be called
        StartCoroutine(DelayedStart());
    }

    System.Collections.IEnumerator DelayedStart()
    {
        yield return null;
        
        // Set initial animation to idle
        if (idleController != null && animator != null)
        {
            animator.runtimeAnimatorController = idleController;
            PlayIdle();
        }
        else if (animator != null && idleController == null)
        {
            // Try to get controller from animator if already set
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[CharacterVisualManager] {gameObject.name} missing idleController, cannot play idle animation");
            }
        }
    }

    /// <summary>
    /// 初始化视觉管理器（设置动画控制器）
    /// </summary>
    public void Initialize(RuntimeAnimatorController idle, 
                          RuntimeAnimatorController move = null, 
                          RuntimeAnimatorController attack = null, 
                          RuntimeAnimatorController skill = null)
    {
        idleController = idle;
        moveController = move;
        attackController = attack;
        skillController = skill;

        // Ensure animator exists
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
            }
        }

        // Set initial animation
        if (idleController != null && animator != null)
        {
            animator.runtimeAnimatorController = idleController;
            animator.enabled = true;
            
            // 强制更新一次，确保sprite显示
            animator.Update(0.01f);
            
            // Play idle animation
            if (animator.isInitialized)
            {
                PlayIdle();
            }
            else
            {
                StartCoroutine(PlayIdleWhenReady());
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterVisualManager] {gameObject.name} Initialize called but idleController is null");
            
            // 即使没有动画控制器，也要确保SpriteRenderer可见
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                // 尝试从动画文件夹加载一个默认sprite
                LoadDefaultSprite();
            }
        }
    }

    System.Collections.IEnumerator PlayIdleWhenReady()
    {
        while (!animator.isInitialized)
        {
            yield return null;
        }
        PlayIdle();
    }

    /// <summary>
    /// 播放待机动画
    /// </summary>
    public void PlayIdle()
    {
        if (animator == null || idleController == null)
        {
            Debug.LogWarning($"[CharacterVisualManager] {gameObject.name} cannot play idle: animator={animator != null}, controller={idleController != null}");
            return;
        }

        if (currentState != AnimationState.Idle)
        {
            animator.runtimeAnimatorController = idleController;
            animator.enabled = true;
            
            // Force update animator
            animator.Update(0f);
            
            // Try different possible state names
            if (animator.HasState(0, Animator.StringToHash("Idle")))
            {
                animator.Play("Idle", 0, 0);
            }
            else if (animator.HasState(0, Animator.StringToHash("Base Layer.Idle")))
            {
                animator.Play("Base Layer.Idle", 0, 0);
            }
            else
            {
                // Try to play first state
                animator.Play(0, 0, 0);
            }
            
            currentState = AnimationState.Idle;
            
            // Ensure sprite renderer is visible
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = 10;
            }
            
            Debug.Log($"[CharacterVisualManager] {gameObject.name} playing idle animation, spriteEnabled={spriteRenderer != null && spriteRenderer.enabled}");
        }
    }

    /// <summary>
    /// 播放移动动画
    /// </summary>
    public void PlayMove()
    {
        if (animator == null || moveController == null)
        {
            Debug.LogWarning($"[CharacterVisualManager] {gameObject.name} missing moveController");
            return;
        }

        animator.runtimeAnimatorController = moveController;
        animator.Play("Base Layer.Move", 0, 0);
        currentState = AnimationState.Move;
        Debug.Log($"[CharacterVisualManager] {gameObject.name} playing move animation");
    }

    /// <summary>
    /// 播放攻击动画
    /// </summary>
    public void PlayAttack(System.Action onComplete = null)
    {
        if (animator == null || attackController == null)
        {
            Debug.LogWarning($"[CharacterVisualManager] {gameObject.name} missing attackController");
            onComplete?.Invoke();
            return;
        }

        animator.runtimeAnimatorController = attackController;
        animator.Play("Base Layer.Attack", 0, 0);
        currentState = AnimationState.Attack;

        // 等待动画播放完成后回调
        if (onComplete != null)
        {
            StartCoroutine(WaitForAnimationComplete(onComplete));
        }

        Debug.Log($"[CharacterVisualManager] {gameObject.name} playing attack animation");
    }

    /// <summary>
    /// 播放技能动画
    /// </summary>
    public void PlaySkill(System.Action onComplete = null)
    {
        if (animator == null || skillController == null)
        {
            Debug.LogWarning($"[CharacterVisualManager] {gameObject.name} missing skillController");
            onComplete?.Invoke();
            return;
        }

        animator.runtimeAnimatorController = skillController;
        animator.Play("Base Layer.Skill", 0, 0);
        currentState = AnimationState.Skill;

        // 等待动画播放完成后回调
        if (onComplete != null)
        {
            StartCoroutine(WaitForAnimationComplete(onComplete));
        }

        Debug.Log($"[CharacterVisualManager] {gameObject.name} playing skill animation");
    }

    /// <summary>
    /// 等待动画播放完成
    /// </summary>
    private System.Collections.IEnumerator WaitForAnimationComplete(System.Action onComplete)
    {
        // 等待动画开始
        yield return null;
        yield return null;

        // 等待动画播放完成
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        // 动画播放完成，回到待机
        PlayIdle();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 设置角色朝向（面向目标位置）
    /// </summary>
    public void FaceDirection(Vector3 targetPosition)
    {
        if (spriteRenderer == null) return;

        Vector3 direction = targetPosition - transform.position;
        
        // 根据X方向决定翻转（面向右侧为正常，面向左侧翻转）
        if (direction.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (direction.x > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    /// <summary>
    /// 设置角色颜色（用于区分队伍）
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 设置排序顺序（确保角色显示在正确的层级）
    /// </summary>
    public void SetSortingOrder(int order)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }

    /// <summary>
    /// 设置是否可见（用于死亡等状态）
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }
    
    /// <summary>
    /// 加载默认sprite（当动画未加载时使用）
    /// </summary>
    void LoadDefaultSprite()
    {
        if (spriteRenderer == null || character == null) return;
        
        // 尝试从动画文件夹加载第一帧sprite
        string characterName = character.characterName;
        
        #if UNITY_EDITOR
        string spritePath = "";
        switch (characterName)
        {
            case "钢腕":
                spritePath = "Assets/animation/iron wait/images/铁腕待机_01.png";
                break;
            case "工程师":
                spritePath = "Assets/animation/gongcheng wait/images/工程师待机_01.png";
                break;
            case "弓箭手":
                spritePath = "Assets/animation/shooter wait/images/弓箭手待机_01.png";
                break;
            case "半魔游侠":
                spritePath = "Assets/animation/youxia wait/images/游侠待机_01.png";
                break;
            case "暗夜猎手":
                spritePath = "Assets/animation/anying wait/images/暗影待机_01.png";
                break;
            case "投弹手":
                spritePath = "Assets/animation/zhadan wait/images/炸弹兵待机_01.png";
                break;
            case "圣光卫士":
                spritePath = "Assets/animation/shengguang wait/images/圣光卫士待机_01.png";
                break;
            case "至忠圣卫":
                spritePath = "Assets/animation/zhizhong wait/images/至忠圣卫待机_01.png";
                break;
        }
        
        if (!string.IsNullOrEmpty(spritePath))
        {
            try
            {
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
                if (texture != null)
                {
                    // 将Texture2D转换为Sprite
                    Sprite defaultSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    if (defaultSprite != null)
                    {
                        spriteRenderer.sprite = defaultSprite;
                        Debug.Log($"[CharacterVisualManager] Loaded default sprite for {characterName}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CharacterVisualManager] Failed to load default sprite: {e.Message}");
            }
        }
        #endif
    }
}

