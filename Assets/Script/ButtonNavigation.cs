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
        Credits
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
        }
    }
}
