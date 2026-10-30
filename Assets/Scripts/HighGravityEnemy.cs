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
        [Tooltip("Fixed checkpoint beside Door .002 for enemy catches.")]
        [SerializeField] private Transform catchRespawnPoint;
        public void ConfigureCatchCheckpoint(Transform checkpoint) => catchRespawnPoint = checkpoint;
        [Tooltip("Fraction of the player's normal, uncrouched walking speed.")]
        [SerializeField, Range(0.01f, 1f)] private float walkSpeedMultiplier = 0.35f;
        [SerializeField, Min(0.1f)] private float catchDistance = 0.8f;
        [SerializeField] private Color bodyColor = new Color(0.24f, 0.08f, 0.11f);
        [SerializeField] private Color eyeColor = new Color(1f, 0.18f, 0.06f);

        // Include the shoulder width and a little clearance, not just the body.
        private const float Radius = 0.42f;
        private const float Height = 1.15f;
        private const float StepHeight = 0.25f;
        private const int WalkableMask = 1;
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
        private NavMeshQueryFilter navigationFilter;
        private NavMeshPath chasePath;
        private NavMeshPath candidatePath;
        private NavMeshPath recoveryPath;
        private readonly Collider[] clearanceHits = new Collider[32];
        private float floorHeight;
        private float navigationRadius = Radius;
        private float navigationHeight = Height;
        private float nextProgressCheck;
        private float recoveryUntil;
        private Vector3 progressPosition;
        private Vector3 chaseTarget;
        private int stalledChecks;
        private int recoveryCount;
        private int navigationSourceCount;
        private bool hasReachableTarget;

        public bool IsReady => agent != null && agent.isOnNavMesh;
        public bool IsChasing => chasing;
        public float WalkSpeed => player != null ? player.NormalMoveSpeed * walkSpeedMultiplier : 0f;
        public Transform EnemyTransform => agent != null ? agent.transform : enemyActor;
        public bool HasReachableTarget => hasReachableTarget;
        public Vector3 ChaseTarget => chaseTarget;
        public float NavigationFloorHeight => floorHeight;
        public int NavigationSourceCount => navigationSourceCount;
        public int RecoveryCount => recoveryCount;
        public NavMeshPathStatus PathStatus => IsReady && agent.hasPath ? agent.pathStatus : NavMeshPathStatus.PathInvalid;

        public void Configure(RoomMechanicVolume volume) => room = volume;

        private void Start()
        {
            // Native navigation objects must be created on Unity's main thread,
            // not while the scene loader constructs this MonoBehaviour.
            chasePath = new NavMeshPath();
            candidatePath = new NavMeshPath();
            recoveryPath = new NavMeshPath();
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
            MeasureActorClearance();
            if (!FindFloorHeight(out floorHeight) || !BuildNavigation() || !FindHome(out home))
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
            for (int i = 0; i < sources.Count; i++)
            {
                NavMeshBuildSource source = sources[i];
                // Boxes and maze walls still obstruct the bake, but their tops
                // are not extra walkable islands for this ground-based enemy.
                if (source.component is Collider obstacle && obstacle.bounds.max.y > floorHeight + StepHeight + 0.05f)
                    source.area = 1; // Unity's built-in Not Walkable area.
                sources[i] = source;
            }
            navigationSourceCount = sources.Count;
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = navigationRadius;
            settings.agentHeight = navigationHeight;
            settings.agentClimb = StepHeight;
            settings.agentSlope = 45f;
            settings.minRegionArea = 0.1f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.07f;
            navigationFilter = new NavMeshQueryFilter { agentTypeID = settings.agentTypeID, areaMask = WalkableMask };
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
            if (enemyActor != null && TrySampleFloor(enemyActor.position, 1f, out Vector3 authoredHome) &&
                HasPhysicalClearance(authoredHome) && HasRoomFloorConnection(authoredHome))
            {
                position = authoredHome;
                return true;
            }

            // Search the room's floor, not the roof or the top of its trigger.
            // Keep the enemy away from the player's approach when it first spawns.
            position = default;
            float bestDistance = -1f;
            for (int x = 1; x <= 7; x++)
                for (int z = 1; z <= 7; z++)
                {
                    Vector3 samplePoint = new Vector3(Mathf.Lerp(roomBounds.min.x, roomBounds.max.x, x / 8f),
                        floorHeight, Mathf.Lerp(roomBounds.min.z, roomBounds.max.z, z / 8f));
                    if (!TrySampleFloor(samplePoint, 0.7f, out Vector3 sample) || !HasPhysicalClearance(sample)) continue;
                    // Prefer a real floor point connected to the centre of the
                    // room, rather than a decorative ledge high above it.
                    if (!HasRoomFloorConnection(sample)) continue;
                    Vector3 delta = sample - player.transform.position;
                    float distance = delta.x * delta.x + delta.z * delta.z;
                    if (distance <= bestDistance) continue;
                    position = sample;
                    bestDistance = distance;
                }
            return bestDistance >= 0f;
        }

        private void MeasureActorClearance()
        {
            if (enemyActor == null) return;
            foreach (Renderer renderer in enemyActor.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;
                Vector3 centre = bounds.center - enemyActor.position;
                navigationRadius = Mathf.Max(navigationRadius, Mathf.Max(Mathf.Abs(centre.x) + bounds.extents.x,
                    Mathf.Abs(centre.z) + bounds.extents.z) + 0.07f);
                navigationHeight = Mathf.Max(navigationHeight, bounds.max.y - enemyActor.position.y + 0.05f);
            }
        }

        private bool FindFloorHeight(out float height)
        {
            height = float.PositiveInfinity;
            for (int x = 1; x <= 7; x++)
                for (int z = 1; z <= 7; z++)
                {
                    Vector3 origin = new Vector3(Mathf.Lerp(roomBounds.min.x, roomBounds.max.x, x / 8f),
                        roomBounds.max.y - 0.05f, Mathf.Lerp(roomBounds.min.z, roomBounds.max.z, z / 8f));
                    foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, roomBounds.size.y, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (IgnoredCollider(hit.collider) || hit.normal.y < 0.7f || IsRoof(hit.collider.transform) ||
                            hit.point.y < roomBounds.min.y + 0.02f) continue;
                        height = Mathf.Min(height, hit.point.y);
                    }
                }
            return !float.IsPositiveInfinity(height);
        }

        private bool TrySampleFloor(Vector3 point, float distance, out Vector3 position)
        {
            point.y = floorHeight + 0.05f;
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, distance, navigationFilter) &&
                InsideRoom(hit.position) && Mathf.Abs(hit.position.y - floorHeight) <= StepHeight + 0.12f)
            {
                position = hit.position;
                return true;
            }
            position = default;
            return false;
        }

        private bool HasRoomFloorConnection(Vector3 point)
        {
            // A usable home must reach more than its own tiny disconnected tile.
            int reachable = 0;
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 other = new Vector3(roomBounds.center.x + x * roomBounds.size.x * 0.25f, floorHeight,
                        roomBounds.center.z + z * roomBounds.size.z * 0.25f);
                    if (TrySampleFloor(other, 1.5f, out Vector3 sample) &&
                        NavMesh.CalculatePath(point, sample, navigationFilter, candidatePath) && IsCompleteRoomPath(candidatePath) &&
                        (sample - point).sqrMagnitude > 4f && ++reachable >= 2) return true;
                }
            return false;
        }

        private bool HasPhysicalClearance(Vector3 position)
        {
            float radius = Mathf.Max(0.05f, navigationRadius - 0.02f);
            Vector3 bottom = position + Vector3.up * (radius + 0.04f);
            Vector3 top = position + Vector3.up * Mathf.Max(radius + 0.04f, navigationHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, clearanceHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!IgnoredCollider(clearanceHits[i])) return false;
            return count < clearanceHits.Length;
        }

        private bool IgnoredCollider(Collider collider)
        {
            return collider == null || collider.isTrigger || collider.transform.IsChildOf(player.transform) ||
                (enemyActor != null && collider.transform.IsChildOf(enemyActor)) ||
                collider.GetComponentInParent<CarryableObject>() != null;
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
            agent.enabled = false;
            agent.agentTypeID = navigationFilter.agentTypeID;
            agent.areaMask = WalkableMask;
            agent.radius = navigationRadius;
            agent.height = navigationHeight;
            agent.baseOffset = 0f;
            agent.speed = WalkSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.05f;
            agent.autoRepath = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoTraverseOffMeshLink = false;
            agent.enabled = true;
            if (!agent.Warp(home)) { enabled = false; return; }
            agent.ResetPath();
            agent.isStopped = true;
            progressPosition = home;
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
            if (Hint.IsReading || !room.EntranceHintCompleted)
            {
                agent.isStopped = true;
                chasing = false;
                return;
            }
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
            if (!InsideRoom(agent.transform.position)) { agent.ResetPath(); agent.isStopped = true; return; }
            if (Time.time >= nextPathUpdate)
            {
                nextPathUpdate = Time.time + 0.25f;
                if (Time.time >= recoveryUntil) UpdateChasePath();
            }
            CheckProgress();
            if (CanCatchPlayer() && respawn.TryRespawnAtCheckpoint(catchRespawnPoint, "Door .002", "The enemy caught you. Try again!"))
            {
                catchCooldownUntil = Time.time + 2f;
                ResetEnemy();
            }
        }

        private bool TryFindPlayerFloor(out Vector3 position)
        {
            // Always follow the floor beneath a jumping player, never a box top
            // or the nearest point on a different disconnected NavMesh island.
            position = default;
            if (TrySampleFloor(player.transform.position, 0.9f, out Vector3 direct) &&
                NavMesh.CalculatePath(agent.nextPosition, direct, navigationFilter, chasePath) && IsCompleteRoomPath(chasePath))
            {
                position = direct;
                return true;
            }
            float nearest = float.PositiveInfinity;
            for (int ring = 1; ring <= 3; ring++)
                for (int direction = 0; direction < 8; direction++)
                {
                    float angle = direction * Mathf.PI * 0.25f;
                    Vector3 point = player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                    if (!TrySampleFloor(point, 0.65f, out Vector3 sample) ||
                        !NavMesh.CalculatePath(agent.nextPosition, sample, navigationFilter, candidatePath) || !IsCompleteRoomPath(candidatePath)) continue;
                    Vector3 delta = sample - player.transform.position;
                    float score = delta.x * delta.x + delta.z * delta.z;
                    if (score >= nearest) continue;
                    nearest = score;
                    position = sample;
                }
            return nearest < float.PositiveInfinity && NavMesh.CalculatePath(agent.nextPosition, position, navigationFilter, chasePath) &&
                IsCompleteRoomPath(chasePath);
        }

        private bool IsCompleteRoomPath(NavMeshPath path)
        {
            if (path.status != NavMeshPathStatus.PathComplete) return false;
            foreach (Vector3 corner in path.corners)
                if (!InsideRoom(corner)) return false;
            return true;
        }

        private void UpdateChasePath()
        {
            hasReachableTarget = TryFindPlayerFloor(out Vector3 target);
            if (!hasReachableTarget)
            {
                // Do not keep grinding against the end of an old partial path.
                agent.ResetPath();
                return;
            }
            bool needsPath = !agent.hasPath || agent.isPathStale || agent.pathStatus != NavMeshPathStatus.PathComplete ||
                (target - chaseTarget).sqrMagnitude > 0.04f;
            chaseTarget = target;
            if (needsPath) agent.SetPath(chasePath);
        }

        private void CheckProgress()
        {
            if (Time.time < nextProgressCheck) return;
            nextProgressCheck = Time.time + 0.75f;
            bool shouldMove = agent.hasPath && !agent.pathPending && agent.remainingDistance > 0.25f;
            if (shouldMove && (agent.transform.position - progressPosition).sqrMagnitude < 0.0064f)
                stalledChecks++;
            else stalledChecks = 0;
            progressPosition = agent.transform.position;
            if (stalledChecks < 2 || Time.time < recoveryUntil) return;
            stalledChecks = 0;
            TryRecoveryDetour();
        }

        private void TryRecoveryDetour()
        {
            // Recover by WALKING a short clear path away from the corner, then
            // recalculate the chase. Never warp or push through a maze wall.
            Vector3 forward = chaseTarget - agent.transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = agent.transform.forward;
            forward.Normalize();
            for (int i = 0; i < 12; i++)
            {
                float angle = (i % 2 == 0 ? 1f : -1f) * (90f + i / 2 * 18f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * forward * (i < 6 ? 0.9f : 1.5f);
                if (!TrySampleFloor(agent.transform.position + offset, 0.4f, out Vector3 target) ||
                    (target - agent.transform.position).sqrMagnitude < 0.16f || !HasPhysicalClearance(target) ||
                    !NavMesh.CalculatePath(agent.nextPosition, target, navigationFilter, recoveryPath) || !IsCompleteRoomPath(recoveryPath)) continue;
                if (!agent.SetPath(recoveryPath)) continue;
                recoveryCount++;
                recoveryUntil = Time.time + 1.5f;
                return;
            }
            agent.ResetPath();
            nextPathUpdate = 0f;
        }

        private bool CanCatchPlayer()
        {
            Vector3 delta = player.transform.position - agent.transform.position;
            if (new Vector2(delta.x, delta.z).sqrMagnitude > catchDistance * catchDistance) return false;
            Bounds body = playerBody.bounds;
            if (body.min.y > agent.transform.position.y + navigationHeight || body.max.y < agent.transform.position.y) return false;
            Vector3 from = agent.transform.position + Vector3.up * (navigationHeight * 0.5f);
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
            nextProgressCheck = 0f;
            recoveryUntil = 0f;
            stalledChecks = 0;
            hasReachableTarget = false;
            progressPosition = home;
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
