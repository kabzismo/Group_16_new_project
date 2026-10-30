using UnityEngine;

namespace FPSStarter
{
    public sealed class GravityFloorHazard : MonoBehaviour
    {
        public RoomMechanicVolume room;
        private float cooldown;
        private FirstPersonController player;
        private Collider field;
        void Awake()
        {
            player = FindFirstObjectByType<FirstPersonController>();
            field = GetComponent<Collider>();
        }
        void Update()
        {
            // CharacterController trigger-stay messages are not reliable when
            // a platform collider disappears beneath a stationary player.
            if (Hint.IsReading || room == null || !room.EntranceHintCompleted || Time.time < cooldown) return;
            if (player == null || player.CurrentRoom != room) return;
            var body = player.GetComponent<CharacterController>();
            if (body == null || !body.enabled || field == null || !field.bounds.Intersects(body.bounds)) return;
            var respawn = player.GetComponent<GroundHazardRespawn>();
            if (respawn != null && respawn.TryRespawnAtDoor("The gravity field caught you. Stay on the platforms!"))
                cooldown = Time.time + 1f;
        }
    }
}
