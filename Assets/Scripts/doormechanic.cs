using FPSStarter;
using UnityEngine;

public class DoorOutward : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    // The room interiors are on the negative local-X side of their doorway,
    // so a negative local-Y swing opens the panel into the room.
    [SerializeField] private bool openOutward = false;

    private Animator animator;
    private string openParameter;
    private bool isOpen;
    private bool useAnimator;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private string requiredKeyId;

    public string Prompt
    {
        get
        {
            if (!HasRequiredKey()) return "[E] Locked - find this room's key";
            return isOpen ? "[E] Close door" : "[E] Open door";
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInParent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);

        openParameter = ResolveOpenParameter();
        // A hinge controller deliberately disables its child Animator and drives
        // the hinge transform itself. This avoids rotating around the mesh centre.
        useAnimator = animator != null && animator.enabled && !string.IsNullOrEmpty(openParameter);
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openOutward ? 90f : -90f, 0f);

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null && meshCollider.sharedMesh == null)
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null) meshCollider.sharedMesh = filter.sharedMesh;
        }
    }

    private void Update()
    {
        if (useAnimator) return;
        Quaternion target = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, 180f * Time.deltaTime);
    }

    public void Interact() => Toggle();

    public void Interact(GameObject interactor) => Toggle();

    public void RequireKey(string keyId)
    {
        requiredKeyId = keyId;
    }

    private void Toggle()
    {
        if (!HasRequiredKey()) return;
        isOpen = !isOpen;
        if (useAnimator) animator.SetBool(openParameter, isOpen);
    }

    private bool HasRequiredKey()
    {
        if (string.IsNullOrEmpty(requiredKeyId)) return true;
        Stage2KeyHunt hunt = FindFirstObjectByType<Stage2KeyHunt>();
        return hunt != null && hunt.HasCollected(requiredKeyId);
    }

    private string ResolveOpenParameter()
    {
        if (animator == null) return null;
        if (HasBool(animator, "IsOpen")) return "IsOpen";
        return null;
    }

    private static bool HasBool(Animator target, string parameterName)
    {
        if (target == null || target.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter parameter in target.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                return true;
        }
        return false;
    }
}
