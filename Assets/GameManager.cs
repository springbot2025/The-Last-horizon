using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    CharacterSelection,
    Battle,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState currentState = GameState.CharacterSelection;

    [Header("Managers")]
    public UIManager uiManager;
    public GridManager gridManager;

    [Header("角色数据")]
    public List<Character> allCharacters = new List<Character>();  // 所有可选角色
    public List<Character> player1Team = new List<Character>();
    public List<Character> player2Team = new List<Character>();

    [Header("战斗控制")]
    public Transform player1SpawnPoint1;
    public Transform player1SpawnPoint2;
    public Transform player2SpawnPoint1;
    public Transform player2SpawnPoint2;

    private int currentRound = 0;
    private bool isPlayer1Turn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (uiManager != null && allCharacters.Count > 0)
        {
            uiManager.ShowCharacterSelectionUI(allCharacters);
        }
        currentState = GameState.CharacterSelection;
    }

    public void OnCharactersSelected(List<Character> team1, List<Character> team2)
    {
        player1Team = team1;
        player2Team = team2;

        // 进入战斗阶段
        StartBattle();
    }

    void StartBattle()
    {
        currentState = GameState.Battle;

        Debug.Log("[GameManager] Battle started!");

        // 验证角色列表
        if (player1Team == null || player1Team.Count < 2)
        {
            Debug.LogError("[GameManager] Player1 team insufficient. Need 2 characters.");
            return;
        }
        if (player2Team == null || player2Team.Count < 2)
        {
            Debug.LogError("[GameManager] Player2 team insufficient. Need 2 characters.");
            return;
        }

        // 生成地图
        if (gridManager != null)
        {
            gridManager.GenerateGrid();
            Debug.Log("[GameManager] Map generated");
        }
        else
        {
            Debug.LogError("[GameManager] GridManager is null! Cannot generate map.");
        }

        // 获取生成点（如果有GridManager，使用GridManager的生成点）
        Vector2Int p1Spawn1 = Vector2Int.zero;
        Vector2Int p1Spawn2 = Vector2Int.zero;
        Vector2Int p2Spawn1 = Vector2Int.zero;
        Vector2Int p2Spawn2 = Vector2Int.zero;

        if (gridManager != null)
        {
            // 从GridManager获取生成点
            p1Spawn1 = gridManager.player1Spawn1;
            p1Spawn2 = gridManager.player1Spawn2;
            p2Spawn1 = gridManager.player2Spawn1;
            p2Spawn2 = gridManager.player2Spawn2;
        }

        // 创建实际实例化的角色列表
        List<Character> player1Chars = new List<Character>();
        List<Character> player2Chars = new List<Character>();

        // 实例化玩家1的角色
        if (player1Team.Count > 0)
        {
            Vector3 spawnPos1 = (gridManager != null && gridManager.player1Spawn1 != null) 
                ? gridManager.GetWorldPosition(p1Spawn1.x, p1Spawn1.y)
                : (player1SpawnPoint1 != null ? player1SpawnPoint1.position : Vector3.zero);
            Character char1 = SpawnCharacter(player1Team[0], spawnPos1);
            if (char1 != null)
            {
                char1.gridX = p1Spawn1.x;
                char1.gridY = p1Spawn1.y;
                player1Chars.Add(char1);
                Debug.Log($"[GameManager] Player1 Char1 spawned: {char1.characterName} at ({p1Spawn1.x}, {p1Spawn1.y})");
            }
        }

        if (player1Team.Count > 1)
        {
            Vector3 spawnPos2 = (gridManager != null && gridManager.player1Spawn2 != null)
                ? gridManager.GetWorldPosition(p1Spawn2.x, p1Spawn2.y)
                : (player1SpawnPoint2 != null ? player1SpawnPoint2.position : Vector3.zero);
            Character char2 = SpawnCharacter(player1Team[1], spawnPos2);
            if (char2 != null)
            {
                char2.gridX = p1Spawn2.x;
                char2.gridY = p1Spawn2.y;
                player1Chars.Add(char2);
                Debug.Log($"[GameManager] Player1 Char2 spawned: {char2.characterName} at ({p1Spawn2.x}, {p1Spawn2.y})");
            }
        }

        // 实例化玩家2的角色
        if (player2Team.Count > 0)
        {
            Vector3 spawnPos3 = (gridManager != null && gridManager.player2Spawn1 != null)
                ? gridManager.GetWorldPosition(p2Spawn1.x, p2Spawn1.y)
                : (player2SpawnPoint1 != null ? player2SpawnPoint1.position : Vector3.zero);
            Character char3 = SpawnCharacter(player2Team[0], spawnPos3);
            if (char3 != null)
            {
                char3.gridX = p2Spawn1.x;
                char3.gridY = p2Spawn1.y;
                player2Chars.Add(char3);
                Debug.Log($"[GameManager] Player2 Char1 spawned: {char3.characterName} at ({p2Spawn1.x}, {p2Spawn1.y})");
            }
        }

        if (player2Team.Count > 1)
        {
            Vector3 spawnPos4 = (gridManager != null && gridManager.player2Spawn2 != null)
                ? gridManager.GetWorldPosition(p2Spawn2.x, p2Spawn2.y)
                : (player2SpawnPoint2 != null ? player2SpawnPoint2.position : Vector3.zero);
            Character char4 = SpawnCharacter(player2Team[1], spawnPos4);
            if (char4 != null)
            {
                char4.gridX = p2Spawn2.x;
                char4.gridY = p2Spawn2.y;
                player2Chars.Add(char4);
                Debug.Log($"[GameManager] Player2 Char2 spawned: {char4.characterName} at ({p2Spawn2.x}, {p2Spawn2.y})");
            }
        }

        // 验证实例化的角色数量
        if (player1Chars.Count < 2 || player2Chars.Count < 2)
        {
            Debug.LogError($"[GameManager] Character spawn failed! Player1: {player1Chars.Count}/2, Player2: {player2Chars.Count}/2");
            return;
        }

        // 隐藏角色选择UI
        if (uiManager != null)
        {
            uiManager.HideCharacterSelectionUI();
            uiManager.ShowBattleUI();
        }

        // 启动BattleManager（重要！）
        if (BattleManager.Instance != null)
        {
            Debug.Log("[GameManager] Starting BattleManager...");
            BattleManager.Instance.StartBattle(player1Chars, player2Chars);
            Debug.Log("[GameManager] BattleManager started, turn-based combat begins!");
        }
        else
        {
            Debug.LogError("[GameManager] BattleManager.Instance not found! Ensure BattleManager exists in scene.");
            // 如果BattleManager不存在，尝试查找或创建
            BattleManager battleMgr = FindObjectOfType<BattleManager>();
            if (battleMgr == null)
            {
                GameObject battleManagerObj = new GameObject("BattleManager");
                battleMgr = battleManagerObj.AddComponent<BattleManager>();
                Debug.Log("[GameManager] Created BattleManager object");
            }
            if (battleMgr != null)
            {
                Debug.Log("[GameManager] Using found BattleManager to start battle");
                battleMgr.StartBattle(player1Chars, player2Chars);
            }
        }
    }

    Character SpawnCharacter(Character prefab, Vector3 spawnPos)
    {
        if (prefab == null)
        {
            Debug.LogError("[GameManager] SpawnCharacter: prefab is null!");
            return null;
        }

        Character newChar = Instantiate(prefab, spawnPos, Quaternion.identity);
        if (newChar == null)
        {
            Debug.LogError("[GameManager] SpawnCharacter: instantiation failed!");
            return null;
        }

        // Ensure visual manager exists before initialization
        if (newChar.visualManager == null)
        {
            CharacterVisualManager visualMgr = newChar.GetComponent<CharacterVisualManager>();
            if (visualMgr == null)
            {
                visualMgr = newChar.gameObject.AddComponent<CharacterVisualManager>();
            }
            newChar.visualManager = visualMgr;
        }

        // Initialize character (this will load animations)
        newChar.Initialize();
        
        // Set grid coordinates
        if (gridManager != null)
        {
            int gridX = Mathf.FloorToInt(spawnPos.x / gridManager.cellSize);
            int gridY = Mathf.FloorToInt(spawnPos.y / gridManager.cellSize);
            newChar.gridX = gridX;
            newChar.gridY = gridY;
        }
        
        Debug.Log($"[GameManager] Spawned character {newChar.characterName} with animations at ({newChar.gridX}, {newChar.gridY})");
        return newChar;
    }

    IEnumerator BattleRoutine()
    {
        Debug.Log("Battle turn order control started");
        yield return null;
    }

    public void EndGame(string winner)
    {
        currentState = GameState.GameOver;
        if (uiManager != null)
        {
            uiManager.ShowGameOver(winner);
        }
    }
}

