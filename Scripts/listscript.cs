using UnityEngine;

/// <summary>
/// 所有角色的基类
/// </summary>
public abstract class Character : MonoBehaviour
{
    [Header("角色基本信息")]
    public string characterName;
    public int health;
    public int maxHealth; // 最大生命值
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

    [Header("状态标识")]
    public bool isAlive = true;
    public int reviveCount = 1; // 复活次数
    public bool canActThisTurn = true; // 本回合是否可以行动（用于工程师的机械臂）
    public bool usedAbilityThisGame = false; // 主动技能是否已使用（复活后重置）
    public bool wasAttackedThisTurn = false; // 本回合是否被攻击（用于钢腕的技能）

    [Header("位置信息")]
    public int gridX = -1;
    public int gridY = -1;

    /// <summary>
    /// 初始化角色（由 GameManager 调用）
    /// </summary>
    public virtual void Initialize()
    {
        isAlive = true;
        maxHealth = health;
        canActThisTurn = true;
        usedAbilityThisGame = false;
        wasAttackedThisTurn = false;
        reviveCount = 1;
        Debug.Log($"{characterName} 初始化完成");
    }

    /// <summary>
    /// 角色受伤逻辑
    /// 规则：护盾优先级大于生命值，受到伤害先扣除护盾，护盾被击破后剩余伤害扣除生命值
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        wasAttackedThisTurn = true;
        int originalDamage = damage;
        int healthDamage = 0;

        // 规则：护盾优先级大于生命值，先扣除护盾
        if (shield > 0)
        {
            int shieldBefore = shield;
            // 计算护盾能吸收的伤害（不能超过护盾值）
            int shieldAbsorb = Mathf.Min(shield, damage);
            
            // 扣除护盾值
            shield -= shieldAbsorb;
            // 确保护盾不会为负数（安全措施）
            if (shield < 0) shield = 0;
            
            // 计算剩余的伤害（护盾被击破后，剩余伤害会扣除生命值）
            int remainingDamage = damage - shieldAbsorb;
            
            Debug.Log($"{characterName} 受到 {originalDamage} 点伤害，护盾吸收 {shieldAbsorb} 点（{shieldBefore} -> {shield}），剩余 {remainingDamage} 点伤害");
            
            // 重要：护盾被击破后，剩余伤害必须扣除生命值
            if (remainingDamage > 0)
            {
                healthDamage = remainingDamage;
            }
        }
        else
        {
            // 没有护盾，所有伤害直接扣除生命值
            healthDamage = damage;
            Debug.Log($"{characterName} 没有护盾，受到 {originalDamage} 点伤害，全部扣除生命值");
        }

        // 扣除生命值（护盾被击破后的剩余伤害）
        if (healthDamage > 0)
        {
            health -= healthDamage;
            Debug.Log($"{characterName} 生命值减少 {healthDamage} 点，当前生命值：{health}");
            
            if (health <= 0)
            {
                Die();
            }
        }
    }

    /// <summary>
    /// 攻击目标
    /// </summary>
    public virtual void Attack(Character target)
    {
        if (!canAttack)
        {
            Debug.Log($"{characterName} 无法攻击！");
            return;
        }

        // 规则：攻击只能攻击相邻方格内的角色
        int dx = Mathf.Abs(target.gridX - gridX);
        int dy = Mathf.Abs(target.gridY - gridY);
        int distance = dx + dy;
        
        // 计算有效攻击距离（考虑角色技能）
        int maxAttackDistance = GetEffectiveAttackDistance();
        
        // 弓箭手的"远视"技能可以攻击2格，但基础规则是相邻（1格）
        // 根据规则，严格限制为相邻或技能允许的距离
        if (distance > maxAttackDistance)
        {
            Debug.Log($"{characterName} 无法攻击 {target.characterName}：距离过远（当前距离：{distance}，最大攻击距离：{maxAttackDistance}）");
            return;
        }

        // 规则：战壕 - 在其中只能被相邻方格中的人锁定
        GridTile targetTile = GridManager.Instance.GetTileAt(target.gridX, target.gridY);
        if (targetTile != null && targetTile.tileType == TileType.Trench)
        {
            if (distance > 1)
            {
                Debug.Log($"{characterName} 无法攻击 {target.characterName}：目标在战壕中，只能被相邻方格锁定");
                return;
            }
        }

        // 规则：检查路径是否被障碍阻挡（弓箭手可以攻击2格，但无法穿过障碍）
        // 攻击/技能可以越过污染水域，但不能穿过障碍
        if (distance > 1 && !GridManager.Instance.IsPathClear(gridX, gridY, target.gridX, target.gridY, allowPollutedWater: true))
        {
            Debug.Log($"{characterName} 无法攻击 {target.characterName}：路径被障碍阻挡");
            return;
        }

        int finalDamage = attackDamage;
        // 荆棘战士：血量<50%时伤害+2
        if (this is ThornWarrior && health < maxHealth * 0.5f)
        {
            finalDamage += 2;
        }

        Debug.Log($"{characterName} 攻击 {target.characterName}，造成 {finalDamage} 点伤害");
        target.TakeDamage(finalDamage);
    }

    /// <summary>
    /// 恢复护盾
    /// </summary>
    public virtual void RestoreShield(int amount = 1)
    {
        shield += amount;
        Debug.Log($"{characterName} 恢复 {amount} 点护盾，当前护盾：{shield}");
    }

    /// <summary>
    /// 角色死亡逻辑
    /// </summary>
    protected virtual void Die()
    {
        if (!isAlive) return;
        isAlive = false;
        Debug.Log($"{characterName} 已死亡");
        
        // 如果还有复活次数，通知BattleManager处理复活
        if (reviveCount > 0 && BattleManager.Instance != null)
        {
            // 延迟一帧处理，避免在TakeDamage中直接调用
            // BattleManager会在回合结束时检查并处理复活
        }
    }

    /// <summary>
    /// 复活角色
    /// </summary>
    public virtual void Revive(int x, int y)
    {
        if (reviveCount <= 0)
        {
            Debug.Log($"{characterName} 没有复活机会");
            return;
        }

        reviveCount--;
        isAlive = true;
        health = maxHealth;
        shield = 0;
        usedAbilityThisGame = false; // 规则：复活后重置主动技能使用次数
        canActThisTurn = false; // 规则：复活后的下一回合无法行动
        gridX = x;
        gridY = y;
        transform.position = GridManager.Instance.GetWorldPosition(x, y);
        Debug.Log($"{characterName} 在 ({x}, {y}) 复活，剩余复活次数：{reviveCount}，下一回合无法行动");
    }

    /// <summary>
    /// 获取实际移动距离（考虑被动技能）
    /// </summary>
    public virtual int GetEffectiveMoveDistance()
    {
        int distance = moveDistance;
        // 半魔游侠：未受伤时移动距离+1
        if (this is HalfDemonRanger && health == maxHealth)
        {
            distance += 1;
        }
        // 至忠圣卫：在己方半场时移动+1（需要在GridManager中判断）
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
        // 注意：wasAttackedThisTurn 在子类中可能被使用，所以要先处理
        wasAttackedThisTurn = false;
        // 钢腕的处理在其自己的OnTurnStart中完成
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
