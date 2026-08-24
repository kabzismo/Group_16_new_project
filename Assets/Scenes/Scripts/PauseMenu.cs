using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    public GameObject pauseButton;

    void Start()
    {
        Time.timeScale = 1f;

        pauseButton.SetActive(true);

        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
            {
                // Escape from Settings goes back to Pause Menu
                CloseSettings();
            }
            else if (controlsPanel.activeSelf)
            {
                // Escape from Controls goes back to Pause Menu
                CloseControls();
            }
            else if (pauseMenu.activeSelf)
            {
                // Escape from Pause Menu resumes the game
                ResumeGame();
            }
            else
            {
                // Escape while playing opens Pause Menu
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        pauseButton.SetActive(false);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        pauseButton.SetActive(true);

        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(true);
        controlsPanel.SetActive(false);

        pauseButton.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
        pauseMenu.SetActive(true);

        pauseButton.SetActive(false);
    }

    public void OpenControls()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);

        pauseButton.SetActive(false);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        pauseMenu.SetActive(true);

        pauseButton.SetActive(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}