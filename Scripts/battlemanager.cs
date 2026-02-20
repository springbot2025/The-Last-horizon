using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public List<Character> player1Team = new List<Character>();
    public List<Character> player2Team = new List<Character>();

    private List<Character> turnOrder = new List<Character>();
    private int currentTurnIndex = 0;
    private bool battleActive = false;

    private Character currentCharacter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 🟢 初始化战斗（由 GameManager 调用）
    public void StartBattle(List<Character> player1Chars, List<Character> player2Chars)
    {
        player1Team = player1Chars;
        player2Team = player2Chars;

        bool player1First = Random.value > 0.5f;
        Debug.Log($"先手方：{(player1First ? "玩家1" : "玩家2")}");

        BuildTurnOrder(player1First);

        foreach (var c in player1Team) c.Initialize();
        foreach (var c in player2Team) c.Initialize();

        battleActive = true;
        currentTurnIndex = 0;

        StartCoroutine(TurnLoop());
    }

    // 🧮 构建行动顺序（符合策划案：先手1号→后手1号→后手2号→先手2号）
    private void BuildTurnOrder(bool player1First)
    {
        turnOrder.Clear();
        if (player1First)
        {
            // 玩家1先手：先手1号角色 → 后手1号角色 → 后手2号角色 → 先手2号角色
            turnOrder.Add(player1Team[0]);  // 先手1号（玩家1的1号角色）
            turnOrder.Add(player2Team[0]);  // 后手1号（玩家2的1号角色）
            turnOrder.Add(player2Team[1]);  // 后手2号（玩家2的2号角色）
            turnOrder.Add(player1Team[1]);  // 先手2号（玩家1的2号角色）
        }
        else
        {
            // 玩家2先手
            turnOrder.Add(player2Team[0]);  // 先手1号（玩家2的1号角色）
            turnOrder.Add(player1Team[0]);  // 后手1号（玩家1的1号角色）
            turnOrder.Add(player1Team[1]);  // 后手2号（玩家1的2号角色）
            turnOrder.Add(player2Team[1]);  // 先手2号（玩家2的2号角色）
        }
    }

    // 🔁 主循环
    private IEnumerator TurnLoop()
    {
        while (battleActive)
        {
            if (CheckVictoryCondition()) yield break;

            currentCharacter = turnOrder[currentTurnIndex];

            // 处理死亡角色的复活
            if (currentCharacter != null && !currentCharacter.isAlive && currentCharacter.reviveCount > 0)
            {
                ProcessRevive(currentCharacter);
                NextTurn();
                continue;
            }

            // 检查角色是否可以行动
            if (currentCharacter == null || !currentCharacter.isAlive || !currentCharacter.canActThisTurn)
            {
                if (currentCharacter != null && !currentCharacter.isAlive)
                {
                    Debug.Log($"{currentCharacter.characterName} 已死亡且无复活机会，移出对局");
                }
                else if (currentCharacter != null && !currentCharacter.canActThisTurn)
                {
                    Debug.Log($"{currentCharacter.characterName} 本回合无法行动（复活后或 engineer机械臂限制）");
                    currentCharacter.canActThisTurn = true; // 重置，以便下一回合可以行动
                }
                NextTurn();
                continue;
            }

            Debug.Log($"轮到 {currentCharacter.characterName} 行动");
            
            // 回合开始处理
            currentCharacter.OnTurnStart();

            // 所有角色都由玩家控制（双人对战）
            yield return StartCoroutine(PlayerTurn(currentCharacter));

            // 回合结束处理
            currentCharacter.OnTurnEnd();

            NextTurn();
        }
    }

    // 👤 玩家行动（双人对战，所有角色都由玩家控制）
    // 规则：每回合只能选择移动、攻击或使用技能中的一个（被动技能的角色除外）
    private IEnumerator PlayerTurn(Character character)
    {
        Debug.Log($"等待玩家操作：{character.characterName}");
        
        // 显示操作菜单
        UIManager.Instance.ShowActionMenu(character);

        // 等待玩家选择主操作（移动、攻击或使用技能）
        while (!UIManager.Instance.HasMadeChoice)
        {
            yield return null;
        }

        string choice = UIManager.Instance.GetPlayerChoice();
        UIManager.Instance.ResetChoice();

        // 解析玩家选择，创建对应的操作
        ActionType selectedAction = ParseActionType(choice);
        
        // 规则：每回合只能选择移动、攻击或使用技能中的一个
        // 恢复护盾和传送是额外操作，不占用主操作槽位
        if (CharacterAction.IsMainAction(selectedAction))
        {
            // 创建并执行主操作
            CharacterAction mainAction = CreateMainAction(character, selectedAction);
            
            if (mainAction != null)
            {
                // 等待玩家提供操作所需的数据（目标位置、攻击目标等）
                yield return StartCoroutine(WaitForActionData(mainAction));
                
                // 执行主操作
                yield return StartCoroutine(mainAction.Execute());
            }
            else
            {
                Debug.Log($"{character.characterName} 无法执行该操作");
            }
        }
        else if (selectedAction == ActionType.RestoreShield)
        {
            // 恢复护盾不占用主操作，可以直接执行
            CharacterAction shieldAction = new CharacterAction(ActionType.RestoreShield, character);
            yield return StartCoroutine(shieldAction.Execute());
        }

        Debug.Log($"{character.characterName} 行动结束。");
    }

    /// <summary>
    /// 解析字符串为操作类型
    /// </summary>
    private ActionType ParseActionType(string choice)
    {
        switch (choice)
        {
            case "Attack": return ActionType.Attack;
            case "Move": return ActionType.Move;
            case "Skill": return ActionType.UseSkill;
            case "Shield": return ActionType.RestoreShield;
            default: return ActionType.None;
        }
    }

    /// <summary>
    /// 创建主操作对象
    /// </summary>
    private CharacterAction CreateMainAction(Character character, ActionType actionType)
    {
        CharacterAction action = new CharacterAction(actionType, character);
        
        // 检查被动技能角色
        if (actionType == ActionType.UseSkill)
        {
            // 如果是被动技能角色，不允许使用技能
            if (!HasActiveSkill(character))
            {
                Debug.Log($"{character.characterName} 只有被动技能，无法主动使用技能");
                return null;
            }
        }
        
        return action;
    }

    /// <summary>
    /// 等待玩家提供操作所需的数据（目标位置、攻击目标等）
    /// </summary>
    private IEnumerator WaitForActionData(CharacterAction action)
    {
        if (action.actionType == ActionType.Move)
        {
            // 规则：如果在传送点上，可以选择传送到对应传送点或移动到相邻格子
            bool isOnTeleporter = GridManager.Instance.IsCharacterOnTeleporter(action.character);
            GridTile linkedTeleporter = null;
            
            if (isOnTeleporter)
            {
                linkedTeleporter = GridManager.Instance.GetLinkedTeleporter(action.character);
                Debug.Log($"{action.character.characterName} 在传送点上，可以选择：");
                Debug.Log($"1. 传送到对应传送点 ({linkedTeleporter.x}, {linkedTeleporter.y})");
                Debug.Log($"2. 移动到相邻格子");
                
                // 等待玩家选择传送或正常移动
                // TODO: 实现UI选择（传送/移动）
                // 这里简化处理：如果点击对应传送点位置，则传送；否则正常移动
                bool choiceMade = false;
                
                while (!choiceMade)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        int clickX = Mathf.FloorToInt(mousePos.x / GridManager.Instance.cellSize);
                        int clickY = Mathf.FloorToInt(mousePos.y / GridManager.Instance.cellSize);
                        
                        // 如果点击的是对应传送点，则选择传送
                        if (clickX == linkedTeleporter.x && clickY == linkedTeleporter.y)
                        {
                            action.useTeleport = true;
                            action.targetX = linkedTeleporter.x;
                            action.targetY = linkedTeleporter.y;
                            Debug.Log($"玩家选择传送到 ({action.targetX}, {action.targetY})");
                            choiceMade = true;
                        }
                        else
                        {
                            // 否则正常移动到相邻格子
                            // 检查是否是相邻格子
                            int dx = Mathf.Abs(clickX - action.character.gridX);
                            int dy = Mathf.Abs(clickY - action.character.gridY);
                            
                            if (dx + dy == 1)
                            {
                                action.useTeleport = false;
                                action.targetX = clickX;
                                action.targetY = clickY;
                                Debug.Log($"玩家选择移动到相邻格子 ({action.targetX}, {action.targetY})");
                                choiceMade = true;
                            }
                            else
                            {
                                Debug.Log("请选择相邻格子或对应传送点");
                            }
                        }
                    }
                    yield return null;
                }
            }
            else
            {
                // 不在传送点上，正常移动
                Debug.Log("请选择移动目标位置");
                action.useTeleport = false;
                
                // 等待UI选择目标位置
                // TODO: 实现UI选择目标位置
                // 这里简化处理，使用鼠标位置
                while (!Input.GetMouseButtonDown(0))
                {
                    yield return null;
                }
                
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                action.targetX = Mathf.FloorToInt(mousePos.x / GridManager.Instance.cellSize);
                action.targetY = Mathf.FloorToInt(mousePos.y / GridManager.Instance.cellSize);
            }
        }
        else if (action.actionType == ActionType.Attack)
        {
            Debug.Log("请选择攻击目标");
            // 等待UI选择攻击目标
            // TODO: 实现UI选择攻击目标
            // 这里简化处理
            yield return new WaitForSeconds(0.5f);
            
            // 临时：选择最近的敌方角色
            List<Character> allEnemies = GetEnemyCharacters(action.character);
            if (allEnemies.Count > 0)
            {
                action.attackTarget = allEnemies[0];
            }
        }
        else if (action.actionType == ActionType.UseSkill)
        {
            Debug.Log("使用技能");
            // 技能可能需要目标，但这里简化处理
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 获取敌方角色列表
    /// </summary>
    private List<Character> GetEnemyCharacters(Character character)
    {
        List<Character> enemies = new List<Character>();
        
        // 判断角色属于哪个队伍
        bool isPlayer1 = player1Team.Contains(character);
        
        if (isPlayer1)
        {
            enemies.AddRange(player2Team);
        }
        else
        {
            enemies.AddRange(player1Team);
        }
        
        // 只返回存活的角色
        enemies.RemoveAll(c => !c.isAlive);
        return enemies;
    }

    /// <summary>
    /// 检查角色是否有主动技能（使用CharacterAction的静态方法）
    /// </summary>
    private bool HasActiveSkill(Character character)
    {
        return CharacterAction.HasActiveSkill(character);
    }

    // ⏭️ 下一个角色
    private void NextTurn()
    {
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
    }

    // 🏁 胜负判定（策划案：己方角色进入对手基地 或 对手在场上不存在角色）
    private bool CheckVictoryCondition()
    {
        // 检查玩家1是否进入玩家2基地
        foreach (var character in player1Team)
        {
            if (character.isAlive && GridManager.Instance.IsCharacterInEnemyBase(character, GetPlayer2Faction()))
            {
                Debug.Log("玩家1胜利！（角色进入玩家2基地）");
                battleActive = false;
                GameManager.Instance.EndGame("玩家1");
                return true;
            }
        }

        // 检查玩家2是否进入玩家1基地
        foreach (var character in player2Team)
        {
            if (character.isAlive && GridManager.Instance.IsCharacterInEnemyBase(character, GetPlayer1Faction()))
            {
                Debug.Log("玩家2胜利！（角色进入玩家1基地）");
                battleActive = false;
                GameManager.Instance.EndGame("玩家2");
                return true;
            }
        }

        // 检查全灭
        bool player1AllDead = player1Team.TrueForAll(c => !c.isAlive);
        bool player2AllDead = player2Team.TrueForAll(c => !c.isAlive);

        if (player1AllDead)
        {
            Debug.Log("玩家2胜利！（玩家1全灭）");
            battleActive = false;
            GameManager.Instance.EndGame("玩家2");
            return true;
        }

        if (player2AllDead)
        {
            Debug.Log("玩家1胜利！（玩家2全灭）");
            battleActive = false;
            GameManager.Instance.EndGame("玩家1");
            return true;
        }

        return false;
    }

    // 获取玩家1阵营
    private string GetPlayer1Faction()
    {
        if (player1Team.Count > 0)
            return player1Team[0].faction;
        return "";
    }

    // 获取玩家2阵营
    private string GetPlayer2Faction()
    {
        if (player2Team.Count > 0)
            return player2Team[0].faction;
        return "";
    }

    // 攻击目标（由UI调用）
    public void ExecuteAttack(Character attacker, Character target)
    {
        if (attacker.canAttack)
        {
            attacker.Attack(target);
        }
    }

    // 处理复活（死亡后）
    public void ProcessRevive(Character character)
    {
        if (character.reviveCount > 0)
        {
            // 规则：在固定复活点复活
            Vector2Int spawnPoint = GetSpawnPointForCharacter(character);
            character.Revive(spawnPoint.x, spawnPoint.y);
            Debug.Log($"{character.characterName} 在复活点 ({spawnPoint.x}, {spawnPoint.y}) 复活");
        }
        else
        {
            Debug.Log($"{character.characterName} 没有复活机会，移出对局");
            // 规则：无复活机会则移出对局（已经在TurnLoop中跳过）
        }
    }

    // 获取角色的复活点（规则：每个角色有固定复活点）
    private Vector2Int GetSpawnPointForCharacter(Character character)
    {
        bool isPlayer1 = player1Team.Contains(character);
        int charIndex = isPlayer1 ? player1Team.IndexOf(character) : player2Team.IndexOf(character);
        
        if (isPlayer1)
        {
            return charIndex == 0 ? GridManager.Instance.player1Spawn1 : GridManager.Instance.player1Spawn2;
        }
        else
        {
            return charIndex == 0 ? GridManager.Instance.player2Spawn1 : GridManager.Instance.player2Spawn2;
        }
    }
}
