using FPSStarter;
using UnityEngine;

public class DoorOutward : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool openOutward = true;

    private Animator animator;
    private string openParameter;
    private bool isOpen;
    private bool useAnimator;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string Prompt => isOpen ? "[E] Close door" : "[E] Open door";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInParent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);

        openParameter = ResolveOpenParameter();
        useAnimator = animator != null && !string.IsNullOrEmpty(openParameter);
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

    private void Toggle()
    {
        isOpen = !isOpen;
        if (useAnimator) animator.SetBool(openParameter, isOpen);
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
