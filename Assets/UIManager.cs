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

    public static UIManager Instance;

    public bool HasMadeChoice { get; private set; } = false;
    private string playerChoice = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShowCharacterSelectionUI(List<Character> availableCharacters)
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
        if (battlePanel != null)
            battlePanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (selectionContainer != null)
        {
            foreach (Transform child in selectionContainer)
                Destroy(child.gameObject);
        }

        if (characterButtonPrefab != null && selectionContainer != null)
        {
            foreach (var c in availableCharacters)
            {
                GameObject btn = Instantiate(characterButtonPrefab, selectionContainer);
                Text textComp = btn.GetComponentInChildren<Text>();
                if (textComp != null)
                    textComp.text = c.characterName;
                
                Button buttonComp = btn.GetComponent<Button>();
                if (buttonComp != null)
                    buttonComp.onClick.AddListener(() => OnCharacterSelected(c));
            }
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
            if (GameManager.Instance != null)
                GameManager.Instance.OnCharactersSelected(selectedTeam1, selectedTeam2);
        }
    }

    public void HideCharacterSelectionUI()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    public void ShowBattleUI()
    {
        if (battlePanel != null)
            battlePanel.SetActive(true);
    }

    public void ShowGameOver(string winner)
    {
        if (battlePanel != null)
            battlePanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        if (winnerText != null)
            winnerText.text = $"{winner} 获胜！";
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

