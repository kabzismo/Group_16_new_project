using UnityEngine;

namespace FPSStarter
{
    public sealed class Stage2CompletionScreen : MonoBehaviour
    {
        private bool visible;
        private GUIStyle titleStyle;
        private GUIStyle messageStyle;

        public void Show()
        {
            visible = true;
        }

        private void OnGUI()
        {
            if (!visible) return;

            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none);

            EnsureStyles();

            GUI.Label(
                new Rect(0f, Screen.height * 0.38f, Screen.width, 56f),
                "CHALLENGE COMPLETE",
                titleStyle);

            GUI.Label(
                new Rect(0f, Screen.height * 0.49f, Screen.width, 34f),
                "You collected all four keys. The game will now close.",
                messageStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = Color.white;

            messageStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20
            };
            messageStyle.normal.textColor = Color.white;
        }
    }
}
