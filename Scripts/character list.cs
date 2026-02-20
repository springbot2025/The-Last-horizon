using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// =====================================
// 钢腕
// =====================================
public class SteelArm : Character
{
    private bool canActivateShield = false;
    private bool hasMagicShield = false; // 是否激活了魔力手盾（可抵挡一次伤害）

    public override void Initialize()
    {
        base.Initialize();
        characterName = "钢腕";
        health = 3;
        shield = 1;
        faction = "地球联合国";
        abilityName = "魔力手盾";
        abilityDescription = "一回合未被攻击后可主动展开，抵挡一次伤害。";
        background = "墨丘集团和赫拉共和国共同生产的第三代改造人，也是第一代自主意识型改造人";
        moveDistance = 1;
        attackDistance = 1;
        attackDamage = 1;
        canAttack = true;
        canActivateShield = false;
        hasMagicShield = false;
    }

    public override void UseAbility()
    {
        if (usedAbilityThisGame)
        {
            Debug.Log("钢腕：主动技能本局游戏只能使用一次！");
            return;
        }

        if (canActivateShield)
        {
            hasMagicShield = true;
            canActivateShield = false;
            usedAbilityThisGame = true;
            Debug.Log("钢腕展开魔力手盾！可以抵挡一次伤害。");
        }
        else
        {
            Debug.Log("钢腕目前无法展开护盾（本回合已被攻击）。");
        }
    }

    // 重写TakeDamage以处理魔力手盾
    public override void TakeDamage(int damage)
    {
        // 如果激活了魔力手盾，完全抵挡这次伤害
        if (hasMagicShield)
        {
            hasMagicShield = false;
            Debug.Log("魔力手盾抵挡了这次伤害！");
            return; // 完全免疫这次伤害
        }
        
        base.TakeDamage(damage);
    }

    public override void OnTurnStart()
    {
        // 先保存上一回合是否被攻击的状态
        bool wasAttackedLastTurn = wasAttackedThisTurn;
        
        base.OnTurnStart(); // 这会重置wasAttackedThisTurn为false
        
        // 规则：一回合未被攻击后可激活手盾
        // 如果上一回合未被攻击，且还没有使用过技能，则可以激活
        if (!wasAttackedLastTurn && !usedAbilityThisGame)
        {
            canActivateShield = true;
        }
        else
        {
            canActivateShield = false;
        }
    }
}

// =====================================
// 半魔游侠
// =====================================
public class HalfDemonRanger : Character
{
    public override void Initialize()
    {
        base.Initialize();
        characterName = "半魔游侠";
        health = 3;
        shield = 0;
        faction = "无";
        abilityName = "迅捷";
        abilityDescription = "未受伤时移动距离+1。";
        background = "一个爱管闲事的人罢了。";
        moveDistance = 1;
        attackDistance = 1;
        attackDamage = 1;
        canAttack = true;
    }

    public override void UseAbility()
    {
        // 迅捷是被动技能，不需要主动使用
        Debug.Log("迅捷是被动技能，未受伤时自动生效（移动距离+1）");
    }
}

// =====================================
// 弓箭手
// =====================================
public class Archer : Character
{
    public override void Initialize()
    {
        base.Initialize();
        characterName = "弓箭手";
        health = 3;
        shield = 0;
        faction = "避风港";
        abilityName = "远视";
        abilityDescription = "攻击距离+1（无法穿墙）。";
        background = "实验录音：弓箭形态的压缩魔能弹药威力更大。";
        moveDistance = 1;
        attackDistance = 1; // 基础攻击距离，被动技能会在GetEffectiveAttackDistance中处理
        attackDamage = 1;
        canAttack = true;
    }

    public override void UseAbility()
    {
        // 远视是被动技能，不需要主动使用
        Debug.Log("远视是被动技能，自动生效（攻击距离+1）");
    }
}

