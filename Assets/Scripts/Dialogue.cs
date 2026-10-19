
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed = 0.03f;

    public Image characterImage;

    // The actual dialogue UI panel
    public GameObject dialoguePanel;

    // Panel that appears after the dialogue is finished
    public GameObject levelChoicePanel;

    private int index;
    private bool isTyping;
    private bool isActive;

    void Start()
    {
        textComponent.text = string.Empty;

        // Hide everything at the start
        characterImage.gameObject.SetActive(false);
        dialoguePanel.SetActive(false);
        levelChoicePanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive)
            return;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                // Instantly finish the current line
                StopAllCoroutines();

                textComponent.text = lines[index];
                isTyping = false;
            }
            else
            {
                // Move to the next line
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

        StopAllCoroutines();

        index = 0;
        isTyping = false;
        isActive = true;

        textComponent.text = string.Empty;

        dialoguePanel.SetActive(true);
        characterImage.gameObject.SetActive(true);
        levelChoicePanel.SetActive(false);

        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
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

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            StartCoroutine(TypeLine());
        }
        else
        {
            // Dialogue is finished
            isActive = false;

            StopAllCoroutines();

            characterImage.gameObject.SetActive(false);
            dialoguePanel.SetActive(false);

            // Show the option to continue to the first level
            levelChoicePanel.SetActive(true);
        }
    }
}
