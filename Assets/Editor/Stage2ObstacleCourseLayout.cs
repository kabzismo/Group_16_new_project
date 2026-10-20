using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FPSStarter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Authors ordinary scene objects; nothing is generated at runtime.</summary>
internal static class Stage2ObstacleCourseLayout
{
    internal const string RootName = "Reference Obstacle Courses";
    internal const string HotCourseName = "Hot Room - Staggered Lava Course";
    internal const string LowCourseName = "Low Gravity - Spiral Tower";
    internal const float Floor = -3.5982f;
    internal readonly struct Landing
    {
        public readonly Vector3 Top;
        public readonly Vector2 Size;
        public readonly float Yaw;
        public readonly bool Round;
        public Landing(float x, float z, float rise, float width, float depth, bool round = false, float yaw = 0f)
        {
            Top = new Vector3(x, Floor + rise, z);
            Size = new Vector2(width, depth);
            Yaw = yaw;
            Round = round;
        }
    }

    internal static Landing[] HotRoute => new[]
    {
        // A winding climb, two changes of direction, then a descending finale
        // beside the exit. Every required rise is <= .65 m; the tension comes
        // from the smaller, angled landings and the all-surfaces heat timer.
        new Landing(6.4f, 67.1f, 0.15f, 3.5f, 3.0f),
        new Landing(9.9f, 65.9f, 0.8f, 2.6f, 2.25f, false, -18f),
        new Landing(13.5f, 64.2f, 1.45f, 1.85f, 1.85f, true),
        new Landing(17.1f, 62.3f, 2.05f, 2.7f, 1.9f, false, 30f),
        new Landing(21.2f, 60.3f, 2.6f, 1.8f, 1.8f, true),
        new Landing(21.1f, 56.2f, 3.1f, 2.5f, 2.05f, false, -18f),
        new Landing(17.3f, 54.1f, 3.7f, 1.85f, 1.85f, true),
        new Landing(13.3f, 55.0f, 4.3f, 2.55f, 1.9f, false, -28f),
        new Landing(9.5f, 52.8f, 4.85f, 1.8f, 1.8f, true),
        new Landing(7.3f, 48.9f, 5.4f, 2.7f, 2.2f, false, 15f),
        new Landing(10.4f, 45.5f, 6.0f, 1.85f, 1.85f, true),
        new Landing(14.5f, 46.2f, 6.65f, 2.7f, 1.85f, false, -28f),
        new Landing(18.3f, 43.8f, 7.25f, 1.8f, 1.8f, true),
        new Landing(21.3f, 40.7f, 7.8f, 2.9f, 2.0f, false, 18f),
        new Landing(18.1f, 37.8f, 8.3f, 1.85f, 1.85f, true),
        new Landing(14.5f, 35.4f, 8.8f, 3.1f, 2.35f, false, -15f),
        new Landing(10.5f, 32.8f, 7.6f, 2.0f, 2.0f, true),
        new Landing(7.3f, 29.9f, 6.4f, 2.8f, 2.0f, false, -18f),
        new Landing(11.9f, 29.4f, 5.7f, 2.0f, 2.0f, true),
        new Landing(16.3f, 29.6f, 3.6f, 2.7f, 2.0f, false, 12f),
        new Landing(20.6f, 30.6f, 2.1f, 1.9f, 1.9f, true),
        new Landing(23.0f, 32.9f, 0.65f, 3.0f, 2.7f)
    };

