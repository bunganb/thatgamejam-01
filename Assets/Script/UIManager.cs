using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Panels")]
    public GameObject MainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject CreditsPanel;
    public GameObject InGamePanel;
    public GameObject PausePanel;
    public GameObject WinPanel;
    public GameObject GameOverPanel;
    
    [Header("Skill Cooldown UI")]
    [SerializeField] private Image skillCooldownImage;
    private PlayerSkillController skill;
    
    [Header("Buttons")]
    public GameObject SkillButton;
    //history panel
    private Stack<GameObject> uiHistory = new Stack<GameObject>();
    private GameObject currentPanel;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentPanel = MainMenuPanel;
        MainMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
        InGamePanel.SetActive(false);
        PausePanel.SetActive(false);

        skill = FindAnyObjectByType<PlayerSkillController>();

        if (skillCooldownImage != null)
            skillCooldownImage.fillAmount = 0f;
    }
    private void Update()
    {
        HandleSkillKeyboard();
        UpdateSkillCooldownUI();
    }
    private void HandleSkillKeyboard()
    {
        if (skill == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            BellButtonActivate();
        }
    }
    private void UpdateSkillCooldownUI()
    {
        if (skill == null || skillCooldownImage == null)
            return;

        skillCooldownImage.fillAmount = skill.CooldownProgress;
    }


    private void SwitchTo(GameObject targetPanel, bool recordHistory = true)
    {
        if (currentPanel == targetPanel) return;

        if (currentPanel != null)
        {
            if (recordHistory)
                uiHistory.Push(currentPanel);

            currentPanel.SetActive(false);
        }

        targetPanel.SetActive(true);
        currentPanel = targetPanel;
    }
    

    public void GoToMainMenu()
    {
        uiHistory.Clear();
        Reset();
        SwitchTo(MainMenuPanel, false);
        Time.timeScale = 0f;
    }
    
    public void GoToSettings()
    {
        SwitchTo(SettingsPanel);
    }
    
    public  void GoToGame()
    {
        uiHistory.Clear();
        SwitchTo(InGamePanel, false);
        Time.timeScale = 1f;
    }

    public void GoToCredits()
    {
       SwitchTo(CreditsPanel);
    }

    public void PauseGame()
    {
        uiHistory.Clear();
        SwitchTo(PausePanel, false);
        Time.timeScale = 0f;
    }
    public void GoBack()
    {
        if (uiHistory.Count == 0) return;

        currentPanel.SetActive(false);
        currentPanel = uiHistory.Pop();
        currentPanel.SetActive(true);
    }
    private void Reset()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    public void ResumeGame()
    {
        uiHistory.Clear();
        SwitchTo(InGamePanel, false);
        Time.timeScale = 1f;
    }

    public void BellButtonActivate()
    {
        if (skill == null)
        {
            Debug.LogError("PlayerSkillController not found");
            return;
        }

        if (!skill.TryActivateSkill())
        {
            Debug.Log("Skill on cooldown");
        }
    }

    public void GameOverPanelUI()
    { 
        GameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void WinPanelUI()
    {
        WinPanel.SetActive(true);
        Time.timeScale = 0f;
    }

}
