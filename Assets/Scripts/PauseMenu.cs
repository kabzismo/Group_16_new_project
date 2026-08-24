using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    public GameObject pausebutton;

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        pauseMenu.SetActive(false);
        controlsPanel.SetActive(false);
        pausebutton.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenu.SetActive(true);
        controlsPanel.SetActive(false);
        pausebutton.SetActive(false);
    }

    public void OpenControls()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);
        pausebutton.SetActive(false);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(true);
        pauseMenu.SetActive(false);
        pausebutton.SetActive(false);
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        Time.timeScale = 0f;

        pausebutton.SetActive(false);
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        Time.timeScale = 1f;

        pausebutton.SetActive(true);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Time.timeScale > 0f)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
    }
}