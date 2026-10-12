using UnityEngine;

namespace FPSStarter
{
    public sealed class Stage2CompletionScreen : MonoBehaviour
    {
        [Header("Screen Content")]
        [SerializeField] private string heading = "CHALLENGE COMPLETE";
        [SerializeField, TextArea(2, 4)] private string message = "All four keys recovered.\nYou made it through every room.";

        [Header("Screen Appearance")]
        [SerializeField] private Font font;
        [SerializeField] private Color overlayColor = new Color(0.025f, 0.035f, 0.045f, 0.94f);
        [SerializeField] private Color panelColor = new Color(0.065f, 0.085f, 0.1f, 1f);
        [SerializeField] private Color accentColor = new Color(1f, 0.75f, 0.35f, 1f);
        [SerializeField, Range(20, 48)] private int headingFontSize = 36;
        [SerializeField, Range(14, 24)] private int messageFontSize = 18;

        private bool visible;
        private float shownAt;
        private float displayDuration = 4f;
        private GUIStyle eyebrowStyle;
        private GUIStyle titleStyle;
        private GUIStyle messageStyle;
        private GUIStyle countdownStyle;

        public void Show(float duration = 4f)
        {
            displayDuration = Mathf.Max(0.1f, duration);
            shownAt = Time.unscaledTime;
            visible = true;
        }

        private void OnGUI()
        {
            if (!visible) return;

            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            try
            {
                GUI.depth = -1000;
                GUI.color = Color.white;
                float scale = RoomHudGraphics.Scale;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                float width = Screen.width / scale;
                float height = Screen.height / scale;
                RoomHudGraphics.Fill(new Rect(0f, 0f, width, height), overlayColor);

                Rect panel = new Rect((width - 620f) * 0.5f, (height - 380f) * 0.5f, 620f, 380f);
                RoomHudGraphics.Panel(panel, panelColor, accentColor);
                float x = panel.x + 36f;
                float innerWidth = panel.width - 72f;
                GUI.Label(new Rect(x, panel.y + 24f, innerWidth, 24f), "STAGE 02  /  ALL KEYS RECOVERED", eyebrowStyle);

                for (int i = 0; i < Stage2KeyHunt.RequiredKeys; i++)
                    RoomHudGraphics.Key(new Rect(panel.center.x - 108f + i * 60f, panel.y + 66f, 36f, 50f), accentColor, panelColor);

                GUI.Label(new Rect(x, panel.y + 136f, innerWidth, 56f), heading, titleStyle);
                GUI.Label(new Rect(x, panel.y + 196f, innerWidth, 64f), message, messageStyle);

                float remaining = Mathf.Max(0f, displayDuration - (Time.unscaledTime - shownAt));
                GUI.Label(new Rect(x, panel.y + 292f, innerWidth, 24f),
                    "CLOSING IN " + Mathf.CeilToInt(remaining) + " SECONDS", countdownStyle);
                Rect track = new Rect(x, panel.y + 336f, innerWidth, 5f);
                RoomHudGraphics.Fill(track, new Color(1f, 1f, 1f, 0.08f));
                RoomHudGraphics.Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(remaining / displayDuration), track.height), accentColor);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.depth = previousDepth;
                GUI.color = previousColor;
            }
        }

        private void EnsureStyles()
        {
            RoomHudGraphics.LabelStyle(ref eyebrowStyle, 13, accentColor, TextAnchor.MiddleCenter, font, true);
            RoomHudGraphics.LabelStyle(ref titleStyle, headingFontSize, Color.white, TextAnchor.MiddleCenter, font, true);
            RoomHudGraphics.LabelStyle(ref messageStyle, messageFontSize, new Color(0.78f, 0.82f, 0.85f), TextAnchor.MiddleCenter, font);
            RoomHudGraphics.LabelStyle(ref countdownStyle, 13, accentColor, TextAnchor.MiddleCenter, font, true);
        }
    }

    // Both overlays share a reference canvas, so their spacing and text scale
    // together at different resolutions without affecting other GUI scripts.
    internal static class RoomHudGraphics
    {
        public static float Scale => Mathf.Max(0.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));

        public static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public static void Panel(Rect rect, Color background, Color accent)
        {
            Fill(new Rect(rect.x + 5f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.3f));
            Fill(rect, new Color(accent.r, accent.g, accent.b, 0.45f));
            Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), background);
            Fill(new Rect(rect.x, rect.y, rect.width, 3f), accent);
        }

        public static void LabelStyle(ref GUIStyle style, int size, Color color, TextAnchor alignment, Font font, bool bold = false)
        {
            if (style == null) style = new GUIStyle(GUI.skin.label);
            style.font = font != null ? font : GUI.skin.font;
            style.fontSize = size;
            style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            style.alignment = alignment;
            style.wordWrap = true;
            style.padding = new RectOffset(0, 0, 0, 0);
            style.normal.textColor = color;
        }

        public static void Key(Rect rect, Color color, Color background)
        {
            Fill(new Rect(rect.x + 6f, rect.y, 24f, 24f), color);
            Fill(new Rect(rect.x + 11f, rect.y + 5f, 14f, 14f), background);
            Fill(new Rect(rect.x + 15f, rect.y + 22f, 6f, 28f), color);
            Fill(new Rect(rect.x + 19f, rect.y + 34f, 10f, 5f), color);
            Fill(new Rect(rect.x + 19f, rect.y + 45f, 10f, 5f), color);
        }
    }
}