// =====================================
// 暗夜猎手
// =====================================
public class NightHunter : Character
{
    public override void Initialize()
    {
        base.Initialize();
        characterName = "暗夜猎手";
        health = 3;
        shield = 0;
        faction = "避风港";
        abilityName = "突袭 / 灵动";
        abilityDescription = "突袭：若范围为2内（不能越过障碍）任意一格存在血量不大于2的目标，位移至该格并将其斩杀（无法越过障碍）。灵动：无视障碍移动。";
        background = "他们没有正邪之分，只做该做的事。";
        moveDistance = 1;
        attackDistance = 1;
        attackDamage = 1;
        canAttack = true;
    }

    // 灵动：被动技能，无视障碍移动
    public bool CanIgnoreObstacles()
    {
        // 规则：灵动（被动）：无视障碍移动
        // 暗夜猎手总是可以无视障碍移动
        return true;
    }

    // 突袭：主动技能
    public override void UseAbility()
    {
        if (usedAbilityThisGame)
        {
            Debug.Log("暗夜猎手：主动技能本局游戏只能使用一次！");
            return;
        }

        // 查找范围内血量≤2的目标
        List<Character> targets = FindTargetsForAssassinate(2);
        if (targets.Count > 0)
        {
            Character target = targets[0]; // 选择第一个目标
            // 移动到目标位置并斩杀
            if (GridManager.Instance.MoveCharacter(this, target.gridX, target.gridY))
            {
                target.Die(); // 直接斩杀
                usedAbilityThisGame = true;
                Debug.Log($"暗夜猎手使用突袭，斩杀 {target.characterName}！");
            }
        }
        else
        {
            Debug.Log("暗夜猎手：范围内没有符合条件的目标（血量≤2）");
        }
    }

    private List<Character> FindTargetsForAssassinate(int range)
    {
        List<Character> targets = new List<Character>();
        // 获取所有角色
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(BattleManager.Instance.player1Team);
        allCharacters.AddRange(BattleManager.Instance.player2Team);

        foreach (var character in allCharacters)
        {
            if (!character.isAlive || character == this) continue;
            if (character.faction == this.faction) continue; // 不攻击同阵营

            int distance = Mathf.Abs(character.gridX - gridX) + Mathf.Abs(character.gridY - gridY);
            if (distance <= range && character.health <= 2)
            {
                // 规则：突袭不能越过障碍进行移动和攻击
                // 路径可以越过污染水域（技能特性），但目标位置不能是污染水域（移动不能进入）
                GridTile targetTile = GridManager.Instance.GetTileAt(character.gridX, character.gridY);
                if (targetTile != null && targetTile.tileType == TileType.PollutedWater)
                {
                    continue; // 目标位置是污染水域，无法移动到那里
                }
                
                // 检查路径：不能穿过障碍，但可以越过污染水域
                if (GridManager.Instance.IsPathClear(gridX, gridY, character.gridX, character.gridY, allowPollutedWater: true))
                {
                    targets.Add(character);
                }
            }
        }
        return targets;
    }
}

// =====================================
// 工程师
// =====================================
public class Engineer : Character
{
    public override void Initialize()
    {
        base.Initialize();
        characterName = "工程师";
        health = 3;
        shield = 1;
        faction = "寰宇联合国";
        abilityName = "机械臂";
        abilityDescription = "3x3范围内移动单位；敌方则使其下回合无法行动。";
        background = "老去的技术人员们，为了后方的孩子再次上战场。";
        moveDistance = 1;
        attackDistance = 0;
        attackDamage = 0;
        canAttack = false; // 工程师无法攻击
    }

    public override void UseAbility()
    {
        if (usedAbilityThisGame)
        {
            Debug.Log("工程师：主动技能本局游戏只能使用一次！");
            return;
        }

        // 选择3x3范围内的角色
        // 这个需要在UI中选择目标，这里简化处理
        Debug.Log("机械臂：选择3x3范围内的角色进行移动");
        // TODO: 实现UI选择目标和移动逻辑
        usedAbilityThisGame = true;
    }

