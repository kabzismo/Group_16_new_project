using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSStarter
{
    /// <summary>
    /// Runtime repairs for Stage 2: solid ground, E-to-open doors, and pickable keys.
    /// </summary>
    public static class StageGameplayFixes
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Main Menu" || sceneName == "PauseMenu" || sceneName == "LoadingScene") return;

            StripPlayerPhysicsConflicts();
            PrepareKeys();
            RepairMeshColliders();
            if (IsStage2(sceneName))
            {
                EnsureGround();
                RepairEmptyDoors();
            }
        }

        private static bool IsStage2(string sceneName)
        {
            return sceneName == "Stage 2" || sceneName == "Level 2 grey scale";
        }

        private static void StripPlayerPhysicsConflicts()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (player.GetComponent<CharacterController>() != null)
            {
                Rigidbody body = player.GetComponent<Rigidbody>();
                if (body != null) Object.Destroy(body);

                foreach (MeshCollider meshCollider in player.GetComponents<MeshCollider>())
                    Object.Destroy(meshCollider);
            }

            Level2RoomSetup setup = player.GetComponent<Level2RoomSetup>();
            if (setup != null) Object.Destroy(setup);
        }

        private static void EnsureGround()
        {
            if (GameObject.Find("Stage 2 FPS Ground") != null) return;

            GameObject ground = new GameObject("Stage 2 FPS Ground");
            ground.transform.position = new Vector3(10f, 1.9f, -4f);
            BoxCollider box = ground.AddComponent<BoxCollider>();
            box.size = new Vector3(60f, 0.3f, 60f);
            Physics.SyncTransforms();
        }

        private static void RepairMeshColliders()
        {
            MeshCollider[] colliders = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
            foreach (MeshCollider meshCollider in colliders)
            {
                if (meshCollider.sharedMesh != null) continue;

                MeshFilter filter = meshCollider.GetComponent<MeshFilter>();
                if (filter == null) filter = meshCollider.GetComponentInChildren<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    meshCollider.enabled = false;
                    continue;
                }

                if (filter.gameObject == meshCollider.gameObject)
                {
                    meshCollider.sharedMesh = filter.sharedMesh;
                    continue;
                }

                meshCollider.enabled = false;
                if (filter.GetComponent<Collider>() != null) continue;
                MeshCollider childCollider = filter.gameObject.AddComponent<MeshCollider>();
                childCollider.sharedMesh = filter.sharedMesh;
                childCollider.convex = true;
            }

            MeshCollider[] levelColliders = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
            foreach (MeshCollider meshCollider in levelColliders)
            {
                if (meshCollider.GetComponent<DoorOutward>() != null) continue;
                if (meshCollider.GetComponentInParent<CarryableObject>() != null) continue;
                if (IsKeyName(meshCollider.gameObject.name)) continue;
                if (meshCollider.sharedMesh == null) continue;
                meshCollider.convex = false;
            }
        }

        private static void PrepareKeys()
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform root in transforms)
            {
                if (root.parent != null) continue;
                if (!IsKeyName(root.name)) continue;
                MakePickup(root.gameObject);
            }
        }

        private static bool IsKeyName(string objectName)
        {
            string lower = objectName.ToLowerInvariant();
            return lower.Contains("key") && !lower.Contains("keyboard");
        }

        private static void MakePickup(GameObject key)
        {
            foreach (MeshCollider meshCollider in key.GetComponentsInChildren<MeshCollider>(true))
            {
                if (meshCollider.sharedMesh == null) meshCollider.enabled = false;
                else meshCollider.convex = true;
            }

            MeshFilter filter = key.GetComponentInChildren<MeshFilter>();
            GameObject host = filter != null ? filter.gameObject : key;
            if (host.GetComponent<BoxCollider>() == null)
            {
                BoxCollider box = host.AddComponent<BoxCollider>();
                box.isTrigger = false;
            }

            Rigidbody body = key.GetComponent<Rigidbody>();
            if (body == null) body = key.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            CarryableObject carryable = key.GetComponent<CarryableObject>();
            if (carryable == null) carryable = key.AddComponent<CarryableObject>();
            string displayName = key.name.Replace("(1)", "").Trim();
            carryable.Configure(displayName, "key");

            if (key.transform.position.y < 1.6f)
            {
                Vector3 position = key.transform.position;
                position.y = 2.2f;
                key.transform.position = position;
            }
        }

        private static void RepairEmptyDoors()
        {
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject door in objects)
            {
                if (door.name != "Door") continue;
                if (door.GetComponentInChildren<DoorOutward>() != null) continue;
                if (door.GetComponentInChildren<MeshFilter>() != null) continue;
                BuildDoorPanel(door.transform);
            }
        }

        private static void BuildDoorPanel(Transform hinge)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Door Panel";
            panel.transform.SetParent(hinge, false);
            panel.transform.localPosition = new Vector3(0f, 0f, 0.48f);
            panel.transform.localScale = new Vector3(0.08f, 1.95f, 0.96f);

            if (panel.GetComponent<DoorOutward>() == null) panel.AddComponent<DoorOutward>();
        }
    }
}
