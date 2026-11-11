using FPSStarter;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HintTrigger : MonoBehaviour
{
    public Hint hint;
    [Tooltip("Room introduced by this entrance hint. Completed only after its final line.")]
    public RoomMechanicVolume entranceRoom;

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
    private PlayerInteractor interactor;
    private Image promptBackground;
    private TextMeshProUGUI promptText;
    private RoomMechanic Room => entranceRoom != null ? entranceRoom.Mechanic : RoomMechanic.None;

    void Start()
    {
        interactor = FindFirstObjectByType<PlayerInteractor>();
        if (promptUI != null)
        {
            if (gameObject.scene.name == "Stage 2")
            {
                promptBackground = promptUI.GetComponent<Image>();
                promptText = promptUI.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            promptUI.SetActive(false);
        }

        if (hintIcon != null)
            hintIcon.SetActive(false);

        if (spawnParticles != null)
            spawnParticles.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange || !isUnlocked || used || Hint.IsReading)
            return;

        bool priorityInteraction = interactor != null && interactor.HasPriorityInteraction;
        SetPromptVisible(!priorityInteraction);
        if (priorityInteraction) return;

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

    private void SetPromptVisible(bool visible)
    {
        if (promptUI == null) return;
        if (visible && !promptUI.activeSelf && promptText != null)
        {
            Stage2PromptTheme theme = Stage2PromptTheme.For(Room);
            if (promptBackground != null) promptBackground.color = theme.Background;
            if (promptUI.transform is RectTransform panel) panel.sizeDelta = new Vector2(258f, 38f);
            promptText.color = theme.Accent;
            promptText.fontSize = 16f;
            promptText.fontStyle = FontStyles.Bold;
            promptText.text = $"[{interactKey}] Speak to the Conductor";
        }
        promptUI.SetActive(visible);
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

            SetPromptVisible(!Hint.IsReading && (interactor == null || !interactor.HasPriorityInteraction));
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

