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
            // Begin inside the room rather than above it: a ray from above can
            // hit the roof and a spawn at the trigger's lower edge can be below
            // the actual floor. The highest valid surface below the room centre
            // is the walkable floor.
            Vector3 origin = bounds.center;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                bounds.extents.y + 12f,
                ~0,
                QueryTriggerInteraction.Ignore);

            RaycastHit floorHit = default;
            bool foundFloor = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.normal.y < 0.6f || hit.point.y >= origin.y - 0.02f || IsRoof(hit.collider)) continue;
                if (!foundFloor || hit.point.y > floorHit.point.y)
                {
                    floorHit = hit;
                    foundFloor = true;
                }
            }

            if (foundFloor) return floorHit.point + Vector3.up * 0.08f;

            // Use the global safety floor only when it covers the Ice Room.
            GameObject safetyFloor = GameObject.Find("Stage 2 Safety Floor");
            Collider safetyCollider = safetyFloor != null ? safetyFloor.GetComponent<Collider>() : null;
            if (safetyCollider != null && Covers(safetyCollider.bounds, bounds.center))
                return new Vector3(bounds.center.x, safetyCollider.bounds.max.y + 0.08f, bounds.center.z);

            // Some imported rooms have no usable floor collider. Give the Ice
            // Room its own tiny, invisible fallback floor at its room level so
            // a respawn remains playable instead of falling through the world.
            float floorY = room.transform.parent != null
                ? room.transform.parent.position.y + 0.15f
                : bounds.min.y + 0.15f;
            return GetOrCreateRespawnPlatform(bounds.center, floorY);
        }

        private static bool Covers(Bounds floor, Vector3 point)
        {
            return point.x >= floor.min.x && point.x <= floor.max.x &&
                   point.z >= floor.min.z && point.z <= floor.max.z;
        }

        private static Vector3 GetOrCreateRespawnPlatform(Vector3 roomCenter, float floorY)
        {
            const string platformName = "Ice Room Respawn Platform";
            GameObject platform = GameObject.Find(platformName);
            if (platform == null)
            {
                platform = new GameObject(platformName);
                platform.transform.position = new Vector3(roomCenter.x, floorY - 0.05f, roomCenter.z);
                BoxCollider collider = platform.AddComponent<BoxCollider>();
                collider.size = new Vector3(4f, 0.1f, 4f);
            }

            Collider platformCollider = platform.GetComponent<Collider>();
            return new Vector3(roomCenter.x, platformCollider.bounds.max.y + 0.08f, roomCenter.z);
        }

        private static bool IsRoof(Collider collider)
        {
            for (Transform current = collider != null ? collider.transform : null; current != null; current = current.parent)
            {
                string name = current.name.ToLowerInvariant();
                if (name.Contains("roof") || name.Contains("ceiling")) return true;
            }
            return false;
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
