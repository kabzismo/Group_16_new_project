
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject MainMenuPanel;
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    public GameObject gameSelectionPanel;

    [Header("Continue Buttons")]
    public GameObject continueLocked;
    public GameObject continueUnlocked;

    private const string LEVEL_UNLOCKED_KEY = "Level1Visited";
    private const string SESSION_STARTED_KEY = "GameSessionStarted";

    private void Start()
    {
        // Only reset progress when starting a NEW game session.
        // Returning from gameplay must NOT reset progress.
        if (PlayerPrefs.GetInt(SESSION_STARTED_KEY, 0) == 0)
        {
            PlayerPrefs.SetInt(LEVEL_UNLOCKED_KEY, 0);
            PlayerPrefs.SetInt(SESSION_STARTED_KEY, 1);
            PlayerPrefs.Save();
        }

        MainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
        gameSelectionPanel.SetActive(false);

        UpdateContinueButton();
    }

    // SETTINGS
    public void OpenSettings()
    {
        MainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        controlsPanel.SetActive(false);
        gameSelectionPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
        MainMenuPanel.SetActive(true);
    }

    // CONTROLS
    public void OpenControls()
    {
        MainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // GAME SELECTION
    public void PlayGame()
    {
        MainMenuPanel.SetActive(false);
        gameSelectionPanel.SetActive(true);

        UpdateContinueButton();
    }

    public void BackToMainMenu()
    {
        gameSelectionPanel.SetActive(false);
        MainMenuPanel.SetActive(true);
    }

    // NEW GAME
    public void NewGame()
    {
        // Reset progress for a new game
        PlayerPrefs.SetInt(LEVEL_UNLOCKED_KEY, 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Prison cell");
    }

    // CONTINUE
    public void ContinueGame()
    {
        if (PlayerPrefs.GetInt(LEVEL_UNLOCKED_KEY, 0) == 1)
        {
            SceneManager.LoadScene("Stage 2");
        }
    }

    // BUTTON VISIBILITY
    private void UpdateContinueButton()
    {
        bool unlocked =
            PlayerPrefs.GetInt(LEVEL_UNLOCKED_KEY, 0) == 1;

        continueLocked.SetActive(!unlocked);
        continueUnlocked.SetActive(unlocked);

        Debug.Log("Continue unlocked: " + unlocked);
    }

    // QUIT APPLICATION
    public void QuitGame()
    {
        // End this game session
        PlayerPrefs.SetInt(SESSION_STARTED_KEY, 0);
        PlayerPrefs.Save();

        Application.Quit();
    }
}
