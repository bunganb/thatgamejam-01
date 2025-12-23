using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
    }

    public void GoToMainMenu()
    {
        
    }
    public void GoToSettings()
    {
    }
    public  void GoToGame()
    {
    }

    public void GoToCredits()
    {
        
    }

    public void PauseGame()
    {
        
    }
}
