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
        MainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

    public void OpenControls()
    {
        MainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        MainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        controlsPanel.SetActive(false);
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