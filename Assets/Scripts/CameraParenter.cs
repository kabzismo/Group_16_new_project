using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSStarter
{
    /// <summary>
    /// Automatically builds a first-person player with camera in every gameplay
    /// scene, exactly like the Framework's FpsStarterBootstrap.CreatePlayer().
    ///
    /// Uses [RuntimeInitializeOnLoadMethod] so it runs without needing any
    /// component in the scene — just having this script in the project is enough.
    ///
    /// Skips menu scenes ("Main Menu", "PauseMenu", "LoadingScene") so they
    /// keep their own cameras.
    /// </summary>
    public static class PlayerBuilder
    {
        private static readonly string[] SkipScenes = { "Main Menu", "PauseMenu", "LoadingScene" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            foreach (string skip in SkipScenes)
                if (sceneName == skip) return;

            // Level 1 contains an older controller with its Camera on the same
            // scaled transform. It cannot provide a stable FPS view, so retire
            // it and build the same Player -> View Camera hierarchy as Level 2.
            FirstPersonController existingController = Object.FindFirstObjectByType<FirstPersonController>();
            if (existingController != null)
            {
                if (existingController.GetComponent<Camera>() == null) return;
                existingController.gameObject.SetActive(false);
            }

            // Don't double-build if a correctly structured player already exists.
            if (GameObject.FindWithTag("Player") != null) return;

            Build();
        }

        private static void Build()
        {
            EnsureLevel2GroundCollider();

            // --- Player root with a human-scale capsule ---
            // Unity units in this level are metres. Keeping the root at the
            // floor (rather than at the controller's centre) makes its scale
            // and eye height behave like a conventional FPS controller.
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetPositionAndRotation(ResolveSpawnPosition(), ResolveSpawnRotation());

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.03f;
            cc.slopeLimit = 45f;

            // Reuse the scene's main camera when available. This preserves
            // its URP settings and, most importantly, makes it a child of the
            // moving player rather than leaving an orphan static camera active.
            Camera camera = FindMainSceneCamera();
            GameObject viewObj = camera != null ? camera.gameObject : new GameObject("View Camera");
            viewObj.name = "View Camera";
            viewObj.transform.SetParent(player.transform, false);
            viewObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            viewObj.transform.localRotation = Quaternion.identity;
            viewObj.tag = "MainCamera";

            if (camera == null) camera = viewObj.AddComponent<Camera>();
            camera.enabled = true;
            camera.nearClipPlane = 0.03f;
            if (viewObj.GetComponent<Camera>() == camera && camera.backgroundColor.a == 0f)
                camera.backgroundColor = new Color(0.035f, 0.035f, 0.035f);

            if (viewObj.GetComponent<AudioListener>() == null) viewObj.AddComponent<AudioListener>();

            // Add FPS components AFTER the camera child exists
            // so FirstPersonController.Awake() finds it via GetComponentInChildren<Camera>().
            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<InteractionUI>();
        }

        private static void EnsureLevel2GroundCollider()
        {
            if (SceneManager.GetActiveScene().name != "Level 2 grey scale") return;
            if (GameObject.Find("Level 2 FPS Ground") != null) return;

            // The visual ProBuilder floor is a 25 m square centred at
            // (-9, 2, 6). A simple box collider gives the FPS capsule a
            // dependable, solid surface without changing the scene's visuals.
            GameObject ground = new GameObject("Level 2 FPS Ground");
            ground.transform.position = new Vector3(-9f, 1.9f, 6f);
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(25f, 0.2f, 25f);
            Physics.SyncTransforms();
        }

        private static Camera FindMainSceneCamera()
        {
            Camera selected = Camera.main;
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (selected == null && cameras.Length > 0) selected = cameras[0];

            // A first-person scene has exactly one rendering camera. Disable
            // the unused cameras before parenting the selected one.
            foreach (Camera cam in cameras)
                if (cam != selected) cam.gameObject.SetActive(false);

            return selected;
        }

        private static Vector3 ResolveSpawnPosition()
        {
            Transform marker = FindSpawnMarker();
            if (marker != null) return marker.position;

            string sceneName = SceneManager.GetActiveScene().name;
            // The Level 2 floor is at y = 2. The controller root represents
            // the player's feet, so start just above that surface.
            if (sceneName == "Level 2 grey scale") return new Vector3(-5f, 2.05f, 2.3f);
            return new Vector3(0f, 1f, -7f);
        }

        private static Quaternion ResolveSpawnRotation()
        {
            Transform marker = FindSpawnMarker();
            if (marker != null) return marker.rotation;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Level 2 grey scale") return Quaternion.Euler(0f, -90f, 0f);
            return Quaternion.identity;
        }

        private static Transform FindSpawnMarker()
        {
            GameObject marker = GameObject.Find("Player Spawn");
            return marker != null ? marker.transform : null;
        }
    }
}