    // 机械臂：将3x3范围内任意角色移动至范围内另一格
    public void MoveTarget(Character target, int targetX, int targetY)
    {
        // 检查目标是否在3x3范围内
        int dx = Mathf.Abs(target.gridX - gridX);
        int dy = Mathf.Abs(target.gridY - gridY);
        if (dx > 1 || dy > 1)
        {
            Debug.Log("目标不在3x3范围内！");
            return;
        }
        
        // 检查目标位置是否在3x3范围内
        int targetDx = Mathf.Abs(targetX - gridX);
        int targetDy = Mathf.Abs(targetY - gridY);
        if (targetDx > 1 || targetDy > 1)
        {
            Debug.Log("目标位置不在3x3范围内！");
            return;
        }
        
        // 检查目标位置是否可走（机械臂移动需要考虑地形限制）
        GridTile targetTile = GridManager.Instance.GetTileAt(targetX, targetY);
        if (targetTile == null || !targetTile.isWalkable)
        {
            Debug.Log("目标位置不可走！");
            return;
        }
        
        // 检查目标位置是否被占用
        // （这里需要检查是否被其他角色占用，但机械臂可能可以移动角色到同一位置）
        // 简化：不允许移动到已被占用的位置
        
        // 移动目标角色（机械臂强制移动，但仍需考虑地形）
        int oldX = target.gridX;
        int oldY = target.gridY;
        
        target.gridX = targetX;
        target.gridY = targetY;
        target.transform.position = GridManager.Instance.GetWorldPosition(targetX, targetY);
        
        Debug.Log($"工程师使用机械臂将 {target.characterName} 从 ({oldX}, {oldY}) 移动到 ({targetX}, {targetY})");
        
        // 如果是敌方，使其下回合无法行动
        if (target.faction != this.faction)
        {
            target.canActThisTurn = false;
            Debug.Log($"{target.characterName} 下回合无法行动！");
        }
    }
}

// =====================================
// 投弹手
// =====================================
public class Bomber : Character
{
    public override void Initialize()
    {
        base.Initialize();
        characterName = "投弹手";
        health = 2; // 策划案：生命值2
        shield = 0;
        faction = "地球联合国";
        abilityName = "艺术就是爆炸";
        abilityDescription = "摧毁3x3范围内所有角色与障碍（包括自己）。";
        background = "被植入芯片的边缘人，用爆炸结束生命。";
        moveDistance = 1;
        attackDistance = 0;
        attackDamage = 0;
        canAttack = false; // 投弹手无法攻击
    }

    public override void UseAbility()
    {
        if (usedAbilityThisGame)
        {
            Debug.Log("投弹手：主动技能本局游戏只能使用一次！");
            return;
        }

        Debug.Log("艺术就是爆炸——BOOM！");
        usedAbilityThisGame = true;

        // 摧毁3x3范围内所有角色和障碍
        Explode3x3(gridX, gridY);
    }

    private void Explode3x3(int centerX, int centerY)
    {
        // 获取所有角色
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(BattleManager.Instance.player1Team);
        allCharacters.AddRange(BattleManager.Instance.player2Team);

        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int y = centerY - 1; y <= centerY + 1; y++)
            {
                // 规则：摧毁该格子周围的所有障碍（障碍位于格子之间的边上）
                if (x >= 0 && x < GridManager.Instance.width && y >= 0 && y < GridManager.Instance.height)
                {
                    GridManager.Instance.RemoveObstaclesAround(x, y);
                }

                // 伤害范围内所有角色
                foreach (var character in allCharacters)
                {
                    if (!character.isAlive) continue;
                    if (character.gridX == x && character.gridY == y)
                    {
                        character.TakeDamage(999); // 足够大的伤害
                    }
                }
            }
        }

        // 最后自己死亡
        Die();
    }
}

// =====================================
// 圣光卫士
// =====================================
public class HolyGuardian : Character
{
    private bool usedHolyShield = false;

    public override void Initialize()
    {
        base.Initialize();
        characterName = "圣光卫士";
        health = 3; // 策划案：生命值3
        shield = 1;
        faction = "地球联合国"; // 策划案修正
        abilityName = "破敌 / 光之圣卫";
        abilityDescription = "破敌：发动一次攻击，若敌方有护盾，此伤害+1。光之圣卫：受到致命伤害时，免疫此伤害。";
        background = "禁军档案，机密资料。";
        moveDistance = 1;
        attackDistance = 1;
        attackDamage = 1;
        canAttack = true;
        usedHolyShield = false;
    }

