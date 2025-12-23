using UnityEngine;

public class ButtonNavigation : MonoBehaviour
{
    public enum ButtonTargets
    {
        MainMenu,
        Settings,
        Quit,
        Pause,
        InGame,
        Credits,
        Back,
        Resume
    }
    public  ButtonTargets ButtonTarget;

    public void OnButtonClicked()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager.Instance is NULL!");
            return;
        }

        switch (ButtonTarget)
        {
            case ButtonTargets.MainMenu:
                UIManager.Instance.GoToMainMenu();
                break;
            case ButtonTargets.Credits:
                UIManager.Instance.GoToCredits();
                break;
            case ButtonTargets.InGame:
                UIManager.Instance.GoToGame();
                break;
            case ButtonTargets.Settings:
                UIManager.Instance.GoToSettings();
                break;
            case ButtonTargets.Quit:
                Application.Quit();
                break;
            case ButtonTargets.Back:
                UIManager.Instance.GoBack();
                break;
            case ButtonTargets.Resume:
                UIManager.Instance.ResumeGame();
                break;
            case ButtonTargets.Pause:
                UIManager.Instance.PauseGame();
                break;
        }
    }
}
