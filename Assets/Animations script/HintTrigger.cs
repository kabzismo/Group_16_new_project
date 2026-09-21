using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class HintTrigger : MonoBehaviour
{
    public Hint hint;

    [TextArea]
    public string[] hintLines;

    public Key interactKey = Key.E;

    public GameObject promptUI;
    public GameObject hintIcon;
    public GameObject spawnParticles;

    public bool isUnlocked = true;
    public bool onlyOnce = false;

    private bool playerInRange;
    private bool used;

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);

        if (hintIcon != null)
            hintIcon.SetActive(false);

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
            ShowHint();
        }
    }

    public void Unlock()
    {
        isUnlocked = true;
    }

    public void ShowHint()
    {
        hint.StartHint(hintLines);

        if (onlyOnce)
            used = true;

        if (promptUI != null)
            promptUI.SetActive(false);

        if (hintIcon != null)
            hintIcon.SetActive(false);

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
            if (hintIcon != null)
                hintIcon.SetActive(true);

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

        if (hintIcon != null)
            hintIcon.SetActive(false);

        if (spawnParticles != null)
            spawnParticles.SetActive(false);

        if (promptUI != null)
            promptUI.SetActive(false);
    }
}