    internal static Landing[] LowRoute
    {
        get
        {
            // x/z, height gained, width/depth. Long leaps alternate with high
            // precision jumps so the strong low-gravity boost cannot skip tiers.
            float[,] steps =
            {
                {29.5f, 32.5f, .65f, 3.3f, 2.8f},
                {35.3f, 33f, 2f, 1.65f, 1.65f},
                {41.5f, 33.5f, 1.2f, 2.6f, 1.35f},
                {44f, 38.5f, 2.5f, 1.6f, 1.6f},
                {40.5f, 43.5f, 1.4f, 2.4f, 1.4f},
                {44f, 48f, 2.4f, 1.5f, 1.5f},
                {41.5f, 53.5f, .7f, 3.1f, 2.7f},
                {43.5f, 59.2f, 2.1f, 2.4f, 1.3f},
                {40.4f, 64.3f, 1.3f, 1.5f, 1.5f},
                {34.8f, 66f, 2.2f, 2.5f, 1.35f},
                {29.8f, 63f, .8f, 3.1f, 2.7f},
                {31.3f, 57.5f, 2.4f, 1.5f, 1.5f},
                {29.5f, 52f, 1.8f, 2.3f, 1.3f},
                {33f, 47.6f, 2.3f, 1.45f, 1.45f},
                {29.6f, 42.7f, 1.2f, 2.4f, 1.3f},
                {32.3f, 37.6f, 2.5f, 1.4f, 1.4f},
                {29.5f, 32.5f, .8f, 3f, 2.5f},
                {35.2f, 34f, 2.4f, 1.4f, 1.4f},
                {41.6f, 33f, 1.2f, 2.4f, 1.25f},
                {43f, 38.3f, 2.4f, 1.35f, 1.35f},
                {39.5f, 43f, 1.4f, 2.35f, 1.25f},
                {42.5f, 47.7f, 2.3f, 1.35f, 1.35f},
                {43.5f, 53.5f, .8f, 3f, 2.5f},
                {40.3f, 58.5f, 2.5f, 1.4f, 1.4f},
                {43.3f, 63.2f, 1.6f, 2.3f, 1.3f},
                {37.5f, 66.3f, 1.2f, 3.4f, 2.8f}
            };
            var landings = new List<Landing>();
            float rise = 0f;
            for (int i = 0; i < steps.GetLength(0); i++)
            {
                rise += steps[i, 2];
                bool round = steps[i, 3] == steps[i, 4];
                bool rest = steps[i, 3] >= 3f;
                landings.Add(new Landing(steps[i, 0], steps[i, 1], rise, steps[i, 3], steps[i, 4], round,
                    round || rest ? 0f : i % 4 == 0 ? -30f : 30f));
            }
            return landings.ToArray();
        }
    }

    [MenuItem("Tools/Stage 2/Rebuild Reference Obstacle Courses")]
    internal static void Rebuild()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode before rebuilding the courses.");

