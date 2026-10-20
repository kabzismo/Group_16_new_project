
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    public Key interactKey = Key.E;

    public GameObject promptUI;
    public GameObject dialogueIcon;
    public GameObject spawnParticles;

    public bool isUnlocked = true;
    public bool onlyOnce = false;

    private bool playerInRange;
    private bool used;

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);

        if (dialogueIcon != null)
            dialogueIcon.SetActive(false);

        if (spawnParticles != null)
            spawnParticles.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange || !isUnlocked || used)
            return;

        if (Keyboard.current != null &&
            Keyboard.current[interactKey].wasPressedThisFrame)
        {
            ShowDialogue();
        }
    }

    public void Unlock()
    {
        isUnlocked = true;
    }

    public void ShowDialogue()
    {
        if (dialogue == null)
        {
            Debug.LogWarning("No Dialogue component assigned!");
            return;
        }

        if (onlyOnce)
            used = true;

        // Start the dialogue
        dialogue.StartDialogue();

        // Hide the interaction visuals
        if (promptUI != null)
            promptUI.SetActive(false);

        if (dialogueIcon != null)
            dialogueIcon.SetActive(false);

        if (spawnParticles != null)
            spawnParticles.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (isUnlocked && !used)
        {
            if (dialogueIcon != null)
                dialogueIcon.SetActive(true);

            if (spawnParticles != null)
                spawnParticles.SetActive(true);

            if (promptUI != null)
                promptUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (dialogueIcon != null)
            dialogueIcon.SetActive(false);

        if (spawnParticles != null)
            spawnParticles.SetActive(false);

        if (promptUI != null)
            promptUI.SetActive(false);
    }
}
