using UnityEngine;

namespace FPSStarter
{
    /// <summary>
    /// Makes the Hot Room floor lethal after a short continuous time on it.
    /// Leaving the ground, even briefly, resets the timer.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController), typeof(CharacterController))]
    public sealed class GroundHazardRespawn : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float maximumGroundTime = 3f;

        private FirstPersonController player;
        private RoomMechanicVolume iceRoom;
        private float groundTime;
        private float respawnMessageUntil;

        private void Awake()
        {
            player = GetComponent<FirstPersonController>();
        }

        private void Update()
        {
            if (player == null || !player.isActiveAndEnabled) return;
            if (iceRoom == null) iceRoom = FindIceRoom();

            if (player.CurrentMechanic != RoomMechanic.Hot || !player.IsGrounded)
            {
                groundTime = 0f;
                return;
            }

            groundTime += Time.deltaTime;
            if (groundTime >= maximumGroundTime) RespawnInIceRoom();
        }

        private void RespawnInIceRoom()
        {
            groundTime = 0f;
            if (iceRoom == null) return;

            player.RespawnAt(GetRoomFloorSpawnPosition(iceRoom), Quaternion.Euler(0f, -90f, 0f), iceRoom);
            Physics.SyncTransforms();
            respawnMessageUntil = Time.time + 1.5f;
        }

        private static RoomMechanicVolume FindIceRoom()
        {
            foreach (RoomMechanicVolume room in FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None))
            {
                if (room.Mechanic == RoomMechanic.Ice) return room;
            }
            return null;
        }

        private static Vector3 GetRoomFloorSpawnPosition(RoomMechanicVolume room)
        {
            Collider roomCollider = room.GetComponent<Collider>();
            if (roomCollider == null) return room.transform.position + Vector3.up * 1.9f;

            Bounds bounds = roomCollider.bounds;
            // Start just above the room's floor instead of casting down from
            // above the building. The latter can select the roof as the first
            // valid upward-facing collider and respawn the player on it.
            return new Vector3(bounds.center.x, bounds.min.y - 0.05f, bounds.center.z);
        }

        private void OnGUI()
        {
            if (player == null || !player.isActiveAndEnabled) return;

            if (Time.time < respawnMessageUntil)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 130f, 52f, 260f, 28f), "The lava got you! Respawned in the Ice Room.");
                return;
            }

            if (player.CurrentMechanic != RoomMechanic.Hot) return;

            float remaining = Mathf.Max(0f, maximumGroundTime - groundTime);
            GUI.Label(new Rect(Screen.width * 0.5f - 110f, 52f, 220f, 28f), "Lava! Jump within " + remaining.ToString("0.0") + "s");
        }
    }
}