        // Preserve a recovery copy of the saved scene, including the designer's work.
        const string backup = "Temp/Stage2BeforeReferenceCourses.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        GameObject crate = Find("Objects/Level 2 grey box/Cube (29)").gameObject;
        GameObject shaft = Find("Objects/Bat/Cylinder.001").gameObject;
        GameObject cap = Find("Objects/Bat/Cylinder.002").gameObject;
        GameObject cannon = Find("Objects/Canons").gameObject;
        GameObject furnace = Find("Objects/Furnace").gameObject;
        var hotRoom = FindRoom(RoomMechanic.Hot);
        var lowRoom = FindRoom(RoomMechanic.LowGravity);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Reshape Hot and Low Gravity obstacle courses");
        GameObject previous = GameObject.Find(RootName);
        if (previous != null) Undo.DestroyObjectImmediate(previous);
        var originals = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer renderer in originals)
        {
            MeshFilter mesh = renderer.GetComponent<MeshFilter>();
            if (mesh == null || !AssetDatabase.GetAssetPath(mesh.sharedMesh).EndsWith("Crate box.blend")) continue;
            if (!hotRoom.GetComponent<Collider>().bounds.Contains(renderer.bounds.center) &&
                !lowRoom.GetComponent<Collider>().bounds.Contains(renderer.bounds.center)) continue;
            Undo.RecordObject(renderer.gameObject, "Retain previous platform (disabled)");
            renderer.gameObject.SetActive(false);
        }
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            bool oldHotBeam = t.name == "long bat" || t.name == "long bat (1)";
            bool oldLowProp = t.parent != null && t.parent.name == "Objects" && (t.name.StartsWith("Canons") || t.name == "Bat" || t.name.StartsWith("Bat ("));
            if (!oldHotBeam && !oldLowProp) continue;
            Undo.RecordObject(t.gameObject, "Retain previous obstacle (disabled)");
            t.gameObject.SetActive(false);
        }

        Transform root = CreateGroup(RootName, null);
        BuildHotCourse(root, crate, shaft, cap, cannon, furnace);

        BuildLowCourse(root, crate, shaft, cap, cannon);

        Physics.SyncTransforms();
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Saved reference courses using only existing meshes and materials. Previous obstacles remain disabled for recovery.");
    }

    [MenuItem("Tools/Stage 2/Rebuild Hot Room Course Only")]
    internal static void RebuildHotOnly()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode before rebuilding the Hot Room.");

        // Save the live scene, including unsaved design work, as a recovery copy.
        string backup = "Temp/Stage2BeforeHotCourse_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true))
            throw new IOException("Could not save the Hot Room recovery copy.");

        // Resolve templates before changing anything. The originals may be
        // inactive, but their existing meshes and materials are retained.
        GameObject crate = Find("Objects/Level 2 grey box/Cube (29)").gameObject;
        GameObject shaft = Find("Objects/Bat/Cylinder.001").gameObject;
        GameObject cap = Find("Objects/Bat/Cylinder.002").gameObject;
        GameObject cannon = Find("Objects/Canons").gameObject;
        GameObject furnace = Find("Objects/Furnace").gameObject;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Elaborate Hot Room obstacle course");
        GameObject rootObject = GameObject.Find(RootName);
        Transform root = rootObject != null ? rootObject.transform : CreateGroup(RootName, null);
        Transform previous = root.Find(HotCourseName);
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
        BuildHotCourse(root, crate, shaft, cap, cannon, furnace);
        Physics.SyncTransforms();
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.Find(HotCourseName).gameObject;
        Debug.Log("Saved the expanded Hot Room: 22 main landings, optional precision shortcut, reused industrial props. Low Gravity was not changed. Recovery copy: " + backup);
    }

    private static void BuildHotCourse(Transform root, GameObject crate, GameObject shaft, GameObject cap, GameObject cannon, GameObject furnace)
    {
        Transform hot = CreateGroup(HotCourseName, root);
        Landing[] route = HotRoute;
        for (int i = 0; i < route.Length; i++)
        {
            string role = i == route.Length - 1 ? " - Exit Landing" : i == 15 ? " - Summit Turn" :
                i >= 16 ? " - Descending Finale" : route[i].Round ? " - Precision Pillar" : " - Angled Deck";
            Platform(hot, "Hot " + i.ToString("00") + role, route[i], crate, shaft, cap, true);
        }

        // Faster alternative from deck 07 to deck 11, saving one landing.
        // The .9 m opening rise and 1.65 m tops make this the harder choice.
        Platform(hot, "Hot Shortcut A - Expert Pillar", new Landing(14.9f, 51.3f, 5.2f, 1.65f, 1.65f, true), crate, shaft, cap, true);
        Platform(hot, "Hot Shortcut B - Expert Pillar", new Landing(14.2f, 48.6f, 6.0f, 1.65f, 1.65f, true), crate, shaft, cap, true);

        // Repeated silhouettes establish a furnace-hall rhythm. These are in
        // the gaps outside the required jump corridors, not invisible blockers
        // on landings. Compound props keep exact child-mesh collision.
        Copy(furnace, hot, "Reused Furnace - Intake", new Vector3(6.3f, Floor + 2.75f, 58f), new Vector3(2.3f, 5.5f, 2.3f), 15f, true, true);
        Copy(furnace, hot, "Reused Furnace - Central Stack", new Vector3(23.5f, Floor + 3.2f, 48.6f), new Vector3(2.4f, 6.4f, 2.4f), -15f, true, true);
        Copy(furnace, hot, "Reused Furnace - Exhaust", new Vector3(6.3f, Floor + 3.5f, 41.6f), new Vector3(2.3f, 7.0f, 2.3f), 15f, true, true);
        Copy(cannon, hot, "Reused Cannon - Inner Foundry", new Vector3(19.7f, Floor + 1.6f, 49.0f), new Vector3(4.7f, 3.2f, 2.8f), 40f, true, true);
        Copy(cannon, hot, "Reused Cannon - Summit Base", new Vector3(11.3f, Floor + 1.6f, 40.3f), new Vector3(4.8f, 3.2f, 2.7f), -25f, true, true);

        Transform finish = CreateGroup("Exit Frame - Reused Bat and Crate", hot);
        Copy(shaft, finish, "Left Frame Post", new Vector3(21.3f, Floor + 2.5f, 33.7f), new Vector3(.28f, 3.7f, .28f), 0f, true, true);
        Copy(shaft, finish, "Right Frame Post", new Vector3(24.7f, Floor + 2.5f, 33.7f), new Vector3(.28f, 3.7f, .28f), 0f, true, true);
        Copy(crate, finish, "Overhead Frame Beam", new Vector3(23f, Floor + 4.65f, 33.7f), new Vector3(3.8f, .25f, .3f), 0f, true);

        // The summit pickup makes the climb meaningful; a key beside the low
        // exit would let players bypass the course by hopping across the floor.
        MoveKey(Find("Big Key_2"), route[15].Top + Vector3.up * 0.75f);
    }

    [MenuItem("Tools/Stage 2/Rebuild Low Gravity Course Only")]
    internal static void RebuildLowGravityOnly()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode before rebuilding the course.");
        Transform root = GameObject.Find(RootName).transform;
        const string backup = "Temp/Stage2BeforeHardLowGravity.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Increase Low Gravity course difficulty");
        Transform previous = root.Find(LowCourseName);
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
        BuildLowCourse(root, Find("Objects/Level 2 grey box/Cube (29)").gameObject,
            Find("Objects/Bat/Cylinder.001").gameObject, Find("Objects/Bat/Cylinder.002").gameObject, Find("Objects/Canons").gameObject);
        Physics.SyncTransforms();
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.Find(LowCourseName).gameObject;
    }

    private static void BuildLowCourse(Transform root, GameObject crate, GameObject shaft, GameObject cap, GameObject cannon)
    {
        Transform low = CreateGroup(LowCourseName, root);
        Landing[] route = LowRoute;
        for (int i = 0; i < route.Length; i++)
            Platform(low, "Low " + i.ToString("00") + (i == route.Length - 1 ? " - Finish Landing" : route[i].Size.x >= 3f ? " - Rest Landing" : route[i].Round ? " - Precision Pillar" : " - Angled Ledge"), route[i], crate, shaft, cap, false, i < 16);
        Copy(cannon, low, "Reused Cannon - Lower Crossing", new Vector3(35.5f, 5.5f, 49f), new Vector3(5.5f, 3.4f, 3f), 20f, true, true);
        Copy(cannon, low, "Reused Cannon - Middle Crossing", new Vector3(36.5f, 19.8f, 57.8f), new Vector3(5.5f, 3.4f, 3f), -20f, true, true);
        Copy(cannon, low, "Reused Cannon - Upper Crossing", new Vector3(35.5f, 30f, 44.5f), new Vector3(4.4f, 3.4f, 3f), 20f, true, true);
        MoveKey(Find("Big Key_3"), route[route.Length - 1].Top + Vector3.up * 0.75f);
    }

    private static void Platform(Transform parent, string name, Landing landing, GameObject crate, GameObject shaft, GameObject cap, bool supported, bool groundedPillar = false)
    {
        Transform group = CreateGroup(name, parent);
        float height = supported ? Mathf.Max(0.15f, landing.Top.y - Floor) : 0.55f;
        if (!landing.Round)
        {
            Copy(crate, group, "Crate Platform", landing.Top - Vector3.up * height * 0.5f,
                new Vector3(landing.Size.x, height, landing.Size.y), landing.Yaw, true);
            return;
        }
        Copy(cap, group, "Round Landing", landing.Top - Vector3.up * 0.15f, new Vector3(landing.Size.x, 0.3f, landing.Size.y), 0f, true, true);
        float stemHeight = supported || groundedPillar ? Mathf.Max(0.2f, landing.Top.y - Floor - 0.3f) : 1.6f;
        Copy(shaft, group, "Pillar", landing.Top - Vector3.up * (0.3f + stemHeight * 0.5f),
            new Vector3(landing.Size.x * 0.7f, stemHeight, landing.Size.y * 0.7f), 0f, true, true);
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create course group");
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private static void Copy(GameObject source, Transform parent, string name, Vector3 center, Vector3 size, float yaw, bool solid, bool exactCollider = false)
    {
        Transform anchor = CreateGroup(name, parent);
        GameObject clone = Object.Instantiate(source);
        clone.name = source.name + " (reused mesh)";
        Undo.RegisterCreatedObjectUndo(clone, "Copy existing obstacle");
        clone.SetActive(true);
        clone.transform.SetParent(anchor, true);
        foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (Rigidbody rigidbody in clone.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rigidbody);
        foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        clone.transform.position -= bounds.center;
        if (solid)
        {
            if (exactCollider)
                foreach (MeshFilter mesh in clone.GetComponentsInChildren<MeshFilter>())
                {
                    if (mesh.sharedMesh == null) continue;
                    MeshCollider collider = mesh.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh.sharedMesh;
                }
            else
            {
                BoxCollider collider = anchor.gameObject.AddComponent<BoxCollider>();
                collider.size = bounds.size;
            }
        }
        anchor.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        anchor.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
    }

    /// <summary>Keep the saved layout while removing invisible boxes around hollow/compound props.</summary>
    internal static int RepairExistingCourseColliders()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode before repairing course colliders.");

        GameObject root = GameObject.Find(RootName);
        if (root == null) return 0;

        int repaired = 0;
        foreach (Transform anchor in root.GetComponentsInChildren<Transform>(true))
        {
            if (!anchor.name.StartsWith("Reused Cannon - ", StringComparison.Ordinal) &&
                !anchor.name.StartsWith("Reused Furnace - ", StringComparison.Ordinal)) continue;

            MeshFilter[] meshes = anchor.GetComponentsInChildren<MeshFilter>(true)
                .Where(mesh => mesh.sharedMesh != null).ToArray();
            if (meshes.Length == 0) continue;

            Collider[] oldColliders = anchor.GetComponentsInChildren<Collider>(true);
            bool alreadyExact = oldColliders.Length == meshes.Length && meshes.All(mesh =>
                oldColliders.Count(candidate => candidate is MeshCollider collider &&
                    collider.gameObject == mesh.gameObject && collider.sharedMesh == mesh.sharedMesh &&
                    collider.enabled && !collider.isTrigger && !collider.convex) == 1);
            if (alreadyExact) continue;

            // A compound's enclosing box seals the spaces between its pieces
            // and the opening of a furnace. Static, non-convex child collision
            // follows the visible meshes, including under the authored scale.
            foreach (Collider oldCollider in oldColliders)
                Undo.DestroyObjectImmediate(oldCollider);
            foreach (MeshFilter mesh in meshes)
            {
                MeshCollider collider = Undo.AddComponent<MeshCollider>(mesh.gameObject);
                collider.sharedMesh = mesh.sharedMesh;
                collider.convex = false;
            }
            repaired++;
        }

        if (repaired > 0) EditorSceneManager.MarkSceneDirty(scene);
        return repaired;
    }

    private static void MoveKey(Transform key, Vector3 center)
    {
        Renderer[] renderers = key.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Undo.RecordObject(key, "Move existing key onto finish landing");
        key.position += center - bounds.center;
    }

    internal static RoomMechanicVolume FindRoom(RoomMechanic mechanic) =>
        Object.FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None).First(room => room.Mechanic == mechanic);
    internal static Transform Find(string path) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(t => FullPath(t) == path);
    internal static string FullPath(Transform t) => t.parent != null ? FullPath(t.parent) + "/" + t.name : t.name;
}
