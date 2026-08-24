using System.Collections;
using UnityEngine;
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
        textComponent.text = string.Empty;

        // Show character at the start
        characterImage.gameObject.SetActive(true);

        // Make sure level choice panel is hidden
        levelChoicePanel.SetActive(false);

        StartDialogue();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
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

            // Hide the character
            characterImage.gameObject.SetActive(false);

            // Hide the dialogue panel
            gameObject.SetActive(false);

            // Show the level choice panel
            levelChoicePanel.SetActive(true);
        }
    }
}