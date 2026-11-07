using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class HintTrigger : MonoBehaviour
{
    public Hint hint;
    [Tooltip("Room introduced by this entrance hint. Completed only after its final line.")]
    public FPSStarter.RoomMechanicVolume entranceRoom;

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
    private FPSStarter.PlayerInteractor interactor;

    void Start()
    {
        interactor = FindFirstObjectByType<FPSStarter.PlayerInteractor>();
        if (promptUI != null)
            promptUI.SetActive(false);

        if (hintIcon != null)
            hintIcon.SetActive(false);

        if (spawnParticles != null)
            spawnParticles.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange || !isUnlocked || used || Hint.IsReading)
            return;

        bool keyInteraction = interactor != null && interactor.HasPriorityInteraction;
        if (promptUI != null) promptUI.SetActive(!keyInteraction);
        if (keyInteraction) return;

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
        if (interactor != null && interactor.HasPriorityInteraction) return;
        if (hint == null || !hint.TryStartHint(hintLines, () =>
        {
            if (entranceRoom != null) entranceRoom.CompleteEntranceHint();
            if (onlyOnce) used = true;
        })) return;

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

