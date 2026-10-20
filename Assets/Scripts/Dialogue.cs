
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using FPSStarter;

public class Dialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed = 0.03f;

    [Header("Dialogue UI")]
    public Image characterImage;
    public GameObject dialoguePanel;
    public GameObject levelChoicePanel;

    [Header("Player Scripts")]
    [SerializeField] private FirstPersonController fpController;
    [SerializeField] private PlayerInteractor interactor;

    private int index;
    private bool isTyping;
    private bool isActive;

    public bool IsActive => isActive;

    private void Awake()
    {
        if (fpController == null)
            fpController = FindFirstObjectByType<FirstPersonController>();

        if (interactor == null)
            interactor = FindFirstObjectByType<PlayerInteractor>();
    }

    private void Start()
    {
        textComponent.text = string.Empty;

        characterImage.gameObject.SetActive(false);
        dialoguePanel.SetActive(false);
        levelChoicePanel.SetActive(false);
    }

    private void Update()
    {
        // Keep the cursor visible during dialogue and level selection.
        if (isActive || (levelChoicePanel != null &&
                         levelChoicePanel.activeInHierarchy))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!isActive)
            return;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                StopAllCoroutines();

                textComponent.text = lines[index];
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue()
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Dialogue has no lines assigned.");
            return;
        }

        if (isActive)
            return;

        StopAllCoroutines();

        // Disable player movement and interaction.
        SetPlayerControls(false);

        // Unlock the cursor.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        index = 0;
        isTyping = false;
        isActive = true;

        textComponent.text = string.Empty;

        dialoguePanel.SetActive(true);
        characterImage.gameObject.SetActive(true);
        levelChoicePanel.SetActive(false);

        StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        textComponent.text = string.Empty;

        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSecondsRealtime(textSpeed);
        }

        isTyping = false;
    }

    private void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            StartCoroutine(TypeLine());
        }
        else
        {
            // End dialogue without restoring player controls.
            isActive = false;
            StopAllCoroutines();

            characterImage.gameObject.SetActive(false);
            dialoguePanel.SetActive(false);

            // Show level selection.
            levelChoicePanel.SetActive(true);

            // Keep cursor free for clicking the level button.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void SetPlayerControls(bool state)
    {
        if (fpController != null)
            fpController.enabled = state;
        else
            Debug.LogWarning(
                "Dialogue: FirstPersonController was not found.");

        if (interactor != null)
            interactor.enabled = state;
        else
            Debug.LogWarning(
                "Dialogue: PlayerInteractor was not found.");
    }

    // Optional: call this only when returning to gameplay.
    public void ResumePlayer()
    {
        SetPlayerControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
