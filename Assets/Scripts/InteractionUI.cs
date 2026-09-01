using UnityEngine;
using UnityEngine.UIElements;

namespace FPSStarter
{

    public sealed class InteractionUI : MonoBehaviour
    {
        [Tooltip("The UIDocument that owns the HUD. Auto-found if blank.")]
        [SerializeField] private UIDocument hudDocument;

        [Tooltip("Name of the Label element inside the UXML to use as the prompt. Default: 'InteractionPrompt'.")]
        [SerializeField] private string promptLabelName = "InteractionPrompt";

        [Tooltip("The PlayerInteractor to read prompts from. Auto-found if blank.")]
        [SerializeField] private PlayerInteractor interactor;

        private Label promptLabel;
        private bool usingFallback;

        // Fallback OnGUI style (only allocated once).
        private GUIStyle fallbackStyle;

        private void Start()
        {
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();

            if (hudDocument == null) hudDocument = FindFirstObjectByType<UIDocument>();

            if (hudDocument != null)
            {
                promptLabel = hudDocument.rootVisualElement.Q<Label>(promptLabelName);
                if (promptLabel == null)
                {
                    Debug.LogWarning($"[InteractionUI] Could not find a Label named '{promptLabelName}' in the UIDocument. " +
                                     "Falling back to OnGUI.", this);
                    usingFallback = true;
                }
            }
            else
            {
                usingFallback = true;
            }
        }

        private void Update()
        {
            if (interactor == null || usingFallback) return;
            string prompt = interactor.CurrentPrompt;
            bool hasPrompt = !string.IsNullOrEmpty(prompt);
            promptLabel.style.display = hasPrompt ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasPrompt) promptLabel.text = prompt;
        }

        private void OnGUI()
        {
            if (!usingFallback || interactor == null) return;
            string prompt = interactor.CurrentPrompt;
            if (string.IsNullOrEmpty(prompt)) return;

            if (fallbackStyle == null)
            {
                fallbackStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
                fallbackStyle.normal.textColor = Color.white;
            }

            Vector2 size = fallbackStyle.CalcSize(new GUIContent(prompt));
            size.x += 20f;
            size.y += 10f;
            Rect rect = new Rect((Screen.width - size.x) * 0.5f, Screen.height * 0.75f, size.x, size.y);
            GUI.Box(rect, prompt, fallbackStyle);
        }
    }
}