    public override void UseAbility()
    {
        if (usedAbilityThisGame)
        {
            Debug.Log("圣光卫士：主动技能本局游戏只能使用一次！");
            return;
        }

        // 破敌：选择目标进行攻击
        Debug.Log("破敌：选择目标进行攻击（若目标有护盾，伤害+1）");
        // TODO: 实现UI选择目标和攻击逻辑
        usedAbilityThisGame = true;
    }

    public void ExecuteBreakEnemy(Character target)
    {
        int damage = attackDamage;
        if (target.shield > 0)
        {
            damage += 1; // 对有护盾的目标额外+1伤害
        }
        target.TakeDamage(damage);
        Debug.Log($"破敌：对 {target.characterName} 造成 {damage} 点伤害");
    }

    protected override void Die()
    {
        if (!usedHolyShield)
        {
            usedHolyShield = true;
            health = 1;
            isAlive = true;
            Debug.Log("光之圣卫：发动，被致命伤免疫一次！");
        }
        else
        {
            base.Die();
        }
    }
}

// =====================================
// 荆棘战士
// =====================================
public class ThornWarrior : Character
{
    public override void Initialize()
    {
        base.Initialize();
        characterName = "荆棘战士";
        health = 4;
        shield = 0; // 策划案：护盾0
        faction = "寰宇联合国";
        abilityName = "能量压缩装甲";
        abilityDescription = "生命值小于50%时，造成的伤害+2。";
        background = "能量压缩装甲吸收能量并释放，增强战斗力。";
        moveDistance = 1;
        attackDistance = 1;
        attackDamage = 1;
        canAttack = true;
    }

    public override void UseAbility()
    {
        // 能量压缩装甲是被动技能，不需要主动使用
        Debug.Log("能量压缩装甲是被动技能，生命值小于50%时自动生效（伤害+2）");
    }
}

// =====================================
// 至忠圣卫
// =====================================
public class LoyalGuardian : Character
{
    public override void Initialize()
    {
        base.Initialize();
        characterName = "至忠圣卫";
        health = 3; // 策划案：生命值3
        shield = 1;
        faction = "寰宇联合国";
        abilityName = "忠诚守护";
        abilityDescription = "在己方半场时，行动距离+1，且可在范围内任意点复活。";
        background = "如神话中的军团，对帝国永远忠诚。";
        moveDistance = 1;
        attackDistance = 1;
        attackDamage = 1;
        canAttack = true;
    }

    public override int GetEffectiveMoveDistance()
    {
        int distance = base.GetEffectiveMoveDistance();
        // 规则：在己方半场时，行动距离+1（包括移动距离）
        if (GridManager.Instance.IsInOwnHalfField(gridX, gridY, faction))
        {
            distance += 1;
        }
        return distance;
    }

    public override int GetEffectiveAttackDistance()
    {
        int distance = base.GetEffectiveAttackDistance();
        // 规则：在己方半场时，行动距离+1（包括攻击距离）
        if (GridManager.Instance.IsInOwnHalfField(gridX, gridY, faction))
        {
            distance += 1;
        }
        return distance;
    }

    public override void Revive(int x, int y)
    {
        // 至忠圣卫可以在己方半场任意点复活
        if (GridManager.Instance.IsInOwnHalfField(x, y, faction))
        {
            base.Revive(x, y);
        }
        else
        {
            // 否则在出生点复活
            base.Revive(GridManager.Instance.GetSpawnPoint(faction).x, 
                       GridManager.Instance.GetSpawnPoint(faction).y);
        }
    }

    public override void UseAbility()
    {
        // 忠诚守护是被动技能，不需要主动使用
        Debug.Log("忠诚守护是被动技能，在己方半场时自动生效（行动距离+1，可在范围内任意点复活）");
    }
}

