using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSStarter
{
    public sealed class Stage2KeyHunt : MonoBehaviour
    {
        public const int RequiredKeys = 4;
        private static readonly string[] RequiredKeyIds =
        {
            "stage2-key-ice",
            "stage2-key-hot",
            "stage2-key-lowgravity",
            "stage2-key-highgravity"
        };

        private readonly HashSet<int> claimed = new HashSet<int>();
        private readonly HashSet<string> claimedKeyIds = new HashSet<string>();
        private bool ending;

        public int ClaimedCount => claimedKeyIds.Count;

        public bool HasCollected(string keyId) => claimedKeyIds.Contains(keyId);

        public bool Claim(GameObject key)
        {
            if (ending || key == null) return false;
            if (!claimed.Add(key.GetInstanceID())) return false;

            CarryableObject carryable = key.GetComponent<CarryableObject>();
            if (carryable != null) claimedKeyIds.Add(carryable.ItemId);

            GameSession.CollectedItems = claimedKeyIds.Count;

            if (HasAllRequiredKeys())
                StartCoroutine(EndStage());

            return true;
        }

        private bool HasAllRequiredKeys()
        {
            foreach (string keyId in RequiredKeyIds)
            {
                if (!claimedKeyIds.Contains(keyId)) return false;
            }
            return true;
        }

        private IEnumerator EndStage()
        {
            ending = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            FirstPersonController player =
                FindFirstObjectByType<FirstPersonController>();

            if (player != null)
                player.enabled = false;

            PlayerInteractor interactor =
                FindFirstObjectByType<PlayerInteractor>();

            if (interactor != null)
                interactor.enabled = false;

            Stage2CompletionScreen screen =
                FindFirstObjectByType<Stage2CompletionScreen>();

            if (screen == null)
                screen = gameObject.AddComponent<Stage2CompletionScreen>();

            screen.Show();

            yield return new WaitForSecondsRealtime(4f);

            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnGUI()
        {
            if (ending) return;

            GUI.Label(
                new Rect(18f, 16f, 420f, 28f),
                "Keys: " + ClaimedCount + " / " + RequiredKeys);
        }
    }
}
