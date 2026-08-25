using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSStarter
{

    public static class PlayerBuilder
    {
        private static readonly string[] SkipScenes = { "Main Menu", "PauseMenu", "LoadingScene" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoader()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene activeScene, LoadSceneMode loadMode)
        {
            string sceneName = activeScene.name;
            foreach (string skip in SkipScenes)
                if (sceneName == skip) return;

            // A player must belong to the scene it is playing in. Remove any
            // persistent controller left over from a previous scene, then use (or
            // build) this scene's own Player object.
            FirstPersonController[] controllers = Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None);
            foreach (FirstPersonController controller in controllers)
            {
                if (controller.gameObject.scene != activeScene)
                    Object.Destroy(controller.gameObject);
            }

            GameObject scenePlayer = FindPlayerInScene(activeScene);
            if (scenePlayer != null)
            {
                ConfigureExistingPlayer(scenePlayer);
                return;
            }

            Build();
        }

        private static GameObject FindPlayerInScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.CompareTag("Player")) return root;
                Transform nestedPlayer = root.transform.Find("Player");
                if (nestedPlayer != null && nestedPlayer.CompareTag("Player")) return nestedPlayer.gameObject;
            }
            return null;
        }

        private static void ConfigureExistingPlayer(GameObject player)
        {
            if (player.GetComponent<CharacterController>() == null) player.AddComponent<CharacterController>();
            if (player.GetComponent<FirstPersonController>() == null) player.AddComponent<FirstPersonController>();
            if (player.GetComponent<PlayerInteractor>() == null) player.AddComponent<PlayerInteractor>();
            if (player.GetComponent<InteractionUI>() == null) player.AddComponent<InteractionUI>();
        }

        private static void Build()
        {
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

          
            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<InteractionUI>();
        }

        private static Camera FindMainSceneCamera()
        {
            Camera selected = Camera.main;
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (selected == null && cameras.Length > 0) selected = cameras[0];

      
            foreach (Camera cam in cameras)
                if (cam != selected) cam.gameObject.SetActive(false);

            return selected;
        }

        private static Vector3 ResolveSpawnPosition()
        {
            Transform marker = FindSpawnMarker();
            if (marker != null) return marker.position;

            string sceneName = SceneManager.GetActiveScene().name;
           
            if (sceneName == "Level 2 grey scale" || sceneName == "Stage 2") return new Vector3(-5f, 2.05f, 2.3f);
            return new Vector3(0f, 1f, -7f);
        }

        private static Quaternion ResolveSpawnRotation()
        {
            Transform marker = FindSpawnMarker();
            if (marker != null) return marker.rotation;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Level 2 grey scale" || sceneName == "Stage 2") return Quaternion.Euler(0f, -90f, 0f);
            return Quaternion.identity;
        }

        private static Transform FindSpawnMarker()
        {
            GameObject marker = GameObject.Find("Player Spawn");
            return marker != null ? marker.transform : null;
        }
    }
}
