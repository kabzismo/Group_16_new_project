using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSStarter
{
    /// <summary>
    /// Builds a swinging door and a themed interior for each of the four
    /// west-side rooms in "Level 2 grey scale". Safe to run more than once.
    /// </summary>
    public sealed class Level2RoomSetup : MonoBehaviour
    {
        public const string RootName = "Level 2 Rooms";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBuild()
        {
            if (SceneManager.GetActiveScene().name != "Level 2 grey scale") return;
            if (GameObject.Find(RootName) != null) return;
            if (Object.FindFirstObjectByType<Level2RoomSetup>() != null) return;
            new GameObject("Level 2 Room Setup").AddComponent<Level2RoomSetup>();
        }

        private void Awake()
        {
            if (GameObject.Find(RootName) != null) return;
            Build();
        }

        private static void Build()
        {
            Transform root = new GameObject(RootName).transform;

            // Door openings sit in the east wall of each room (x ≈ -9).
            CreateRoom(root, "Ice Room", new Vector3(-12.975f, 3.6f, -2.375f), new Vector3(7.25f, 3.2f, 4.05f),
                new Vector3(-9f, 3f, -1.66f), RoomMechanic.Ice, new Color(0.55f, 0.82f, 0.95f), BuildIceInterior);
            CreateRoom(root, "Hot Room", new Vector3(-12.975f, 3.6f, 1.8f), new Vector3(7.25f, 3.2f, 3.5f),
                new Vector3(-9f, 3f, 2.28f), RoomMechanic.Hot, new Color(0.72f, 0.22f, 0.08f), BuildHotInterior);
            CreateRoom(root, "Low Gravity Room", new Vector3(-12.975f, 3.6f, 5.75f), new Vector3(7.25f, 3.2f, 3.5f),
                new Vector3(-9f, 3f, 6.19f), RoomMechanic.LowGravity, new Color(0.45f, 0.32f, 0.78f), BuildLowGravityInterior);
            CreateRoom(root, "High Gravity Room", new Vector3(-12.975f, 3.6f, 9.68f), new Vector3(7.25f, 3.2f, 3.45f),
                new Vector3(-9f, 3f, 10.16f), RoomMechanic.HighGravity, new Color(0.28f, 0.24f, 0.2f), BuildHighGravityInterior);
        }

        private static void CreateRoom(Transform root, string roomName, Vector3 volumeCenter, Vector3 volumeSize,
            Vector3 doorHinge, RoomMechanic mechanic, Color doorColour, System.Action<Transform, Color> buildInterior)
        {
            Transform room = new GameObject(roomName).transform;
            room.SetParent(root, false);

            GameObject volumeObject = new GameObject("Mechanic Volume");
            volumeObject.transform.SetParent(room, false);
            volumeObject.transform.position = volumeCenter;
            BoxCollider trigger = volumeObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = volumeSize;
            RoomMechanicVolume volume = volumeObject.AddComponent<RoomMechanicVolume>();
            volume.Configure(mechanic);

            CreateDoor(room, doorHinge, doorColour);
            buildInterior(room, doorColour);
        }

        private static void CreateDoor(Transform room, Vector3 hingePosition, Color colour)
        {
            GameObject hinge = new GameObject("Door");
            hinge.transform.SetParent(room, false);
            hinge.transform.position = hingePosition + new Vector3(0f, 0f, -0.48f);
            hinge.AddComponent<DoorInteractable>();

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Door Panel";
            panel.transform.SetParent(hinge.transform, false);
            panel.transform.localPosition = new Vector3(0f, 0f, 0.48f);
            panel.transform.localScale = new Vector3(0.08f, 1.95f, 0.96f);
            VisualColor.Set(panel.GetComponent<Renderer>(), colour);

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.SetParent(panel.transform, false);
            handle.transform.localPosition = new Vector3(-0.6f, 0f, 0.32f);
            handle.transform.localScale = new Vector3(0.35f, 0.08f, 0.08f);
            Object.Destroy(handle.GetComponent<Collider>());
            VisualColor.Set(handle.GetComponent<Renderer>(), colour * 0.55f);
        }

        private static void BuildIceInterior(Transform room, Color accent)
        {
            CreateFloorOverlay(room, new Color(0.72f, 0.88f, 0.98f, 1f));
            CreateLight(room, new Vector3(0f, 1.2f, 0f), new Color(0.55f, 0.85f, 1f), 8f);
            CreateParticles(room, new Color(0.8f, 0.92f, 1f), false);

            MakeCube(room, "Ice Crystal", new Vector3(-1.6f, -1.1f, -0.9f), new Vector3(0.35f, 1.1f, 0.35f), accent, new Vector3(12f, 20f, 8f));
            MakeCube(room, "Ice Crystal", new Vector3(1.8f, -1.2f, 0.7f), new Vector3(0.28f, 0.9f, 0.28f), Color.white, new Vector3(-18f, 35f, 10f));
            MakeCube(room, "Ice Crystal", new Vector3(-0.4f, -1.25f, 1.1f), new Vector3(0.45f, 0.7f, 0.45f), accent * 1.1f, new Vector3(8f, -12f, 25f));
            MakeCube(room, "Frozen Block", new Vector3(1.4f, -1.35f, -1.1f), new Vector3(1.1f, 0.35f, 0.8f), new Color(0.62f, 0.8f, 0.92f));
        }

        private static void BuildHotInterior(Transform room, Color accent)
        {
            CreateFloorOverlay(room, new Color(0.45f, 0.12f, 0.04f, 1f));
            CreateLight(room, new Vector3(0f, 0.8f, 0f), new Color(1f, 0.45f, 0.12f), 10f);
            CreateParticles(room, new Color(1f, 0.4f, 0.08f), true);

            MakeCube(room, "Lava Rock", new Vector3(-1.7f, -1.35f, 0.8f), new Vector3(1.2f, 0.4f, 0.9f), accent);
            MakeCube(room, "Lava Rock", new Vector3(1.5f, -1.3f, -0.9f), new Vector3(0.9f, 0.5f, 1.1f), accent * 0.7f);
            MakeCube(room, "Ember Column", new Vector3(-0.2f, -0.7f, 0.1f), new Vector3(0.35f, 1.7f, 0.35f), new Color(0.95f, 0.35f, 0.08f));
            MakeCube(room, "Cinder", new Vector3(1.8f, -1.4f, 1.0f), new Vector3(0.4f, 0.25f, 0.4f), new Color(0.2f, 0.08f, 0.04f));
        }

        private static void BuildLowGravityInterior(Transform room, Color accent)
        {
            CreateFloorOverlay(room, new Color(0.12f, 0.08f, 0.22f, 1f));
            CreateLight(room, new Vector3(0f, 1.4f, 0f), new Color(0.62f, 0.48f, 1f), 7f);

            CreateFloatingProp(room, "Floater", new Vector3(-1.4f, -0.2f, -0.7f), new Vector3(0.45f, 0.45f, 0.45f), accent, 0.35f);
            CreateFloatingProp(room, "Floater", new Vector3(1.6f, 0.3f, 0.6f), new Vector3(0.3f, 0.3f, 0.3f), Color.white, 0.5f);
            CreateFloatingProp(room, "Floater", new Vector3(0.2f, 0.1f, 1.1f), new Vector3(0.55f, 0.2f, 0.55f), accent * 0.8f, 0.28f);
            MakeCube(room, "Anchor Pad", new Vector3(0f, -1.42f, 0f), new Vector3(1.4f, 0.12f, 1.4f), new Color(0.2f, 0.16f, 0.35f));
        }

        private static void BuildHighGravityInterior(Transform room, Color accent)
        {
            CreateFloorOverlay(room, new Color(0.18f, 0.16f, 0.14f, 1f));
            CreateLight(room, new Vector3(0f, 0.6f, 0f), new Color(0.55f, 0.42f, 0.22f), 5f);

            MakeCube(room, "Heavy Block", new Vector3(-1.5f, -1.15f, -0.6f), new Vector3(1.4f, 0.7f, 1.2f), accent);
            MakeCube(room, "Heavy Block", new Vector3(-1.5f, -0.55f, -0.6f), new Vector3(1.1f, 0.5f, 0.9f), accent * 1.15f);
            MakeCube(room, "Crushed Slab", new Vector3(1.6f, -1.45f, 0.7f), new Vector3(1.6f, 0.18f, 1.3f), new Color(0.22f, 0.2f, 0.18f));
            MakeCube(room, "Weight", new Vector3(0.2f, -1.2f, 1.0f), new Vector3(0.7f, 0.55f, 0.7f), new Color(0.12f, 0.12f, 0.12f));
        }

        private static void CreateFloorOverlay(Transform room, Color colour)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Themed Floor";
            floor.transform.SetParent(room, false);
            floor.transform.position = new Vector3(-12.975f, 2.03f, room.Find("Mechanic Volume").position.z);
            floor.transform.localScale = new Vector3(7.1f, 0.04f, 3.2f);
            Object.Destroy(floor.GetComponent<Collider>());
            VisualColor.Set(floor.GetComponent<Renderer>(), colour);
        }

        private static void CreateLight(Transform room, Vector3 localOffset, Color colour, float intensity)
        {
            GameObject lightObject = new GameObject("Room Light");
            lightObject.transform.SetParent(room.Find("Mechanic Volume"), false);
            lightObject.transform.localPosition = localOffset;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.intensity = intensity;
            light.range = 8f;
        }

        private static void CreateParticles(Transform room, Color colour, bool rise)
        {
            GameObject particleObject = new GameObject("Atmosphere");
            particleObject.transform.SetParent(room.Find("Mechanic Volume"), false);
            particleObject.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.startColor = colour;
            main.startSize = rise ? 0.08f : 0.16f;
            main.startLifetime = 2.4f;
            main.startSpeed = rise ? 0.55f : 0.12f;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = rise ? -0.05f : 0.02f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 12f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5.5f, 0.2f, 2.4f);

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.material = new Material(shader);
        }

        private static void CreateFloatingProp(Transform room, string objectName, Vector3 localOffset, Vector3 scale, Color colour, float bob)
        {
            GameObject prop = MakeCube(room.Find("Mechanic Volume"), objectName, localOffset, scale, colour);
            FloatingProp motion = prop.AddComponent<FloatingProp>();
            motion.Configure(bob);
        }

        private static GameObject MakeCube(Transform parent, string objectName, Vector3 localPosition, Vector3 scale, Color colour, Vector3 euler = default)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            cube.transform.localRotation = Quaternion.Euler(euler);
            Object.Destroy(cube.GetComponent<Collider>());
            VisualColor.Set(cube.GetComponent<Renderer>(), colour);
            return cube;
        }
    }

    public sealed class FloatingProp : MonoBehaviour
    {
        private Vector3 origin;
        private float amplitude = 0.35f;
        private float seed;

        public void Configure(float bobHeight)
        {
            amplitude = bobHeight;
        }

        private void Start()
        {
            origin = transform.localPosition;
            seed = Random.Range(0f, 12f);
        }

        private void Update()
        {
            float t = Time.time + seed;
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(t * 0.8f) * amplitude);
            transform.Rotate(12f * Time.deltaTime, 22f * Time.deltaTime, 7f * Time.deltaTime, Space.Self);
        }
    }
}
