using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSStarter
{
    public sealed class Stage2KeyHunt : MonoBehaviour
    {
        public const int RequiredKeys = 4;
        [Header("Completion Screen")]
        [SerializeField, Min(0.5f)] private float completionScreenDuration = 4f;
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
        public bool IsEnding => ending;

        public bool HasCollected(string keyId) => claimedKeyIds.Contains(keyId);

        public bool Claim(GameObject key)
        {
            if (ending || key == null) return false;
            CarryableObject carryable = key.GetComponent<CarryableObject>();
            if (carryable == null || System.Array.IndexOf(RequiredKeyIds, carryable.ItemId) < 0 ||
                claimedKeyIds.Contains(carryable.ItemId) || !claimed.Add(key.GetInstanceID())) return false;
            claimedKeyIds.Add(carryable.ItemId);

            GameSession.CollectedItems = claimedKeyIds.Count;

            // Authored stages finish at the exit table. Older scenes without
            // that table retain their immediate four-key completion behaviour.
            if (FindFirstObjectByType<Stage2ExitInteractable>() == null) TryComplete();

            return true;
        }

        public bool TryComplete()
        {
            if (ending || !HasAllRequiredKeys()) return false;
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

            Hint.CancelActiveHint();
            foreach (HintTrigger hint in FindObjectsByType<HintTrigger>(FindObjectsSortMode.None))
            {
                if (hint.promptUI != null) hint.promptUI.SetActive(false);
                hint.enabled = false;
            }

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

            screen.Show(completionScreenDuration);

            yield return new WaitForSecondsRealtime(completionScreenDuration);

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
            if (HasAllRequiredKeys() && FindFirstObjectByType<Stage2ExitInteractable>() != null)
                GUI.Label(new Rect(18f, 44f, 540f, 28f), "All room keys recovered. Return to the exit key/table.");
        }
    }
}
