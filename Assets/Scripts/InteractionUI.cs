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
        private GUIStyle roomLabelStyle;
        private GUIStyle actionStyle;
        private GUIStyle keycapStyle;

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
            if (interactor == null || promptLabel == null) return;
            string prompt = interactor.CurrentPrompt;
            bool hasPrompt = !interactor.HasThemedPrompt && !string.IsNullOrEmpty(prompt);
            promptLabel.style.display = hasPrompt ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasPrompt) promptLabel.text = prompt;
        }

        private void OnGUI()
        {
            if (interactor == null) return;
            string prompt = interactor.CurrentPrompt;
            if (string.IsNullOrEmpty(prompt)) return;
            if (interactor.HasThemedPrompt)
            {
                DrawRoomPrompt(prompt, Stage2PromptTheme.For(interactor.PromptRoom));
                return;
            }
            if (!usingFallback) return;

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

        private void DrawRoomPrompt(string prompt, Stage2PromptTheme theme)
        {
            const string keyPrefix = "[E] ";
            bool showKey = prompt.StartsWith(keyPrefix, System.StringComparison.Ordinal);
            string action = showKey ? prompt.Substring(keyPrefix.Length) : prompt;
            RoomHudGraphics.LabelStyle(ref roomLabelStyle, 12, theme.Accent, TextAnchor.MiddleLeft, null, true);
            RoomHudGraphics.LabelStyle(ref actionStyle, 18, Color.white, TextAnchor.MiddleLeft, null, true);
            RoomHudGraphics.LabelStyle(ref keycapStyle, 19, theme.Background, TextAnchor.MiddleCenter, null, true);
            float contentInset = showKey ? 66f : 18f;
            float width = Mathf.Max(260f, actionStyle.CalcSize(new GUIContent(action)).x + contentInset + 18f);

            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = RoomHudGraphics.Scale;
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                Rect panel = new Rect((Screen.width / scale - width) * .5f, Screen.height / scale * .74f, width, 62f);
                RoomHudGraphics.Panel(panel, theme.Background, theme.Accent);
                if (showKey)
                {
                    Rect keycap = new Rect(panel.x + 15f, panel.y + 15f, 34f, 34f);
                    RoomHudGraphics.Fill(keycap, theme.Accent);
                    GUI.Label(keycap, "E", keycapStyle);
                }
                GUI.Label(new Rect(panel.x + contentInset, panel.y + 10f, width - contentInset - 12f, 16f), theme.Title, roomLabelStyle);
                GUI.Label(new Rect(panel.x + contentInset, panel.y + 27f, width - contentInset - 12f, 25f), action, actionStyle);
            }
            finally { GUI.matrix = previousMatrix; }
        }
    }

    internal readonly struct Stage2PromptTheme
    {
        public readonly string Title;
        public readonly Color Background;
        public readonly Color Accent;

        private Stage2PromptTheme(string title, Color32 background, Color32 accent)
        {
            Title = title;
            Background = background;
            Accent = accent;
        }

        public static Stage2PromptTheme For(RoomMechanic room) => room switch
        {
            RoomMechanic.Ice => new Stage2PromptTheme("ICE ROOM", new Color32(12, 33, 53, 249),
                new Color32(155, 228, 255, 255)),
            RoomMechanic.Hot => new Stage2PromptTheme("HOT ROOM", new Color32(58, 23, 16, 249),
                new Color32(255, 166, 94, 255)),
            RoomMechanic.LowGravity => new Stage2PromptTheme("LOW GRAVITY", new Color32(37, 25, 64, 249),
                new Color32(213, 185, 255, 255)),
            RoomMechanic.HighGravity => new Stage2PromptTheme("HIGH GRAVITY", new Color32(38, 36, 26, 249),
                new Color32(223, 207, 134, 255)),
            _ => new Stage2PromptTheme("CARNIVAL", new Color32(57, 23, 32, 249),
                new Color32(255, 214, 154, 255))
        };
    }
}
