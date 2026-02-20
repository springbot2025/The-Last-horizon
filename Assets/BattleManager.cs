using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public List<Character> playerTeam = new List<Character>();
    public List<Character> enemyTeam = new List<Character>();

    private List<Character> turnOrder = new List<Character>();
    private int currentTurnIndex = 0;
    private bool battleActive = false;

    private Character currentCharacter;
    private string firstFaction; // "Player" or "Enemy"
    
    // 技能弹窗UI
    private GameObject skillWindow;
    private Button skillButton;
    private Button skillCancelButton;
    private TextMeshProUGUI skillDescriptionText;
    private bool skillUsed = false; // 技能是否已使用的标志

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        CreateSkillWindow();
    }

    /// <summary>
    /// 创建技能弹窗UI
    /// </summary>
    void CreateSkillWindow()
    {
        // 查找或创建Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("BattleCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 创建技能窗口
        GameObject windowObj = new GameObject("SkillWindow");
        windowObj.transform.SetParent(canvas.transform, false);
        skillWindow = windowObj;

        RectTransform windowRect = windowObj.AddComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.7f);
        windowRect.anchorMax = new Vector2(0.5f, 0.7f);
        windowRect.sizeDelta = new Vector2(300, 200);
        windowRect.anchoredPosition = Vector2.zero;

        // 背景
        Image bg = windowObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        // 技能描述文本
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(windowObj.transform, false);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.1f, 0.4f);
        descRect.anchorMax = new Vector2(0.9f, 0.9f);
        descRect.sizeDelta = Vector2.zero;
        skillDescriptionText = descObj.AddComponent<TextMeshProUGUI>();
        skillDescriptionText.text = "";
        skillDescriptionText.fontSize = 16;
        skillDescriptionText.color = Color.white;
        skillDescriptionText.alignment = TextAlignmentOptions.Center;

        // 技能按钮
        GameObject skillBtnObj = new GameObject("SkillButton");
        skillBtnObj.transform.SetParent(windowObj.transform, false);
        RectTransform skillBtnRect = skillBtnObj.AddComponent<RectTransform>();
        skillBtnRect.anchorMin = new Vector2(0.1f, 0.1f);
        skillBtnRect.anchorMax = new Vector2(0.45f, 0.3f);
        skillBtnRect.sizeDelta = Vector2.zero;
        Image skillBtnBg = skillBtnObj.AddComponent<Image>();
        skillBtnBg.color = new Color(0.3f, 0.7f, 0.3f, 1f);
        skillButton = skillBtnObj.AddComponent<Button>();

        GameObject skillBtnTextObj = new GameObject("Text");
        skillBtnTextObj.transform.SetParent(skillBtnObj.transform, false);
        RectTransform skillBtnTextRect = skillBtnTextObj.AddComponent<RectTransform>();
        skillBtnTextRect.anchorMin = Vector2.zero;
        skillBtnTextRect.anchorMax = Vector2.one;
        skillBtnTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI skillBtnText = skillBtnTextObj.AddComponent<TextMeshProUGUI>();
        skillBtnText.text = "使用技能";
        skillBtnText.fontSize = 18;
        skillBtnText.color = Color.white;
        skillBtnText.alignment = TextAlignmentOptions.Center;

        // 取消按钮
        GameObject cancelBtnObj = new GameObject("CancelButton");
        cancelBtnObj.transform.SetParent(windowObj.transform, false);
        RectTransform cancelBtnRect = cancelBtnObj.AddComponent<RectTransform>();
        cancelBtnRect.anchorMin = new Vector2(0.55f, 0.1f);
        cancelBtnRect.anchorMax = new Vector2(0.9f, 0.3f);
        cancelBtnRect.sizeDelta = Vector2.zero;
        Image cancelBtnBg = cancelBtnObj.AddComponent<Image>();
        cancelBtnBg.color = new Color(0.7f, 0.3f, 0.3f, 1f);
        skillCancelButton = cancelBtnObj.AddComponent<Button>();

        GameObject cancelBtnTextObj = new GameObject("Text");
        cancelBtnTextObj.transform.SetParent(cancelBtnObj.transform, false);
        RectTransform cancelBtnTextRect = cancelBtnTextObj.AddComponent<RectTransform>();
        cancelBtnTextRect.anchorMin = Vector2.zero;
        cancelBtnTextRect.anchorMax = Vector2.one;
        cancelBtnTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI cancelBtnText = cancelBtnTextObj.AddComponent<TextMeshProUGUI>();
        cancelBtnText.text = "取消";
        cancelBtnText.fontSize = 18;
        cancelBtnText.color = Color.white;
        cancelBtnText.alignment = TextAlignmentOptions.Center;

        // 初始隐藏
        skillWindow.SetActive(false);
    }

    /// <summary>
    /// 显示技能弹窗
    /// </summary>
    void ShowSkillWindow(Character character)
    {
        if (skillWindow == null || character == null || character.usedAbilityThisGame)
            return;

        skillDescriptionText.text = $"{character.abilityName}\n{character.abilityDescription}";
        skillWindow.SetActive(true);

        // 清除之前的监听器
        skillButton.onClick.RemoveAllListeners();
        skillCancelButton.onClick.RemoveAllListeners();

        // 使用技能
        skillButton.onClick.AddListener(() =>
        {
            Debug.Log($"{character.characterName} uses skill: {character.abilityName}");
            
            if (character.visualManager != null)
            {
                character.visualManager.PlaySkill(() =>
                {
                    character.UseAbility();
                    character.usedAbilityThisGame = true;
                });
            }
            else
            {
                character.UseAbility();
                character.usedAbilityThisGame = true;
            }
            
            HideSkillWindow();
        });

        // 取消
        skillCancelButton.onClick.AddListener(() =>
        {
            HideSkillWindow();
        });
    }

    /// <summary>
    /// 隐藏技能弹窗
    /// </summary>
    void HideSkillWindow()
    {
        if (skillWindow != null)
        {
            skillWindow.SetActive(false);
        }
    }

    // 初始化战斗（由 GameManager 调用）
    public void StartBattle(List<Character> playerChars, List<Character> enemyChars)
    {
        Debug.Log("[BattleManager] StartBattle called");
        
        if (playerChars == null || playerChars.Count < 2)
        {
            Debug.LogError("[BattleManager] Player team insufficient. Need 2 characters.");
            return;
        }
        if (enemyChars == null || enemyChars.Count < 2)
        {
            Debug.LogError("[BattleManager] Enemy team insufficient. Need 2 characters.");
            return;
        }
        
        playerTeam = playerChars;
        enemyTeam = enemyChars;

        Debug.Log($"[BattleManager] Player team: {playerTeam[0].characterName}, {playerTeam[1].characterName}");
        Debug.Log($"[BattleManager] Enemy team: {enemyTeam[0].characterName}, {enemyTeam[1].characterName}");

        bool playerFirst = Random.value > 0.5f;
        firstFaction = playerFirst ? "Player" : "Enemy";
        Debug.Log($"[BattleManager] First turn: {firstFaction}");

        BuildTurnOrder(playerFirst);
        
        Debug.Log("[BattleManager] Turn order built:");
        for (int i = 0; i < turnOrder.Count; i++)
        {
            if (turnOrder[i] != null)
            {
                Debug.Log($"  Turn {i + 1}: {turnOrder[i].characterName}");
            }
        }

        foreach (var c in playerTeam)
        {
            if (c != null)
            {
                c.Initialize();
            }
        }
        foreach (var c in enemyTeam)
        {
            if (c != null)
            {
                c.Initialize();
            }
        }

        battleActive = true;
        currentTurnIndex = 0;

        Debug.Log("[BattleManager] Starting turn loop...");
        StartCoroutine(TurnLoop());
    }

    // 构建行动顺序（符合策划案：先手1号→后手1号→后手2号→先手2号）
    private void BuildTurnOrder(bool playerFirst)
    {
        turnOrder.Clear();
        if (playerFirst)
        {
            // 先手1号角色 → 后手1号角色 → 后手2号角色 → 先手2号角色
            turnOrder.Add(playerTeam[0]);  // 先手1号
            turnOrder.Add(enemyTeam[0]);   // 后手1号
            turnOrder.Add(enemyTeam[1]);   // 后手2号
            turnOrder.Add(playerTeam[1]);  // 先手2号
        }
        else
        {
            // 敌方先手
            turnOrder.Add(enemyTeam[0]);   // 先手1号
            turnOrder.Add(playerTeam[0]);  // 后手1号
            turnOrder.Add(playerTeam[1]);  // 后手2号
            turnOrder.Add(enemyTeam[1]);   // 先手2号
        }
    }

    // 主循环
    private IEnumerator TurnLoop()
    {
        while (battleActive)
        {
            if (CheckVictoryCondition()) yield break;

            currentCharacter = turnOrder[currentTurnIndex];

            if (currentCharacter == null || !currentCharacter.isAlive || !currentCharacter.canActThisTurn)
            {
                if (currentCharacter != null && !currentCharacter.canActThisTurn)
                {
                    Debug.Log($"{currentCharacter.characterName} cannot act this turn (Engineer arm restriction)");
                    currentCharacter.canActThisTurn = true; // 重置
                }
                NextTurn();
                continue;
            }

            Debug.Log($"Turn: {currentCharacter.characterName}");
            
            // 回合开始处理
            currentCharacter.OnTurnStart();

            // 双人对战：所有角色都由玩家控制，轮流操作
            // 所有角色都走PlayerTurn流程
            yield return StartCoroutine(PlayerTurn(currentCharacter));

            // 回合结束处理
            currentCharacter.OnTurnEnd();

            NextTurn();
        }
    }

    // 判断角色是否由玩家控制
    private bool IsPlayerControlled(Character character)
    {
        return playerTeam.Contains(character);
    }

    // 玩家行动（鼠标点击控制：点击地图移动，点击角色攻击）
    private IEnumerator PlayerTurn(Character character)
    {
        Debug.Log($"Turn: {character.characterName} - Click map to move, click character to attack");
        
        bool actionPerformed = false;
        bool mainActionUsed = false; // 主操作（移动/攻击/技能）是否已使用
        skillUsed = false; // 重置技能使用标志

        // 显示技能弹窗（可选，不占用主操作）
        if (!character.usedAbilityThisGame)
        {
            ShowSkillWindow(character);
        }

        // 等待玩家输入
        while (!actionPerformed)
        {
            // 检查技能是否已被使用（通过弹窗按钮）
            if (skillUsed && !mainActionUsed)
            {
                mainActionUsed = true;
                actionPerformed = true;
                HideSkillWindow();
                yield return new WaitForSeconds(0.5f);
                break;
            }

            // 检测鼠标点击
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;

                // 转换为网格坐标
                int clickX = Mathf.FloorToInt(mousePos.x / GridManager.Instance.cellSize);
                int clickY = Mathf.FloorToInt(mousePos.y / GridManager.Instance.cellSize);

                // 检查点击的是角色还是地图
                Character clickedCharacter = GetCharacterAt(clickX, clickY);
                
                if (clickedCharacter != null && clickedCharacter != character)
                {
                    // 点击了其他角色 = 攻击
                    if (!mainActionUsed && character.canAttack)
                    {
                        int attackRange = character.GetEffectiveAttackDistance();
                        int distance = Mathf.Abs(clickedCharacter.gridX - character.gridX) + 
                                      Mathf.Abs(clickedCharacter.gridY - character.gridY);
                        
                        if (distance <= attackRange)
                        {
                            // 检查是否是敌人
                            bool isEnemy = (playerTeam.Contains(character) && enemyTeam.Contains(clickedCharacter)) ||
                                         (enemyTeam.Contains(character) && playerTeam.Contains(clickedCharacter));
                            
                            if (isEnemy)
                            {
                                Debug.Log($"{character.characterName} attacks {clickedCharacter.characterName}");
                                character.Attack(clickedCharacter);
                                mainActionUsed = true;
                                actionPerformed = true;
                                HideSkillWindow(); // 隐藏技能窗口
                                yield return new WaitForSeconds(0.5f);
                            }
                            else
                            {
                                Debug.Log("Cannot attack friendly character!");
                            }
                        }
                        else
                        {
                            Debug.Log("Target out of attack range!");
                        }
                    }
                    else if (mainActionUsed)
                    {
                        Debug.Log("Main action already used, cannot attack!");
                    }
                    else if (!character.canAttack)
                    {
                        Debug.Log($"{character.characterName} cannot attack!");
                    }
                }
                else
                {
                    // 点击了地图 = 移动
                    if (!mainActionUsed)
                    {
                        int distance = Mathf.Abs(clickX - character.gridX) + Mathf.Abs(clickY - character.gridY);
                        
                        // 检查是否是相邻格子或传送点
                        GridTile targetTile = GridManager.Instance.GetTileAt(clickX, clickY);
                        
                        if (targetTile != null && targetTile.tileType == TileType.Teleporter)
                        {
                            // 传送点
                            if (GridManager.Instance.UseTeleporter(character))
                            {
                                Debug.Log($"{character.characterName} uses teleporter");
                                actionPerformed = true;
                                HideSkillWindow();
                                yield return new WaitForSeconds(0.3f);
                            }
                        }
                        else if (distance == 1 && targetTile != null && targetTile.isWalkable)
                        {
                            // 移动到相邻格子
                            if (GridManager.Instance.MoveCharacter(character, clickX, clickY))
                            {
                                Debug.Log($"{character.characterName} moved to ({clickX}, {clickY})");
                                mainActionUsed = true;
                                actionPerformed = true;
                                HideSkillWindow();
                                yield return new WaitForSeconds(0.3f);
                            }
                            else
                            {
                                Debug.Log("Cannot move to that position!");
                            }
                        }
                        else if (distance > 1)
                        {
                            Debug.Log("Can only move to adjacent tiles!");
                        }
                        else
                        {
                            Debug.Log("Target position is not walkable!");
                        }
                    }
                    else
                    {
                        Debug.Log("Main action already used, cannot move!");
                    }
                }
            }

            yield return null;
        }

        Debug.Log($"{character.characterName} turn ended.");
    }

    /// <summary>
    /// 获取指定网格位置的角色
    /// </summary>
    private Character GetCharacterAt(int x, int y)
    {
        foreach (var c in playerTeam)
        {
            if (c != null && c.isAlive && c.gridX == x && c.gridY == y)
                return c;
        }
        foreach (var c in enemyTeam)
        {
            if (c != null && c.isAlive && c.gridX == x && c.gridY == y)
                return c;
        }
        return null;
    }

    /// <summary>
    /// 查找角色攻击范围内的最近敌人
    /// </summary>
    private Character FindNearestEnemyInRange(Character attacker)
    {
        int attackRange = attacker.GetEffectiveAttackDistance();
        Character nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        // 确定敌方队伍（如果攻击者在playerTeam，敌人就是enemyTeam，反之亦然）
        List<Character> targetEnemyTeam = playerTeam.Contains(attacker) ? enemyTeam : playerTeam;

        foreach (Character enemy in targetEnemyTeam)
        {
            if (enemy == null || !enemy.isAlive) continue;

            int distance = Mathf.Abs(enemy.gridX - attacker.gridX) + Mathf.Abs(enemy.gridY - attacker.gridY);
            if (distance <= attackRange)
            {
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }

        return nearestEnemy;
    }

    // 敌人自动行动
    private IEnumerator EnemyTurn(Character character)
    {
        Debug.Log($"{character.characterName} (AI) thinking...");

        // 选择目标角色（攻击第一个存活的玩家）
        Character target = playerTeam.Find(c => c.isAlive);
        if (target != null)
        {
            Debug.Log($"{character.characterName} attacks {target.characterName}!");
            target.TakeDamage(10); // 敌人对玩家造成10点伤害
        }

        // 如果敌人需要移动，则可以在这里添加敌人移动的逻辑
        if (character != null)
        {
            Vector3 targetPos = GridManager.Instance.GetWorldPosition(3, 3); // 假设敌人移动到 (3,3)
            int targetX = Mathf.FloorToInt(targetPos.x / GridManager.Instance.cellSize);
            int targetY = Mathf.FloorToInt(targetPos.y / GridManager.Instance.cellSize);

            // 使用 GridManager 处理移动
            if (GridManager.Instance.MoveCharacter(character, targetX, targetY))
            {
                Debug.Log($"{character.characterName} moved to {targetX}, {targetY}");
            }
        }

        yield return new WaitForSeconds(1f);
    }

    // 下一个角色
    private void NextTurn()
    {
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
    }

    // 胜负判定（策划案：己方角色进入对手基地 或 对手在场上不存在角色）
    private bool CheckVictoryCondition()
    {
        // 检查玩家是否进入敌方基地
        foreach (var character in playerTeam)
        {
            if (character.isAlive && GridManager.Instance.IsCharacterInEnemyBase(character, GetEnemyFaction()))
            {
                Debug.Log("Player wins! (Character entered enemy base)");
                battleActive = false;
                GameManager.Instance.EndGame("玩家");
                return true;
            }
        }

        // 检查敌方是否进入玩家基地
        foreach (var character in enemyTeam)
        {
            if (character.isAlive && GridManager.Instance.IsCharacterInEnemyBase(character, GetPlayerFaction()))
            {
                Debug.Log("Enemy wins! (Character entered player base)");
                battleActive = false;
                GameManager.Instance.EndGame("敌方");
                return true;
            }
        }

        // 检查全灭
        bool playerAllDead = playerTeam.TrueForAll(c => !c.isAlive);
        bool enemyAllDead = enemyTeam.TrueForAll(c => !c.isAlive);

        if (playerAllDead)
        {
            Debug.Log("Enemy wins! (Player team eliminated)");
            battleActive = false;
            GameManager.Instance.EndGame("敌方");
            return true;
        }

        if (enemyAllDead)
        {
            Debug.Log("Player wins! (Enemy team eliminated)");
            battleActive = false;
            GameManager.Instance.EndGame("玩家");
            return true;
        }

        return false;
    }

    // 获取敌方阵营
    private string GetEnemyFaction()
    {
        if (enemyTeam.Count > 0)
            return enemyTeam[0].faction;
        return "";
    }

    // 获取玩家阵营
    private string GetPlayerFaction()
    {
        if (playerTeam.Count > 0)
            return playerTeam[0].faction;
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
            Vector2Int spawnPoint = GridManager.Instance.GetSpawnPoint(character.faction);
            character.Revive(spawnPoint.x, spawnPoint.y);
            // 复活后下一回合无法行动
            character.canActThisTurn = false;
        }
        else
        {
            Debug.Log($"{character.characterName} has no revive chance, removed from battle");
            // 从队伍中移除（保留对象但不参与战斗）
        }
    }
}

