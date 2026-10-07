using UnityEngine;

/// Connects menu buttons to Last Light's UIManager.
public class ButtonFunctions : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;

    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    public void PlayGame()
    {
        if (uiManager == null)
            return;

        uiManager.PlayButtonClick();
        uiManager.StartGame();
    }

    public void Resume()
    {
        if (uiManager == null)
            return;

        uiManager.PlayButtonClick();
        uiManager.ResumeGame();
    }

    public void Pause()
    {
        if (uiManager == null)
            return;

        uiManager.PlayButtonClick();
        uiManager.PauseGame();
    }

    public void Restart()
    {
        if (uiManager == null)
            return;

        uiManager.PlayButtonClick();
        uiManager.RestartGame();
    }

    public void ReturnToMainMenu()
    {
        if (uiManager == null)
            return;

        uiManager.PlayButtonClick();
        uiManager.ReturnToMenu();
    }

    public void Quit()
    {
        if (uiManager == null)
            return;

        uiManager.PlayButtonClick();
        uiManager.QuitGame();
    }
}