using System.Collections;
using UnityEngine;

/// <summary>
/// 角色操作类型枚举
/// </summary>
public enum ActionType
{
    None,           // 无操作
    Move,           // 移动
    Attack,         // 攻击
    UseSkill,       // 使用主动技能
    RestoreShield   // 恢复护盾（不占用主要操作）
}

/// <summary>
/// 角色操作类 - 打包移动、攻击和使用技能
/// 规则：每回合只能选择移动、攻击或使用技能中的一个（被动技能角色除外）
/// </summary>
public class CharacterAction
{
    public ActionType actionType;
    public Character character;
    
    // 移动相关参数
    public int targetX;
    public int targetY;
    public bool useTeleport = false; // 是否使用传送（当角色在传送点上时）
    
    // 攻击相关参数
    public Character attackTarget;
    
    // 技能相关参数（可能需要目标等）
    public object skillData;

    public CharacterAction(ActionType type, Character chara)
    {
        actionType = type;
        character = chara;
    }

    /// <summary>
    /// 执行操作
    /// </summary>
    public IEnumerator Execute()
    {
        switch (actionType)
        {
            case ActionType.Move:
                yield return ExecuteMove();
                break;
                
            case ActionType.Attack:
                yield return ExecuteAttack();
                break;
                
            case ActionType.UseSkill:
                yield return ExecuteSkill();
                break;
                
            case ActionType.RestoreShield:
                yield return ExecuteRestoreShield();
                break;
        }
    }

    private IEnumerator ExecuteMove()
    {
        Debug.Log($"{character.characterName} 执行移动操作");
        
        // 规则：如果在传送点上且选择使用传送，则传送到对应传送点
        if (useTeleport)
        {
            if (GridManager.Instance.TeleportCharacter(character, targetX, targetY))
            {
                Debug.Log($"{character.characterName} 成功传送到 ({targetX}, {targetY})");
            }
            else
            {
                Debug.Log($"{character.characterName} 传送失败");
            }
        }
        else
        {
            // 正常移动到相邻格子
            if (GridManager.Instance.MoveCharacter(character, targetX, targetY))
            {
                Debug.Log($"{character.characterName} 成功移动到 ({targetX}, {targetY})");
            }
            else
            {
                Debug.Log($"{character.characterName} 移动失败：目标位置不可达");
            }
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteAttack()
    {
        Debug.Log($"{character.characterName} 执行攻击操作");
        
        if (attackTarget != null && attackTarget.isAlive)
        {
            character.Attack(attackTarget);
        }
        else
        {
            Debug.Log($"{character.characterName} 攻击失败：目标无效");
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteSkill()
    {
        Debug.Log($"{character.characterName} 执行使用技能操作");
        
        // 检查是否有主动技能
        if (HasActiveSkill(character))
        {
            character.UseAbility();
        }
        else
        {
            Debug.Log($"{character.characterName} 没有主动技能（只有被动技能）");
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteRestoreShield()
    {
        Debug.Log($"{character.characterName} 恢复护盾");
        character.RestoreShield(1);
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 检查角色是否有主动技能（被动技能的角色除外）
    /// </summary>
    public static bool HasActiveSkill(Character chara)
    {
        // 检查是否是只有被动技能的角色
        // 这些角色的UseAbility只是日志输出，不算主动技能
        if (chara is HalfDemonRanger || 
            chara is Archer || 
            chara is ThornWarrior || 
            chara is LoyalGuardian)
        {
            return false; // 只有被动技能
        }
        
        return true; // 有主动技能
    }

    /// <summary>
    /// 检查操作是否为主操作（移动、攻击、使用技能）
    /// </summary>
    public static bool IsMainAction(ActionType actionType)
    {
        return actionType == ActionType.Move || 
               actionType == ActionType.Attack || 
               actionType == ActionType.UseSkill;
    }
}

