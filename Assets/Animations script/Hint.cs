using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class Hint : MonoBehaviour
{
    public GameObject hintPanel;
    public TextMeshProUGUI textComponent;
    public Image characterImage;
    public string[] lines;
    public float textSpeed = 0.03f;
    public MonoBehaviour[] disableWhileActive;
    public UnityEvent onFinished;
    public static bool IsReading => activeHint != null && activeHint.isActive;
    private static Hint activeHint;
    private int index;
    private bool isTyping;
    private bool isActive;
    private int startFrame;
    private int closedFrame = -1;
    private System.Action completed;
    private CursorLockMode previousLock;
    private bool previousCursor;
    private readonly List<MonoBehaviour> disabled = new List<MonoBehaviour>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => activeHint = null;

    void Start()
    {
        if (isActive) return;
        if (textComponent != null) textComponent.text = string.Empty;
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    public void StartHint() => TryStartHint(lines, null);
    public void StartHint(string[] newLines) => TryStartHint(newLines, null);

    public bool TryStartHint(string[] newLines, System.Action onComplete)
    {
        if (IsReading || closedFrame == Time.frameCount || hintPanel == null || textComponent == null ||
            newLines == null || newLines.Length == 0) return false;
        lines = newLines;
        completed = onComplete;
        index = 0;
        isActive = true;
        activeHint = this;
        startFrame = Time.frameCount;
        previousLock = Cursor.lockState;
        previousCursor = Cursor.visible;
        if (disableWhileActive != null)
            foreach (var behaviour in disableWhileActive) DisableControl(behaviour);
        // Also support scenes with no manually assigned movement controls.
        foreach (var control in FindObjectsByType<FPController>(FindObjectsSortMode.None)) DisableControl(control);
        foreach (var control in FindObjectsByType<FPSStarter.PlayerInteractor>(FindObjectsSortMode.None)) DisableControl(control);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        hintPanel.SetActive(true);
        if (characterImage != null) characterImage.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(TypeLine());
        return true;
    }

    private void DisableControl(MonoBehaviour control)
    {
        // This controller owns its freeze/reset logic, so keep its Update running.
        if (control is FPSStarter.FirstPersonController) return;
        if (control == null || control == this || !control.enabled || disabled.Contains(control)) return;
        disabled.Add(control);
        control.enabled = false;
    }

    void Update()
    {
        if (!isActive || Time.frameCount == startFrame) return;
        bool advance = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (!advance) return;
        if (isTyping)
        {
            StopAllCoroutines();
            textComponent.text = lines[index];
            isTyping = false;
        }
        else if (++index < lines.Length) StartCoroutine(TypeLine());
        else Close(true);
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        textComponent.text = string.Empty;
        foreach (char c in lines[index] ?? string.Empty)
        {
            textComponent.text += c;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, textSpeed));
        }
        isTyping = false;
    }

    private void Close(bool finished)
    {
        if (!isActive) return;
        isActive = false;
        closedFrame = Time.frameCount;
        if (activeHint == this) activeHint = null;
        StopAllCoroutines();
        if (characterImage != null) characterImage.gameObject.SetActive(false);
        if (hintPanel != null) hintPanel.SetActive(false);
        foreach (var control in disabled) if (control != null) control.enabled = true;
        disabled.Clear();
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursor;
        var callback = completed;
        completed = null;
        if (finished) { callback?.Invoke(); onFinished?.Invoke(); }
    }

    void OnDisable() => Close(false);
}
