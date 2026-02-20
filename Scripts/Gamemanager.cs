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
        uiManager.ShowCharacterSelectionUI(allCharacters);
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

        // 生成地图（5行8列）
        gridManager.GenerateGrid();

        // 获取出生点坐标
        Vector2Int p1Spawn1 = gridManager.player1Spawn1;
        Vector2Int p1Spawn2 = gridManager.player1Spawn2;
        Vector2Int p2Spawn1 = gridManager.player2Spawn1;
        Vector2Int p2Spawn2 = gridManager.player2Spawn2;

        // 生成角色并设置网格坐标，创建实际实例列表
        List<Character> player1Chars = new List<Character>();
        List<Character> player2Chars = new List<Character>();

        Character char1 = SpawnCharacter(player1Team[0], gridManager.GetWorldPosition(p1Spawn1.x, p1Spawn1.y));
        char1.gridX = p1Spawn1.x;
        char1.gridY = p1Spawn1.y;
        player1Chars.Add(char1);

        Character char2 = SpawnCharacter(player1Team[1], gridManager.GetWorldPosition(p1Spawn2.x, p1Spawn2.y));
        char2.gridX = p1Spawn2.x;
        char2.gridY = p1Spawn2.y;
        player1Chars.Add(char2);

        Character char3 = SpawnCharacter(player2Team[0], gridManager.GetWorldPosition(p2Spawn1.x, p2Spawn1.y));
        char3.gridX = p2Spawn1.x;
        char3.gridY = p2Spawn1.y;
        player2Chars.Add(char3);

        Character char4 = SpawnCharacter(player2Team[1], gridManager.GetWorldPosition(p2Spawn2.x, p2Spawn2.y));
        char4.gridX = p2Spawn2.x;
        char4.gridY = p2Spawn2.y;
        player2Chars.Add(char4);

        uiManager.HideCharacterSelectionUI();
        uiManager.ShowBattleUI();

        // 启动战斗管理器，传入实际实例化的角色列表
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.StartBattle(player1Chars, player2Chars);
        }
        else
        {
            Debug.LogError("BattleManager.Instance 未找到！请确保场景中有BattleManager对象。");
        }
    }

    Character SpawnCharacter(Character prefab, Vector3 spawnPos)
    {
        Character newChar = Instantiate(prefab, spawnPos, Quaternion.identity);
        newChar.Initialize();
        return newChar;
    }

    IEnumerator BattleRoutine()
    {
        Debug.Log("开始战斗回合顺序控制");
        yield return null;
    }

    public void EndGame(string winner)
    {
        currentState = GameState.GameOver;
        uiManager.ShowGameOver(winner);
    }
}
