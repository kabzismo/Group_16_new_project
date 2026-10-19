using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FPSStarter
{
    /// <summary>The High Gravity room pursuer with navigation built from the loaded room.</summary>
    public sealed class HighGravityEnemy : MonoBehaviour
    {
        [Tooltip("The High Gravity room the enemy patrols and chases inside.")]
        [SerializeField] private RoomMechanicVolume room;
        [Tooltip("Saved scene character, visible and positionable before Play mode.")]
        [SerializeField] private Transform enemyActor;
        [Tooltip("Fraction of the player's normal, uncrouched walking speed.")]
        [SerializeField, Range(0.01f, 1f)] private float walkSpeedMultiplier = 0.35f;
        [SerializeField, Min(0.1f)] private float catchDistance = 0.8f;
        [SerializeField] private Color bodyColor = new Color(0.24f, 0.08f, 0.11f);
        [SerializeField] private Color eyeColor = new Color(1f, 0.18f, 0.06f);

        private const float Radius = 0.32f;
        private const float Height = 1.15f;
        private FirstPersonController player;
        private CharacterController playerBody;
        private GroundHazardRespawn respawn;
        private NavMeshAgent agent;
        private NavMeshData navMesh;
        private NavMeshDataInstance navMeshInstance;
        private Bounds roomBounds;
        private Vector3 home;
        private float nextPathUpdate;
        private float catchCooldownUntil;
        private bool chasing;
        private bool createdActor;

        public bool IsReady => agent != null && agent.isOnNavMesh;
        public bool IsChasing => chasing;
        public float WalkSpeed => player != null ? player.NormalMoveSpeed * walkSpeedMultiplier : 0f;
        public Transform EnemyTransform => agent != null ? agent.transform : enemyActor;

        public void Configure(RoomMechanicVolume volume) => room = volume;

        private void Start()
        {
            player = FindFirstObjectByType<FirstPersonController>();
            // Correct scenes that still assign the enemy to the Low Gravity volume.
            if (room == null || room.Mechanic != RoomMechanic.HighGravity)
                foreach (RoomMechanicVolume volume in FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None))
                    if (volume.Mechanic == RoomMechanic.HighGravity) { room = volume; break; }
            if (player == null || room == null || room.Mechanic != RoomMechanic.HighGravity) { enabled = false; return; }
            playerBody = player.GetComponent<CharacterController>();
            respawn = player.GetComponent<GroundHazardRespawn>();
            if (respawn == null) respawn = player.gameObject.AddComponent<GroundHazardRespawn>();
            Collider volumeCollider = room.GetComponent<Collider>();
            if (volumeCollider == null) { enabled = false; return; }
            Physics.SyncTransforms();
            roomBounds = volumeCollider.bounds;
            if (!BuildNavigation() || !FindHome(out home))
            {
                Debug.LogError("High Gravity enemy could not find a walkable floor. Check the room's floor colliders and volume.", this);
                enabled = false;
                return;
            }
            CreateEnemy();
        }

        private bool BuildNavigation()
        {
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(roomBounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<NavMeshBuildMarkup>(), sources);
            sources.RemoveAll(source => source.component != null &&
                (source.component.transform.IsChildOf(player.transform) ||
                 (enemyActor != null && source.component.transform.IsChildOf(enemyActor)) ||
                 source.component.GetComponentInParent<CarryableObject>() != null ||
                 source.component is Collider collider && collider.isTrigger));
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = Radius;
            settings.agentHeight = Height;
            settings.agentClimb = 0.3f;
            settings.agentSlope = 45f;
            settings.minRegionArea = 0.1f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.08f;
            navMesh = NavMeshBuilder.BuildNavMeshData(settings, sources, roomBounds, Vector3.zero, Quaternion.identity);
            if (navMesh == null) return false;
            navMesh.name = "High Gravity Room Runtime Navigation";
            navMeshInstance = NavMesh.AddNavMeshData(navMesh);
            return navMeshInstance.valid;
        }

        private bool FindHome(out Vector3 position)
        {
            // Respect the saved character's position whenever it is walkable.
            // This lets the designer place it in the Scene view before Play.
            if (enemyActor != null && NavMesh.SamplePosition(enemyActor.position, out NavMeshHit authoredHome, 1.5f, NavMesh.AllAreas) &&
                InsideRoom(authoredHome.position))
            {
                position = authoredHome.position;
                return true;
            }

            // Search the room's floor, not the roof or the top of its trigger.
            // Keep the enemy away from the player's approach when it first spawns.
            position = default;
            float bestDistance = -1f;
            for (int x = 1; x <= 7; x++)
                for (int z = 1; z <= 7; z++)
                {
                    Vector3 origin = new Vector3(Mathf.Lerp(roomBounds.min.x, roomBounds.max.x, x / 8f),
                        roomBounds.center.y, Mathf.Lerp(roomBounds.min.z, roomBounds.max.z, z / 8f));
                    foreach (RaycastHit floor in Physics.RaycastAll(origin, Vector3.down, roomBounds.size.y + 4f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (floor.normal.y < 0.7f || floor.collider.transform.IsChildOf(player.transform) || IsRoof(floor.collider.transform)) continue;
                        if (!NavMesh.SamplePosition(floor.point, out NavMeshHit sample, 0.6f, NavMesh.AllAreas) || !InsideRoom(sample.position)) continue;
                        float distance = (sample.position - player.transform.position).sqrMagnitude;
                        if (distance <= bestDistance) continue;
                        position = sample.position;
                        bestDistance = distance;
                    }
                }
            return bestDistance >= 0f;
        }

        private void CreateEnemy()
        {
            // Use a world-space root: room trigger transforms are heavily scaled.
            GameObject actor = enemyActor != null ? enemyActor.gameObject : new GameObject("High Gravity Chaser");
            createdActor = enemyActor == null;
            enemyActor = actor.transform;
            actor.SetActive(true);
            actor.transform.position = home;
            agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null) agent = actor.AddComponent<NavMeshAgent>();
            agent.radius = Radius;
            agent.height = Height;
            agent.baseOffset = 0f;
            agent.speed = WalkSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.05f;
            agent.autoTraverseOffMeshLink = false;
            agent.Warp(home);
            if (!createdActor) return;
            MakePart(actor.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.55f, 0f), new Vector3(0.64f, 0.55f, 0.64f), bodyColor);
            MakePart(actor.transform, "Left Eye", PrimitiveType.Sphere, new Vector3(-0.12f, 0.83f, 0.285f), Vector3.one * 0.11f, eyeColor);
            MakePart(actor.transform, "Right Eye", PrimitiveType.Sphere, new Vector3(0.12f, 0.83f, 0.285f), Vector3.one * 0.11f, eyeColor);
            MakePart(actor.transform, "Shoulder Band", PrimitiveType.Cube, new Vector3(0f, 0.57f, 0f), new Vector3(0.7f, 0.1f, 0.48f), eyeColor);
        }

        private static void MakePart(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            VisualColor.Set(part.GetComponent<Renderer>(), color);
        }

        private void Update()
        {
            if (!IsReady || player == null) return;
            agent.speed = WalkSpeed;
            bool shouldChase = player.isActiveAndEnabled && player.CurrentMechanic == RoomMechanic.HighGravity &&
                player.CurrentRoom == room && InsideRoom(player.transform.position) && Time.time >= catchCooldownUntil;
            if (!shouldChase)
            {
                if (chasing) ResetEnemy();
                return;
            }
            chasing = true;
            agent.isStopped = false;
            if (!InsideRoom(agent.transform.position)) { ResetEnemy(); return; }
            if (Time.time >= nextPathUpdate)
            {
                nextPathUpdate = Time.time + 0.2f;
                if (TryFindPlayerFloor(out Vector3 target)) agent.SetDestination(target);
            }
            if (CanCatchPlayer() && respawn.TryRespawnAtDoor("The enemy caught you. Try again!"))
            {
                catchCooldownUntil = Time.time + 2f;
                ResetEnemy();
            }
        }

        private bool TryFindPlayerFloor(out Vector3 position)
        {
            if (NavMesh.SamplePosition(player.transform.position, out NavMeshHit nearby, 1.5f, NavMesh.AllAreas) && InsideRoom(nearby.position))
            {
                position = nearby.position;
                return true;
            }

            // Jumping onto obstacles can put the player above the NavMesh.
            // Keep following their floor position, but only catch at body height.
            position = default;
            float highestFloor = float.NegativeInfinity;
            foreach (RaycastHit floor in Physics.RaycastAll(playerBody.bounds.center, Vector3.down, roomBounds.size.y, ~0, QueryTriggerInteraction.Ignore))
            {
                if (floor.normal.y < 0.7f || floor.collider.transform.IsChildOf(player.transform) ||
                    floor.collider.transform.IsChildOf(enemyActor) || IsRoof(floor.collider.transform)) continue;
                if (floor.point.y <= highestFloor || !NavMesh.SamplePosition(floor.point, out NavMeshHit sample, 0.6f, NavMesh.AllAreas) ||
                    !InsideRoom(sample.position)) continue;
                highestFloor = floor.point.y;
                position = sample.position;
            }
            return highestFloor > float.NegativeInfinity;
        }

        private bool CanCatchPlayer()
        {
            Vector3 delta = player.transform.position - agent.transform.position;
            if (new Vector2(delta.x, delta.z).sqrMagnitude > catchDistance * catchDistance) return false;
            Bounds body = playerBody.bounds;
            if (body.min.y > agent.transform.position.y + Height || body.max.y < agent.transform.position.y) return false;
            Vector3 from = agent.transform.position + Vector3.up * (Height * 0.5f);
            // Close range is not enough: a wall must still protect the player.
            if (Physics.Linecast(from, body.center, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.transform.IsChildOf(player.transform);
            return true;
        }

        private bool InsideRoom(Vector3 point)
        {
            return roomBounds.Contains(point);
        }

        private static bool IsRoof(Transform surface)
        {
            for (Transform current = surface; current != null; current = current.parent)
            {
                string name = current.name.ToLowerInvariant();
                if (name.Contains("roof") || name.Contains("ceiling")) return true;
            }
            return false;
        }

        private void ResetEnemy()
        {
            agent.ResetPath();
            agent.Warp(home);
            agent.isStopped = true;
            chasing = false;
            nextPathUpdate = 0f;
        }

        private void OnDisable()
        {
            if (IsReady) { agent.ResetPath(); agent.isStopped = true; }
            chasing = false;
        }

        private void OnDestroy()
        {
            if (agent != null)
            {
                if (createdActor) Destroy(agent.gameObject);
                else Destroy(agent);
            }
            if (navMeshInstance.valid) navMeshInstance.Remove();
            if (navMesh != null) Destroy(navMesh);
        }
    }
}
