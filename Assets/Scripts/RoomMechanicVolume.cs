using UnityEngine;

namespace FPSStarter
{
    public enum RoomMechanic
    {
        None,
        Ice,
        Hot,
        LowGravity,
        HighGravity
    }

    /// <summary>
    /// Put this component on a trigger collider that fills a room. The player
    /// automatically uses the selected mechanic while inside the trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomMechanicVolume : MonoBehaviour
    {
        [SerializeField] private RoomMechanic mechanic;
        [SerializeField] private int priority;

        [Header("Movement Overrides")]
        [Tooltip("Multiplies the player's gravity while this is the active room. Values below 1 make gravity weaker; values above 1 make it stronger.")]
        [SerializeField, Min(0.01f)] private float gravityMultiplier = 1f;
        [Tooltip("Multiplies the player's movement speed while this is the active room.")]
        [SerializeField, Min(0f)] private float movementSpeedMultiplier = 1f;

        public RoomMechanic Mechanic => mechanic;
        public int Priority => priority;
        public float GravityMultiplier => gravityMultiplier;
        public float MovementSpeedMultiplier => movementSpeedMultiplier;

        public void Configure(RoomMechanic roomMechanic, int roomPriority = 0, float gravity = 1f, float moveSpeed = 1f)
        {
            mechanic = roomMechanic;
            priority = roomPriority;
            gravityMultiplier = gravity;
            movementSpeedMultiplier = moveSpeed;
        }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnValidate()
        {
            Collider roomCollider = GetComponent<Collider>();
            if (roomCollider != null) roomCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null) player.EnterRoom(this);
        }

        private void OnTriggerExit(Collider other)
        {
            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null) player.ExitRoom(this);
        }
    }
}
