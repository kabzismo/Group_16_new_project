using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSStarter
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField, Min(0.1f)] private float throwForce = 12f;
        [SerializeField, Min(0.01f)] private float holdRotationSensitivity = 0.25f;
        [SerializeField, Min(0.001f)] private float holdDistanceSensitivity = 0.01f;

        public string CurrentPrompt { get; private set; }
        private CarryableObject heldObject;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            CurrentPrompt = string.Empty;
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) return;
            bool interactPressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                                   (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide);

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

            if (!hitSomething) return;
            CarryableObject carryable = hit.collider.GetComponentInParent<CarryableObject>();
            if (carryable != null && !carryable.IsHeld && !carryable.IsPlaced)
            {
                CurrentPrompt = "[E] Pick up " + carryable.ItemName;
                if (interactPressed)
                {
                    FirstPersonController controller = GetComponent<FirstPersonController>();
                    carryable.PickUp(controller != null ? controller.HoldPoint : transform);
                    heldObject = carryable;
                }
                return;
            }

            IInteractable interactable = FindInteractable(hit.collider);
            if (interactable == null) return;
            CurrentPrompt = interactable.Prompt;
            if (interactPressed) interactable.Interact(gameObject);
        }

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
