using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject settingsPanel;
    public GameObject controlsPanel;

    public void OpenSettings()
    {
        MainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
        MainMenuPanel.SetActive(true);
    }

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

    public void PlayGame()
    {
        SceneManager.LoadScene("Main Scene Prototype");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

