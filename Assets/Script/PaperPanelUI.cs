using UnityEngine;
using UnityEngine.UI;

public class PaperPanelUI : MonoBehaviour
{
    public static PaperPanelUI Instance;

    public GameObject panel;
    public Image paperImage;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        panel.SetActive(false);
    }

    public void Open(Sprite sprite)
    {
        paperImage.sprite = sprite;
        panel.SetActive(true);
        Time.timeScale = 0;
    }

    public void Close()
    {
        panel.SetActive(false);
        Time.timeScale = 1;
    }
}
