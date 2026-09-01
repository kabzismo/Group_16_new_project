using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSStarter
{
    /// <summary>
    /// Runtime repairs for Stage 2: free movement, E-to-open animated doors, pickable keys, and a 3-key finish.
    /// </summary>
    public static class StageGameplayFixes
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoader()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            string sceneName = scene.name;
            if (sceneName == "Main Menu" || sceneName == "PauseMenu" || sceneName == "LoadingScene") return;

            Time.timeScale = 1f;
            GameSession.Reset();
            StripPlayerPhysicsConflicts();
            RemoveBlockingGroundPlanes();
            RepairMeshColliders();
            PrepareAnimatedDoors();
            PrepareChests();
            PrepareKeys();

            if (IsStage2(sceneName))
            {
                EnableStage2EnvironmentalRooms();
                EnsureStage2DoorBootstrapper();
                EnsureKeyHunt();
                UnstickPlayer();
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

        private static void RemoveBlockingGroundPlanes()
        {
            string[] names = { "Stage 2 FPS Ground", "Level 2 FPS Ground" };
            foreach (string objectName in names)
            {
                GameObject ground = GameObject.Find(objectName);
                if (ground != null) Object.Destroy(ground);
            }
        }

        private static void UnstickPlayer()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            if (player == null) return;

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller == null) return;

            if (player.transform.position.y < 0.9f)
            {
                Vector3 position = player.transform.position;
                position.y = 1.05f;
                player.transform.position = position;
            }

            controller.Move(Vector3.up * 0.05f);
        }

        private static void EnableStage2EnvironmentalRooms()
        {
            RoomMechanicVolume[] rooms = Object.FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None);
            foreach (RoomMechanicVolume room in rooms)
            {
                room.enabled = true;
                Collider roomCollider = room.GetComponent<Collider>();
                if (roomCollider == null) roomCollider = room.gameObject.AddComponent<BoxCollider>();
                roomCollider.enabled = true;
                roomCollider.isTrigger = true;
            }
        }

        private static void EnsureStage2DoorBootstrapper()
        {
            if (Object.FindFirstObjectByType<Stage2DoorBootstrapper>() != null) return;
            new GameObject("Stage 2 Door Bootstrapper").AddComponent<Stage2DoorBootstrapper>();
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
        }

        internal static void PrepareAnimatedDoors()
        {
            Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator animator in animators)
            {
                if (!HasBool(animator, "IsOpen")) continue;

                GameObject door = animator.gameObject;
                if (door.GetComponent<DoorOutward>() == null) door.AddComponent<DoorOutward>();
                EnsureInteractableCollider(door);
                EnsureBoxDoorCollider(door);
            }

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform transform in transforms)
            {
                if (!IsDoorName(transform.name)) continue;
                if (transform.GetComponent<Animator>() == null && transform.GetComponentInChildren<MeshFilter>() == null) continue;
                if (transform.GetComponent<DoorOutward>() == null && transform.GetComponentInParent<DoorOutward>() == null)
                    transform.gameObject.AddComponent<DoorOutward>();
                EnsureInteractableCollider(transform.gameObject);
            }
        }

        private static bool IsDoorName(string objectName)
        {
            string lower = objectName.ToLowerInvariant();
            return lower.StartsWith("door") && !lower.Contains("controller");
        }

        private static void EnsureInteractableCollider(GameObject door)
        {
            MeshFilter filter = door.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                MeshCollider meshCollider = door.GetComponent<MeshCollider>();
                if (meshCollider == null) meshCollider = door.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.enabled = true;
                return;
            }

            foreach (Collider collider in door.GetComponents<Collider>())
            {
                if (collider.enabled && (!(collider is MeshCollider meshCollider) || meshCollider.sharedMesh != null))
                    return;
            }

            Renderer renderer = door.GetComponent<Renderer>();
            if (renderer == null) renderer = door.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            BoxCollider box = door.GetComponent<BoxCollider>();
            if (box == null) box = door.AddComponent<BoxCollider>();
            box.enabled = true;
            box.isTrigger = false;
            Bounds bounds = renderer.bounds;
            box.center = door.transform.InverseTransformPoint(bounds.center);
            Vector3 size = door.transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        private static void EnsureBoxDoorCollider(GameObject door)
        {
            Renderer renderer = door.GetComponent<Renderer>();
            if (renderer == null) return;

            BoxCollider box = door.GetComponent<BoxCollider>();
            if (box == null) box = door.AddComponent<BoxCollider>();
            box.enabled = true;
            box.isTrigger = false;

            Bounds bounds = renderer.bounds;
            box.center = door.transform.InverseTransformPoint(bounds.center);
            Vector3 size = door.transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Max(0.05f, Mathf.Abs(size.x)), Mathf.Max(0.05f, Mathf.Abs(size.y)), Mathf.Max(0.05f, Mathf.Abs(size.z)));
        }

        private static bool HasBool(Animator animator, string parameterName)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                    return true;
            }
            return false;
        }

        private static void PrepareChests()
        {
            Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator animator in animators)
            {
                if (!IsChestLid(animator.transform)) continue;
                if (animator.GetComponent<ChestInteractable>() == null)
                    animator.gameObject.AddComponent<ChestInteractable>();
            }
        }

        private static bool IsChestLid(Transform candidate)
        {
            if (candidate == null || !candidate.name.ToLowerInvariant().Contains("lid")) return false;
            for (Transform parent = candidate.parent; parent != null; parent = parent.parent)
            {
                if (parent.name.ToLowerInvariant().Contains("chest")) return true;
            }
            return false;
        }

        private static void PrepareKeys()
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform root in transforms)
            {
                if (!IsKeyName(root.name)) continue;
                if (root.parent != null && IsKeyName(root.parent.name)) continue;
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

            // The root is the object carrying CarryableObject, so it must always
            // own a real collider for PlayerInteractor's ray to resolve the key.
            // Imported child MeshColliders are not relied on here.
            BoxCollider interactionCollider = key.GetComponent<BoxCollider>();
            if (interactionCollider == null) interactionCollider = key.AddComponent<BoxCollider>();
            interactionCollider.enabled = true;
            interactionCollider.isTrigger = false;

            Renderer pickupRenderer = key.GetComponentInChildren<Renderer>();
            if (pickupRenderer != null)
            {
                Bounds pickupBounds = pickupRenderer.bounds;
                interactionCollider.center = key.transform.InverseTransformPoint(pickupBounds.center);
                Vector3 size = key.transform.InverseTransformVector(pickupBounds.size);
                interactionCollider.size = new Vector3(Mathf.Max(0.15f, Mathf.Abs(size.x)), Mathf.Max(0.15f, Mathf.Abs(size.y)), Mathf.Max(0.15f, Mathf.Abs(size.z)));
            }

            Rigidbody body = key.GetComponent<Rigidbody>();
            if (body == null) body = key.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            CarryableObject carryable = key.GetComponent<CarryableObject>();
            if (carryable == null) carryable = key.AddComponent<CarryableObject>();
            if (key.GetComponent<SimpleInteractable>() == null) key.AddComponent<SimpleInteractable>();
            string displayName = key.name.Replace("(1)", "").Trim();
            carryable.Configure(displayName, "key");

            Renderer keyRenderer = key.GetComponentInChildren<Renderer>();
            float height = keyRenderer != null ? keyRenderer.bounds.center.y : key.transform.position.y;
            if (height < 0.6f)
            {
                Vector3 position = key.transform.position;
                position.y += 1.1f;
                key.transform.position = position;
            }
        }

        private static void EnsureKeyHunt()
        {
            if (Object.FindFirstObjectByType<Stage2KeyHunt>() != null) return;
            new GameObject("Stage 2 Key Hunt").AddComponent<Stage2KeyHunt>();
        }
    }

    /// <summary>Re-applies animated-door setup after a scene transition has fully initialized its objects.</summary>
    internal sealed class LegacyStage2DoorBootstrapper : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Runtime-initialization callback order differs when entering from the
            // menu, so run after two frames as well as immediately on scene load.
            yield return null;
            StageGameplayFixes.PrepareAnimatedDoors();
            Physics.SyncTransforms();

            yield return null;
            StageGameplayFixes.PrepareAnimatedDoors();
            Physics.SyncTransforms();
        }
    }

    internal sealed class LegacyStage2KeyHunt : MonoBehaviour
    {
        public const int RequiredKeys = 3;
        private readonly HashSet<int> claimed = new HashSet<int>();
        private bool ending;

        public int ClaimedCount => claimed.Count;

        public bool Claim(GameObject key)
        {
            if (ending || key == null) return false;
            if (!claimed.Add(key.GetInstanceID())) return false;
            GameSession.CollectedItems = claimed.Count;
            if (claimed.Count >= RequiredKeys) StartCoroutine(EndStage());
            return true;
        }

        private IEnumerator EndStage()
        {
            ending = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (player != null) player.enabled = false;
            PlayerInteractor interactor = FindFirstObjectByType<PlayerInteractor>();
            if (interactor != null) interactor.enabled = false;

            Stage2CompletionScreen screen = FindFirstObjectByType<Stage2CompletionScreen>();
            if (screen == null) screen = gameObject.AddComponent<Stage2CompletionScreen>();
            screen.Show();

            yield return new WaitForSecondsRealtime(4f);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnGUI()
        {
            if (!ending) GUI.Label(new Rect(18f, 16f, 420f, 28f), "Keys: " + claimed.Count + " / " + RequiredKeys);
        }
    }

    internal sealed class LegacyStage2CompletionScreen : MonoBehaviour
    {
        private bool visible;
        private GUIStyle titleStyle;
        private GUIStyle messageStyle;

        public void Show() => visible = true;

        private void OnGUI()
        {
            if (!visible) return;

            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            EnsureStyles();
            GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 56f), "CHALLENGE COMPLETE", titleStyle);
            GUI.Label(new Rect(0f, Screen.height * 0.49f, Screen.width, 34f), "You collected all three keys. The game will now close.", messageStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = Color.white;

            messageStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20
            };
            messageStyle.normal.textColor = Color.white;
        }
    }
}
