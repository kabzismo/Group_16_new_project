using UnityEngine;

namespace FPSStarter
{
    /// <summary>The final table becomes usable after recovering the four room keys.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Stage2ExitInteractable : MonoBehaviour, IInteractable
    {
        public string Prompt
        {
            get
            {
                Stage2KeyHunt hunt = FindFirstObjectByType<Stage2KeyHunt>();
                if (hunt == null || hunt.IsEnding) return string.Empty;
                return hunt.ClaimedCount == Stage2KeyHunt.RequiredKeys
                    ? "[E] Use final key - finish game"
                    : "Exit locked - recover all four room keys (" + hunt.ClaimedCount + "/" + Stage2KeyHunt.RequiredKeys + ")";
            }
        }

        public void Interact(GameObject interactor)
        {
            FindFirstObjectByType<Stage2KeyHunt>()?.TryComplete();
        }
    }
}
