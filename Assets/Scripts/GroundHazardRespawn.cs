using UnityEngine;

namespace FPSStarter
{
    /// <summary>Respawns a player in the Ice Room after five seconds on the ground.</summary>
    [RequireComponent(typeof(FirstPersonController), typeof(CharacterController))]
    public sealed class GroundHazardRespawn : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float maximumGroundTime = 5f;

        private FirstPersonController player;
        private RoomMechanicVolume frozenRoom;
        private float groundTime;
        private float respawnMessageUntil;

        private void Awake()
        {
            player = GetComponent<FirstPersonController>();
        }

        private void Update()
        {
            if (player == null || !player.isActiveAndEnabled) return;
            if (frozenRoom == null) frozenRoom = FindFrozenRoom();

            if (!player.IsGrounded)
            {
                groundTime = 0f;
                return;
            }

            groundTime += Time.deltaTime;
            if (groundTime >= maximumGroundTime) RespawnInFrozenRoom();
        }

        private void RespawnInFrozenRoom()
        {
            groundTime = 0f;
            if (frozenRoom == null) return;

            player.RespawnAt(GetSpawnPosition(frozenRoom), Quaternion.Euler(0f, -90f, 0f), frozenRoom);
            Physics.SyncTransforms();
            respawnMessageUntil = Time.time + 1.5f;
        }

        private static RoomMechanicVolume FindFrozenRoom()
        {
            foreach (RoomMechanicVolume room in FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None))
            {
                if (room.Mechanic == RoomMechanic.Ice) return room;
            }
            return null;
        }

        private static Vector3 GetSpawnPosition(RoomMechanicVolume room)
        {
            Collider roomCollider = room.GetComponent<Collider>();
            if (roomCollider == null) return room.transform.position + Vector3.up * 1.9f;

            Bounds bounds = roomCollider.bounds;
            Vector3 rayOrigin = new Vector3(bounds.center.x, bounds.max.y + 3f, bounds.center.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, bounds.size.y + 8f, ~0, QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            Vector3 ground = Vector3.zero;
            bool foundGround = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.normal.y < 0.6f || hit.distance >= closestDistance) continue;
                closestDistance = hit.distance;
                ground = hit.point;
                foundGround = true;
            }

            return foundGround
                ? ground + Vector3.up * 0.95f
                : new Vector3(bounds.center.x, Mathf.Max(1.9f, bounds.center.y), bounds.center.z);
        }

        private void OnGUI()
        {
            if (player == null || !player.isActiveAndEnabled) return;

            if (Time.time < respawnMessageUntil)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 130f, 52f, 260f, 28f), "You froze! Respawned in the Frozen Room.");
                return;
            }

            float remaining = Mathf.Max(0f, maximumGroundTime - groundTime);
            GUI.Label(new Rect(Screen.width * 0.5f - 100f, 52f, 200f, 28f), "Jump! Ground freezes in " + remaining.ToString("0.0") + "s");
        }
    }
}
