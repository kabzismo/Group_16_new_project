using UnityEngine;
using FPSStarter;

public class DoorOutward : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private bool openOutward = true;

    [Header("Hinge Pivot")]
    [Tooltip("Assign the hinge point (vertical edge of the door) to rotate around. Leave empty to rotate around the door's own transform.")]
    [SerializeField] private Transform hingePivot;

    private bool isOpen;
    private float currentAngle;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public string Prompt => isOpen ? "[E] Close door" : "[E] Open door";

    private void Start()
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null && meshCollider.sharedMesh == null)
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null) meshCollider.sharedMesh = filter.sharedMesh;
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        float targetAngle = isOpen ? openAngle : 0f;
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * Time.deltaTime);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        float direction = openOutward ? 1f : -1f;
        float appliedAngle = currentAngle * direction;

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (hingePivot != null)
            transform.RotateAround(hingePivot.position, hingePivot.up, appliedAngle);
        else
            transform.Rotate(Vector3.up, appliedAngle, Space.Self);
    }

    public void Interact(GameObject interactor) => ToggleDoor();
    public void ToggleDoor() => isOpen = !isOpen;
    public void OpenDoor() => isOpen = true;
    public void CloseDoor() => isOpen = false;
}
