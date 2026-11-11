using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSStarter
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.1f)] private float interactionDistance = 4.5f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField, Min(0.1f)] private float throwForce = 12f;
        [SerializeField, Min(0.01f)] private float holdRotationSensitivity = 0.25f;
        [SerializeField, Min(0.001f)] private float holdDistanceSensitivity = 0.01f;

        public string CurrentPrompt { get; private set; }
        internal bool HasThemedPrompt { get; private set; }
        internal RoomMechanic PromptRoom { get; private set; }
        private FirstPersonController controller;
        private CarryableObject heldObject;
        private CarryableObject assistedKey;
        private BoxCollider assistedKeyTarget;
        private bool stage2;
        private int priorityInteractionFrame = -1;

        // Hints also listen for E. Resolve the current ray directly so the
        // result is valid regardless of which component updates first.
        public bool HasPriorityInteraction
        {
            get
            {
                if (priorityInteractionFrame == Time.frameCount) return true;
                if (!isActiveAndEnabled || playerCamera == null || heldObject != null) return false;
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore);
                CarryableObject key = ResolvePickup(ray, hit.collider);
                return key != null && key.IsStage2Key || hit.collider != null &&
                    (hit.collider.GetComponentInParent<Stage2ExitInteractable>() != null ||
                     stage2 && hit.collider.GetComponentInParent<DoorOutward>() != null);
            }
        }

        private void Awake()
        {
            controller = GetComponent<FirstPersonController>();
            stage2 = gameObject.scene.name == "Stage 2";
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera == null) playerCamera = Camera.main;
        }

        internal void SetAssistedKey(CarryableObject key)
        {
            assistedKey = key;
            assistedKeyTarget = key != null ? key.GetComponent<BoxCollider>() : null;
        }

        private void Update()
        {
            CurrentPrompt = string.Empty;
            HasThemedPrompt = false;
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return;
            bool interactPressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                                   (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            // Room-mechanic volumes are triggers. Ignoring them ensures the ray
            // reaches the solid collider on a door, key, or other interactable.
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore);

            if (heldObject != null)
            {
                UpdateHeldObject();
                if (heldObject == null) return;
                IItemReceiver receiver = hitSomething ? FindReceiver(hit.collider) : null;
                CurrentPrompt = receiver != null ? receiver.HeldItemPrompt : "[E] Drop | RMB throw | Wheel move | MMB drag rotate";
                if (receiver != null && interactPressed && receiver.TryPlace(heldObject)) heldObject = null;
                else if (receiver == null && interactPressed)
                {
                    heldObject.Drop();
                    heldObject = null;
                }
                return;
            }

            CarryableObject carryable = ResolvePickup(ray, hit.collider);
            if (carryable != null)
            {
                CurrentPrompt = "[E] Pick up " + carryable.ItemName;
                if (stage2 && carryable.KeyRoom != RoomMechanic.None)
                {
                    CurrentPrompt = "[E] Pick up key";
                    RoomMechanic currentRoom = controller != null ? controller.CurrentMechanic : RoomMechanic.None;
                    SetPromptRoom(currentRoom != RoomMechanic.None ? currentRoom : carryable.KeyRoom);
                }
                if (interactPressed)
                {
                    // Stage 2 keys are collectibles, not physics objects the player
                    // needs to carry around. Claim them immediately and remove them
                    // from the room so they cannot be collected more than once.
                    if (carryable.IsStage2Key)
                    {
                        priorityInteractionFrame = Time.frameCount;
                        Stage2KeyHunt hunt = FindFirstObjectByType<Stage2KeyHunt>();
                        if (hunt != null)
                        {
                            if (hunt.Claim(carryable.gameObject)) Destroy(carryable.gameObject);
                            return;
                        }
                    }

                    carryable.PickUp(controller != null ? controller.HoldPoint : transform);
                    heldObject = carryable;
                }
                return;
            }

            if (!hitSomething) return;
            IInteractable interactable = FindInteractable(hit.collider);
            if (interactable == null) return;
            CurrentPrompt = interactable.Prompt;
            bool isExit = interactable is Stage2ExitInteractable;
            bool isDoor = stage2 && interactable is DoorOutward;
            if (isDoor)
            {
                RoomMechanic room = ((DoorOutward)interactable).PromptRoom;
                SetPromptRoom(room != RoomMechanic.None || controller == null ? room : controller.CurrentMechanic);
            }
            else if (stage2 && isExit) SetPromptRoom(RoomMechanic.None);
            if (interactPressed)
            {
                if (isExit || isDoor) priorityInteractionFrame = Time.frameCount;
                interactable.Interact(gameObject);
            }
        }

        private void SetPromptRoom(RoomMechanic room)
        {
            HasThemedPrompt = true;
            PromptRoom = room;
        }

        private CarryableObject ResolvePickup(Ray ray, Collider directHit)
        {
            CarryableObject directKey = directHit != null ? directHit.GetComponentInParent<CarryableObject>() : null;
            if (directKey != null && !directKey.IsHeld && !directKey.IsPlaced) return directKey;
            if (assistedKey == null || assistedKeyTarget == null || !assistedKeyTarget.enabled ||
                !assistedKey.isActiveAndEnabled || assistedKey.IsHeld || assistedKey.IsPlaced) return null;

            const float aimTolerance = .8f;
            Vector3 towardKey = assistedKeyTarget.bounds.center - ray.origin;
            float alongRay = Vector3.Dot(towardKey, ray.direction);
            if (alongRay <= 0f || towardKey.sqrMagnitude > interactionDistance * interactionDistance ||
                (towardKey - ray.direction * alongRay).sqrMagnitude > aimTolerance * aimTolerance) return null;

            // Give a directly aimed interaction priority, and never reach the key through a maze wall.
            if (directHit != null && FindInteractable(directHit) != null) return null;
            return Physics.Linecast(ray.origin, ray.origin + towardKey, out RaycastHit hit,
                interactionMask, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<CarryableObject>() == assistedKey ? assistedKey : null;
        }

        private void OnDisable() => CurrentPrompt = string.Empty;

        private static IItemReceiver FindReceiver(Collider collider)
        {
            foreach (MonoBehaviour behaviour in collider.GetComponentsInParent<MonoBehaviour>()) if (behaviour is IItemReceiver receiver) return receiver;
            return null;
        }

        private static IInteractable FindInteractable(Collider collider)
        {
            foreach (MonoBehaviour behaviour in collider.GetComponentsInParent<MonoBehaviour>()) if (behaviour is IInteractable interactable) return interactable;
            return null;
        }

        private void UpdateHeldObject()
        {
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || (gamepad != null && gamepad.rightTrigger.wasPressedThisFrame))
            {
                heldObject.Throw(playerCamera != null ? playerCamera.transform.forward : transform.forward, throwForce);
                heldObject = null;
                return;
            }
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.001f) heldObject.AdjustHoldDistance(scroll * holdDistanceSensitivity);
            if (mouse.middleButton.isPressed) heldObject.RotateInHand(mouse.delta.ReadValue(), holdRotationSensitivity);
        }
    }
}
