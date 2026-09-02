using FPSStarter;
using UnityEngine;

/// <summary>
/// Keeps a chest lid closed until the player presses the interact key, then
/// plays its opening animation once and leaves it open permanently.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class ChestInteractable : MonoBehaviour, IInteractable
{
    private Animator chestAnimator;
    private bool opened;

    public string Prompt => opened ? "Chest opened" : "[E] Open chest";

    private void Awake()
    {
        chestAnimator = GetComponent<Animator>();

        // The supplied controller starts directly in the opening state. Keeping
        // it disabled stops that state from playing as soon as the level loads.
        if (chestAnimator != null)
            chestAnimator.enabled = false;
    }

    public void Interact(GameObject interactor)
    {
        if (opened || chestAnimator == null) return;

        opened = true;
        chestAnimator.enabled = true;
        chestAnimator.Rebind();
        chestAnimator.Play(0, 0, 0f);
    }
}
