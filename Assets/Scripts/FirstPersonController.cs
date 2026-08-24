using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSStarter
{
    /// <summary>Classic FPS movement: move, look, jump, and a click-to-capture cursor.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform view;
        [SerializeField] private Transform holdPoint;

        [Header("Basic FPS Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -24f;

        [Header("Room Mechanics")]
        [SerializeField, Range(0.05f, 1f)] private float iceSpeedMultiplier = 0.65f;
        [SerializeField, Min(0.01f)] private float iceAcceleration = 2.25f;
        [SerializeField, Min(1f)] private float hotSpeedMultiplier = 1.15f;
        [SerializeField, Min(1f)] private float hotControlMultiplier = 1.3f;
        [SerializeField, Range(0.05f, 1f)] private float lowGravityMultiplier = 0.45f;
        [SerializeField, Min(1f)] private float lowGravityJumpMultiplier = 1.6f;
        [SerializeField, Range(0.25f, 1f)] private float highGravityCrouchHeightMultiplier = 0.7f;
        [SerializeField, Range(0.1f, 1f)] private float crawlSpeedMultiplier = 0.4f;

        [Header("Look")]
        [SerializeField, Range(0.01f, 5f)] private float mouseLookSensitivity = 0.2f;
        [SerializeField, Range(30f, 89f)] private float verticalLookLimit = 85f;
        [SerializeField, Min(0.01f)] private float gamepadLookSpeed = 140f;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;
        private float yaw;
        private bool cursorLocked;
        private readonly System.Collections.Generic.List<RoomMechanicVolume> activeRoomVolumes = new System.Collections.Generic.List<RoomMechanicVolume>();
        private Vector3 horizontalVelocity;
        private RoomMechanic currentMechanic;
        private RoomMechanicVolume activeRoomVolume;
        private float standingHeight;
        private Vector3 standingCenter;
        private Vector3 standingViewPosition;

        public bool IsGrounded => controller != null && controller.isGrounded;
        public Transform HoldPoint => holdPoint;
        public RoomMechanic CurrentMechanic => currentMechanic;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standingHeight = controller.height;
            standingCenter = controller.center;
            if (view == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                view = childCamera != null ? childCamera.transform : transform;
            }

            yaw = transform.eulerAngles.y;
            standingViewPosition = view != null ? view.localPosition : Vector3.zero;
            CreateHoldPoint();
            LockCursor(true);
        }

        private void Update()
        {
            ResolveViewCamera();
            UpdateCursor();
            if (!cursorLocked || view == null) return;
            UpdateLook();
            UpdateMovement();
        }

        /// <summary>Called by room trigger volumes when this player enters them.</summary>
        public void EnterRoom(RoomMechanicVolume volume)
        {
            if (volume != null && !activeRoomVolumes.Contains(volume)) activeRoomVolumes.Add(volume);
            RefreshRoomMechanic();
        }

        /// <summary>Called by room trigger volumes when this player leaves them.</summary>
        public void ExitRoom(RoomMechanicVolume volume)
        {
            activeRoomVolumes.Remove(volume);
            RefreshRoomMechanic();
        }

        private void RefreshRoomMechanic()
        {
            activeRoomVolumes.RemoveAll(volume => volume == null);
            RoomMechanicVolume selected = null;
            foreach (RoomMechanicVolume volume in activeRoomVolumes)
            {
                if (selected == null || volume.Priority > selected.Priority) selected = volume;
            }
            activeRoomVolume = selected;
            currentMechanic = selected != null ? selected.Mechanic : RoomMechanic.None;
            // Reset the controller immediately when leaving high gravity, even if
            // the cursor is unlocked and UpdateMovement is not currently running.
            if (currentMechanic != RoomMechanic.HighGravity) ApplyHighGravityPosture(false, false);
        }

        private void ResolveViewCamera()
        {
            if (view != null && view != transform) return;
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera == null) return;
            view = childCamera.transform;
            standingViewPosition = view.localPosition;
            if (holdPoint != null && holdPoint.parent != view) holdPoint.SetParent(view, true);
        }

        private void UpdateCursor()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && !cursorLocked) LockCursor(true);
        }

        private void UpdateLook()
        {
            Vector2 mouse = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            Vector2 stick = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
            float controlMultiplier = currentMechanic == RoomMechanic.Hot ? hotControlMultiplier : 1f;
            yaw += (mouse.x * mouseLookSensitivity + stick.x * gamepadLookSpeed * Time.deltaTime) * controlMultiplier;
            pitch -= (mouse.y * mouseLookSensitivity + stick.y * gamepadLookSpeed * Time.deltaTime) * controlMultiplier;
            pitch = Mathf.Clamp(pitch, -verticalLookLimit, verticalLookLimit);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 input = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            if (keyboard != null)
            {
                input = new Vector2(
                    (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f));
            }

            input = Vector2.ClampMagnitude(input, 1f);
            bool crawl = currentMechanic == RoomMechanic.HighGravity && IsCrouchPressed(keyboard);
            ApplyHighGravityPosture(currentMechanic == RoomMechanic.HighGravity, crawl);

            float roomGravityMultiplier = activeRoomVolume != null ? activeRoomVolume.GravityMultiplier : 1f;
            float roomSpeedMultiplier = activeRoomVolume != null ? activeRoomVolume.MovementSpeedMultiplier : 1f;
            float effectiveGravity = gravity * (currentMechanic == RoomMechanic.LowGravity ? lowGravityMultiplier : 1f) * roomGravityMultiplier;
            float effectiveJumpHeight = jumpHeight * (currentMechanic == RoomMechanic.LowGravity ? lowGravityJumpMultiplier : 1f);
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            bool jump = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (controller.isGrounded && jump && !crawl) verticalVelocity = Mathf.Sqrt(effectiveJumpHeight * -2f * effectiveGravity);
            verticalVelocity += effectiveGravity * Time.deltaTime;

            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            float speedMultiplier = currentMechanic == RoomMechanic.Ice ? iceSpeedMultiplier : currentMechanic == RoomMechanic.Hot ? hotSpeedMultiplier : 1f;
            if (crawl) speedMultiplier *= crawlSpeedMultiplier;
            Vector3 targetVelocity = direction * moveSpeed * speedMultiplier * roomSpeedMultiplier;
            if (currentMechanic == RoomMechanic.Ice)
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, iceAcceleration * Time.deltaTime);
            else
                horizontalVelocity = targetVelocity;
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private bool IsCrouchPressed(Keyboard keyboard)
        {
            return (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed)) ||
                   (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        }

        private void ApplyHighGravityPosture(bool highGravity, bool crawling)
        {
            float targetHeight = highGravity ? standingHeight * (crawling ? highGravityCrouchHeightMultiplier * 0.62f : highGravityCrouchHeightMultiplier) : standingHeight;
            float heightDifference = targetHeight - standingHeight;
            controller.height = targetHeight;
            controller.center = standingCenter + Vector3.up * (heightDifference * 0.5f);
            if (view != null) view.localPosition = standingViewPosition + Vector3.up * heightDifference;
        }

        private void CreateHoldPoint()
        {
            if (holdPoint != null) return;
            holdPoint = new GameObject("Hold Point").transform;
            holdPoint.SetParent(view != null ? view : transform, false);
            holdPoint.localPosition = new Vector3(0.32f, -0.28f, 1f);
        }

        private void LockCursor(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && cursorLocked) LockCursor(true);
        }
    }
}
