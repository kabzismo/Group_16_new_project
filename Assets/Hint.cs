using System.Collections;
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

    private int index;
    private bool isTyping;
    private bool isActive;
    private int startFrame;

    void Start()
    {
        textComponent.text = string.Empty;
        hintPanel.SetActive(false);
    }

    
    public void StartHint()
    {
        index = 0;
        isActive = true;
        startFrame = Time.frameCount;

        
        foreach (var m in disableWhileActive)
            if (m != null) m.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        hintPanel.SetActive(true);
        characterImage.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    
    public void StartHint(string[] newLines)
    {
        lines = newLines;
        StartHint();
    }

    void Update()
    {
        if (!isActive) return;
        if (Time.frameCount == startFrame) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
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

    IEnumerator TypeLine()
    {
        isTyping = true;
        textComponent.text = string.Empty;

        foreach (char c in lines[index])
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
            isActive = false;
            characterImage.gameObject.SetActive(false);
            hintPanel.SetActive(false);

            
            foreach (var m in disableWhileActive)
                if (m != null) m.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            onFinished.Invoke();
        }
    }
}