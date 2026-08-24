using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed;

    public Image characterImage;

    // Panel that appears after the dialogue is finished
    public GameObject levelChoicePanel;

    private int index;
    private bool isTyping;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (textComponent != null) textComponent.text = string.Empty;
        if (characterImage != null) characterImage.gameObject.SetActive(true);
        if (levelChoicePanel != null) levelChoicePanel.SetActive(false);

        if (lines == null || lines.Length == 0)
        {
            ShowLevelChoice();
            return;
        }

        StartDialogue();
    }

    void Update()
    {
        if (!AdvancePressed()) return;

        if (isTyping)
        {
            StopAllCoroutines();
            if (textComponent != null) textComponent.text = lines[index];
            isTyping = false;
        }
        else
        {
            NextLine();
        }
    }

    private static bool AdvancePressed()
    {
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool keyboard = Keyboard.current != null &&
                        (Keyboard.current.spaceKey.wasPressedThisFrame ||
                         Keyboard.current.enterKey.wasPressedThisFrame ||
                         Keyboard.current.eKey.wasPressedThisFrame);
        return mouse || keyboard;
    }

    void StartDialogue()
    {
        index = 0;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        textComponent.text = string.Empty;

        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;

            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
    }

    void NextLine()
    {
        // If there are still lines left
        if (index < lines.Length - 1)
        {
            index++;

            textComponent.text = string.Empty;

            StartCoroutine(TypeLine());
        }
        else
        {
            // Dialogue is completely finished

            ShowLevelChoice();
        }
    }

    private void ShowLevelChoice()
    {
        if (characterImage != null) characterImage.gameObject.SetActive(false);
        if (levelChoicePanel != null) levelChoicePanel.SetActive(true);
        gameObject.SetActive(false);
    }
}