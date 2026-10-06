using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using FPSStarter;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu")]
    public GameObject pauseMenu;
    public GameObject settingsPanel;
    public GameObject controlsPanel;

    [Header("Pause Button")]
    public GameObject pausebutton;
    public GameObject pauseButtonTitle;

    [Header("Player")]
    public GameObject player;

    private FirstPersonController fpcController;
    private PlayerInteractor playerInteractor;

    private void Start()
    {
       
        if (player != null)
        {
            fpcController = player.GetComponent<FirstPersonController>();
            playerInteractor = player.GetComponent<PlayerInteractor>();
        }
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        pauseMenu.SetActive(false);
        controlsPanel.SetActive(false);

        pauseButtonTitle.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenu.SetActive(true);
        controlsPanel.SetActive(false);

        pauseButtonTitle.SetActive(false);
    }

    public void OpenControls()
    {
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);

        pauseButtonTitle.SetActive(false);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(true);
        pauseMenu.SetActive(false);

        pauseButtonTitle.SetActive(false);
    }

    public void PauseGame()
    {
        // Show pause menu
        pauseMenu.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        // Hide pause button title
        pauseButtonTitle.SetActive(false);

        // Unlock and show cursor for the pause menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player controls
        if (fpcController != null)
            fpcController.enabled = false;

        if (playerInteractor != null)
            playerInteractor.enabled = false;

        // Pause gameplay
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        // Hide pause menu
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        // Show pause button title again
        pauseButtonTitle.SetActive(true);

        // Resume gameplay
        Time.timeScale = 1f;

        // Re-enable player controls
        if (fpcController != null)
            fpcController.enabled = true;

        if (playerInteractor != null)
            playerInteractor.enabled = true;

        // Lock cursor back into the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        
        Time.timeScale = 1f;

        if (fpcController != null)
            fpcController.enabled = true;

        if (playerInteractor != null)
            playerInteractor.enabled = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
