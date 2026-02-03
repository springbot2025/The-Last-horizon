using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("角色选择界面")]
    public GameObject selectionPanel;
    public Transform selectionContainer;
    public GameObject characterButtonPrefab;

    [Header("战斗界面")]
    public GameObject battlePanel;
    public Text roundText;

    [Header("结算界面")]
    public GameObject gameOverPanel;
    public Text winnerText;

    private List<Character> selectedTeam1 = new List<Character>();
    private List<Character> selectedTeam2 = new List<Character>();

    public void ShowCharacterSelectionUI(List<Character> availableCharacters)
    {
        selectionPanel.SetActive(true);
        battlePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        foreach (Transform child in selectionContainer)
            Destroy(child.gameObject);

        foreach (var c in availableCharacters)
        {
            GameObject btn = Instantiate(characterButtonPrefab, selectionContainer);
            btn.GetComponentInChildren<Text>().text = c.characterName;
            btn.GetComponent<Button>().onClick.AddListener(() => OnCharacterSelected(c));
        }
    }

    void OnCharacterSelected(Character c)
    {
        if (selectedTeam1.Count < 2)
        {
            selectedTeam1.Add(c);
            Debug.Log($"玩家1选中：{c.characterName}");
        }
        else if (selectedTeam2.Count < 2)
        {
            selectedTeam2.Add(c);
            Debug.Log($"玩家2选中：{c.characterName}");
        }

        if (selectedTeam1.Count == 2 && selectedTeam2.Count == 2)
        {
            GameManager.Instance.OnCharactersSelected(selectedTeam1, selectedTeam2);
        }
    }

    public void HideCharacterSelectionUI()
    {
        selectionPanel.SetActive(false);
    }

    public void ShowBattleUI()
    {
        battlePanel.SetActive(true);
    }

    public void ShowGameOver(string winner)
    {
        battlePanel.SetActive(false);
        gameOverPanel.SetActive(true);
        winnerText.text = $"{winner} 获胜！";
    }
    public static UIManager Instance;

    public bool HasMadeChoice { get; private set; } = false;
    private string playerChoice = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShowActionMenu(Character currentChar)
    {
        Debug.Log($"显示操作菜单（假装有按钮）：攻击/移动/技能/护盾");
    }

    public void PlayerSelectAction(string action)
    {
        playerChoice = action;
        HasMadeChoice = true;
    }

    public string GetPlayerChoice() => playerChoice;

    public void ResetChoice()
    {
        HasMadeChoice = false;
        playerChoice = "";
    }
}
