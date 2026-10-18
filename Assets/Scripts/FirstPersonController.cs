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
        [SerializeField, Min(0.01f)] private float iceAcceleration = 7f;
        [SerializeField, Min(0.01f)] private float iceDeceleration = 9f;
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
        private Vector3 targetHorizontalVelocity;
        private RoomMechanic currentMechanic;
        private RoomMechanicVolume activeRoomVolume;
        private float standingHeight;
        private Vector3 standingCenter;
        private Vector3 standingViewPosition;

        public bool IsGrounded => controller != null && controller.isGrounded;
        public float NormalMoveSpeed => moveSpeed;
        public Transform HoldPoint => holdPoint;
        public RoomMechanic CurrentMechanic => currentMechanic;
        public RoomMechanicVolume CurrentRoom => activeRoomVolume;

        /// <summary>Applies the Stage 2 movement tuning after the scene has loaded.</summary>
        public void ConfigureStage2Movement(float iceSpeed, float iceAccelerationRate, float iceDecelerationRate, float hotSpeed)
        {
            iceSpeedMultiplier = Mathf.Clamp(iceSpeed, 0.05f, 1f);
            iceAcceleration = Mathf.Max(0.01f, iceAccelerationRate);
            iceDeceleration = Mathf.Max(0.01f, iceDecelerationRate);
            hotSpeedMultiplier = Mathf.Max(1f, hotSpeed);
        }

        public void RespawnAt(Vector3 position, Quaternion rotation, RoomMechanicVolume room)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller == null) return;

            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            yaw = rotation.eulerAngles.y;
            pitch = 0f;
            if (view != null && view != transform) view.localRotation = Quaternion.identity;
            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
            targetHorizontalVelocity = Vector3.zero;
            activeRoomVolumes.Clear();
            activeRoomVolume = null;
            currentMechanic = RoomMechanic.None;
            controller.enabled = true;

            if (room != null) EnterRoom(room);
            else ApplyPosture(false, false);
        }

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

            // Movement must not depend on a camera reference. This keeps WASD
            // working even if the camera is assigned a frame later at startup.
            if (cursorLocked && view != null) UpdateLook();
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
            if (currentMechanic != RoomMechanic.HighGravity) ApplyPosture(false, false);
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
            Vector2 input = ReadMovementInput(keyboard);
            input = Vector2.ClampMagnitude(input, 1f);
            bool crouching = IsCrouchPressed(keyboard);
            ApplyPosture(currentMechanic == RoomMechanic.HighGravity, crouching);

            float roomGravityMultiplier = activeRoomVolume != null ? activeRoomVolume.GravityMultiplier : 1f;
            float roomSpeedMultiplier = activeRoomVolume != null ? activeRoomVolume.MovementSpeedMultiplier : 1f;
            float effectiveGravity = gravity * (currentMechanic == RoomMechanic.LowGravity ? lowGravityMultiplier : 1f) * roomGravityMultiplier;
            float effectiveJumpHeight = jumpHeight * (currentMechanic == RoomMechanic.LowGravity ? lowGravityJumpMultiplier : 1f);
            bool grounded = CheckGrounded();
            bool standingOnBox = currentMechanic == RoomMechanic.Ice && verticalVelocity <= 0f && IsStandingOnBox();
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
            bool jump = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (grounded && jump && !crouching) verticalVelocity = Mathf.Sqrt(effectiveJumpHeight * -2f * effectiveGravity);
            verticalVelocity += effectiveGravity * Time.deltaTime;

            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0f;
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            float speedMultiplier = currentMechanic == RoomMechanic.Ice ? iceSpeedMultiplier : currentMechanic == RoomMechanic.Hot ? hotSpeedMultiplier : 1f;
            if (crouching) speedMultiplier *= crawlSpeedMultiplier;
            Vector3 targetVelocity = direction * moveSpeed * speedMultiplier * roomSpeedMultiplier;
            targetHorizontalVelocity = targetVelocity;
            if (currentMechanic == RoomMechanic.Ice && !standingOnBox)
            {
                // Ice keeps its lower top speed, but now responds promptly when
                // the player starts moving or releases every movement control.
                float responseRate = input.sqrMagnitude > 0f ? iceAcceleration : iceDeceleration;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, responseRate * Time.deltaTime);
            }
            else
                horizontalVelocity = targetVelocity;
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private static Vector2 ReadMovementInput(Keyboard keyboard)
        {
            Vector2 keyboardInput = Vector2.zero;
            if (keyboard != null)
            {
                keyboardInput.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                                  (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                keyboardInput.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                                  (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            // Prefer keyboard when either WASD/arrow key is held, while still
            // allowing a gamepad to work when the keyboard is idle.
            if (keyboardInput.sqrMagnitude > 0f) return keyboardInput;
            return Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        }

        private bool CheckGrounded()
        {
            if (controller.isGrounded) return true;
            float radius = Mathf.Max(0.08f, controller.radius * 0.9f);
            Vector3 origin = transform.TransformPoint(controller.center) + Vector3.down * (controller.height * 0.5f - radius);
            return Physics.SphereCast(origin, radius, Vector3.down, out _, controller.skinWidth + 0.12f, ~0, QueryTriggerInteraction.Ignore);
        }

        private bool IsStandingOnBox()
        {
            float radius = Mathf.Max(0.08f, controller.radius * 0.9f);
            Vector3 origin = transform.TransformPoint(controller.center) + Vector3.down * (controller.height * 0.5f - radius);
            float distance = radius + controller.skinWidth + 0.12f;
            // The ray also works when the bottom sphere already overlaps the
            // supporting surface, which prevents flickering traction on landing.
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore) &&
                hit.normal.y > 0.5f && IsBoxSurface(hit.collider)) return true;

            return Physics.SphereCast(origin, radius, Vector3.down, out hit, controller.skinWidth + 0.12f, ~0, QueryTriggerInteraction.Ignore) &&
                   hit.normal.y > 0.5f && IsBoxSurface(hit.collider);
        }

        private static bool IsBoxSurface(Collider surface)
        {
            // Imported boxes can use MeshColliders. Floor slabs can use
            // BoxColliders, so collider type alone does not identify a box.
            string name = surface.name.ToLowerInvariant();
            if (name.Contains("floor") || name.Contains("ground") || name.StartsWith("plane") || name.Contains("respawn"))
                return false;
            bool boxName = name.StartsWith("cube") || name.Contains("box") || name.Contains("crate") || name.Contains("block");
            Vector3 size = surface.bounds.size;
            return boxName && size.y > 0.2f && size.x <= 6f && size.z <= 6f;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (currentMechanic == RoomMechanic.Ice && verticalVelocity <= 0f && hit.normal.y > 0.5f && IsBoxSurface(hit.collider))
                horizontalVelocity = targetHorizontalVelocity;
        }

        private bool IsCrouchPressed(Keyboard keyboard)
        {
            return (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)) ||
                   (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        }

        private void ApplyPosture(bool highGravity, bool crouching)
        {
            float targetHeight = highGravity ? standingHeight * highGravityCrouchHeightMultiplier : standingHeight;
            if (crouching) targetHeight *= highGravity ? 0.62f : highGravityCrouchHeightMultiplier;
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
