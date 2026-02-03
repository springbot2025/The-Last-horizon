using UnityEngine;

/// <summary>
/// 所有角色的基类
/// </summary>
public abstract class Character : MonoBehaviour
{
    [Header("角色基本信息")]
    public string characterName;
    public int health;
    public int maxHealth; // 最大生命
    public int shield;
    public string faction;
    public string abilityName;
    public string abilityDescription;
    [TextArea]
    public string background;

    [Header("战斗属性")]
    public int moveDistance = 1; // 移动距离
    public int attackDistance = 1; // 攻击距离
    public int attackDamage = 1; // 基础攻击伤害
    public bool canAttack = true; // 是否可以攻击（工程师和投弹手不能攻击）

    [Header("状态")]
    public bool isAlive = true;
    public int reviveCount = 1; // 复活次数
    public bool canActThisTurn = true; // 本回合是否可以行动（用于工程师的机械臂）
    public bool usedAbilityThisGame = false; // 主动技能是否已使用（复活后重置）
    public bool wasAttackedThisTurn = false; // 本回合是否被攻击（用于钢腕的技能）

    [Header("位置信息")]
    public int gridX = -1;
    public int gridY = -1;

    [Header("Visual Components")]
    public CharacterVisualManager visualManager;

    /// <summary>
    /// 初始化角色
    /// </summary>
    public virtual void Initialize()
    {
        isAlive = true;
        maxHealth = health;
        canActThisTurn = true;
        usedAbilityThisGame = false;
        wasAttackedThisTurn = false;
        reviveCount = 1;

        // Ensure visual manager exists
        if (visualManager == null)
        {
            visualManager = GetComponent<CharacterVisualManager>();
            if (visualManager == null)
            {
                visualManager = gameObject.AddComponent<CharacterVisualManager>();
                Debug.Log($"[Character] {characterName} added CharacterVisualManager component");
            }
        }

        // Load animation controllers
        CharacterAnimationLoader.LoadAnimationsForCharacter(this);

        // Set initial visual state
        if (visualManager != null && isAlive)
        {
            visualManager.SetVisible(true);
            visualManager.PlayIdle();
        }

        Debug.Log($"{characterName} initialized");
    }

    /// <summary>
    /// 角色受伤逻辑
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        wasAttackedThisTurn = true;
        int remainingDamage = damage;

        if (shield > 0)
        {
            int shieldAbsorb = Mathf.Min(shield, remainingDamage);
            shield -= shieldAbsorb;
            remainingDamage -= shieldAbsorb;
        }

        if (remainingDamage > 0)
        {
            health -= remainingDamage;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 攻击目标
    /// </summary>
    public virtual void Attack(Character target)
    {
        if (!canAttack)
        {
            Debug.Log($"{characterName} cannot attack");
            return;
        }

        int finalDamage = attackDamage;

        // Face target
        if (visualManager != null && target != null)
        {
            visualManager.FaceDirection(target.transform.position);
            // Play attack animation
            visualManager.PlayAttack(() =>
            {
                // Deal damage after animation
                Debug.Log($"{characterName} attacks {target.characterName} for {finalDamage} damage");
                target.TakeDamage(finalDamage);
            });
        }
        else
        {
            // If no visual manager, deal damage directly
            Debug.Log($"{characterName} attacks {target.characterName} for {finalDamage} damage");
            target.TakeDamage(finalDamage);
        }
    }

    /// <summary>
    /// 恢复护盾
    /// </summary>
    public virtual void RestoreShield(int amount = 1)
    {
        shield += amount;
        Debug.Log($"{characterName} restores {amount} shield. Current shield: {shield}");
    }

    /// <summary>
    /// 角色死亡逻辑
    /// </summary>
    public virtual void Die()
    {
        if (!isAlive) return;
        isAlive = false;
        Debug.Log($"{characterName} died");
        
        // Hide visual
        if (visualManager != null)
        {
            visualManager.SetVisible(false);
        }
        
        // If has revive count, notify BattleManager
        if (reviveCount > 0 && BattleManager.Instance != null)
        {
            // Delay one frame to avoid calling directly in TakeDamage
            // BattleManager will check and handle revive at end of turn
        }
    }

    /// <summary>
    /// 复活角色
    /// </summary>
    public virtual void Revive(int x, int y)
    {
        if (reviveCount <= 0)
        {
            Debug.Log($"{characterName} has no revive chance");
            return;
        }

        reviveCount--;
        isAlive = true;
        health = maxHealth;
        shield = 0;
        usedAbilityThisGame = false; // 复活后重置主动技能使用次数
        gridX = x;
        gridY = y;
        transform.position = GridManager.Instance.GetWorldPosition(x, y);
        
        // Show visual and play idle animation
        if (visualManager != null)
        {
            visualManager.SetVisible(true);
            visualManager.PlayIdle();
        }
        
        Debug.Log($"{characterName} revived at ({x}, {y}). Remaining revives: {reviveCount}");
    }

    /// <summary>
    /// 获取实际移动距离（考虑被动技能）
    /// </summary>
    public virtual int GetEffectiveMoveDistance()
    {
        int distance = moveDistance;
        // 半魔游侠：未受伤时移动距1
        if (this is HalfDemonRanger && health == maxHealth)
        {
            distance += 1;
        }
        // 至忠圣卫：在己方半场时移1（需要在GridManager中判断）
        return distance;
    }

    /// <summary>
    /// 获取实际攻击距离（考虑被动技能）
    /// </summary>
    public virtual int GetEffectiveAttackDistance()
    {
        int distance = attackDistance;
        // 弓箭手：攻击距离+1
        if (this is Archer)
        {
            distance += 1;
        }
        return distance;
    }

    /// <summary>
    /// 回合开始时的处理
    /// </summary>
    public virtual void OnTurnStart()
    {
        wasAttackedThisTurn = false;
        // 钢腕：如果上一回合未被攻击，可以激活护
        // 注意：需要在回合开始时检查上一回合的状态
    }

    /// <summary>
    /// 回合结束时的处理
    /// </summary>
    public virtual void OnTurnEnd()
    {
        canActThisTurn = true; // 重置行动限制（工程师的机械臂）
    }

    /// <summary>
    /// 子类必须实现的主动技能逻辑
    /// </summary>
    public abstract void UseAbility();
}
