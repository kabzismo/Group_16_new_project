using System;
using System.Linq;
using FPSStarter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class Stage2GravityChallengeLayout
{
    [MenuItem("Tools/Stage 2/Configure Low Gravity Platforms and Enemy Checkpoint")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode first.");
        var rooms = Object.FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None);
        var low = rooms.First(r => r.Mechanic == RoomMechanic.LowGravity);
        var high = rooms.First(r => r.Mechanic == RoomMechanic.HighGravity);
        var course = GameObject.Find(Stage2ObstacleCourseLayout.RootName).transform.Find(Stage2ObstacleCourseLayout.LowCourseName);
        // Add behaviour to the existing authored Blender crate landings, preserving meshes and textures.
        foreach (int index in new[] { 2, 7, 18 })
        {
            var landing = FindLanding(course, index);
            var moving = landing.GetComponent<MovingGravityPlatform>() ?? Undo.AddComponent<MovingGravityPlatform>(landing.gameObject);
            moving.room = low;
            moving.travel = new Vector3(.55f, 0f, 0f);
            moving.period = 4f;
            EditorUtility.SetDirty(moving);
        }
        foreach (int index in new[] { 4, 11, 20 })
        {
            var landing = FindLanding(course, index);
            if (!landing.GetComponentsInChildren<MeshFilter>().Any(m => AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith(".blend", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Timed landing must use an imported Blender mesh: " + landing.name);
            var timed = landing.GetComponent<TimedGravityPlatform>() ?? Undo.AddComponent<TimedGravityPlatform>(landing.gameObject);
            timed.room = low;
            timed.secondsToCollapse = 1.6f;
            timed.secondsToRestore = 4f;
            EditorUtility.SetDirty(timed);
        }
        var old = GameObject.Find("High Gravity - Moving and Timed Crossings");
        if (old != null) Undo.DestroyObjectImmediate(old);
        foreach (var hint in Object.FindObjectsByType<HintTrigger>(FindObjectsSortMode.None))
        {
            if (hint.entranceRoom == high)
            {
                Undo.RecordObject(hint, "Update high gravity hint");
                hint.hintLines = hint.hintLines.Where(line =>
                    !line.StartsWith("The maze now has red") && !line.StartsWith("Ride the blue") &&
                    !line.StartsWith("Plan each crossing") && !line.StartsWith("Click, press Enter")).ToArray();
                if (!hint.hintLines.Any(l => l.Contains("Door .002")))
                    hint.hintLines = hint.hintLines.Concat(new[] { "The enemy hunts after you finish this hint. If caught, you return beside Door .002." }).ToArray();
            }
            if (hint.entranceRoom == low && !hint.hintLines.Any(l => l.Contains("See-through")))
            {
                Undo.RecordObject(hint, "Update low gravity hint");
                hint.hintLines = hint.hintLines.Concat(new[] {
                    "Some of these crates move. Time your jump and ride them across.",
                    "Other crates phase out shortly after you land. Move quickly!",
                    "Solid crates support you. See-through crates cannot be landed on; wait for them to become solid again."
                }).ToArray();
            }
            EditorUtility.SetDirty(hint);
        }
        ConfigureCheckpoint(high, low);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
    private static Transform FindLanding(Transform course, int index) =>
        course.Cast<Transform>().First(t => t.name.StartsWith("Low " + index.ToString("00") + " -"));

    private static void ConfigureCheckpoint(RoomMechanicVolume high, RoomMechanicVolume low)
    {
        var highBounds = high.GetComponent<Collider>().bounds;
        var door = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(r => r.name.Replace(" ", "").Equals("door.002", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => (r.bounds.center - highBounds.center).sqrMagnitude).First();
        Bounds bounds = door.bounds;
        Vector3 side = bounds.size.x >= bounds.size.z ? Vector3.forward : Vector3.right;
        // Respawn on the approach side, away from the chaser's room.
        if (Vector3.Dot(side, low.GetComponent<Collider>().bounds.center - bounds.center) < 0) side = -side;
        float thickness = bounds.size.x >= bounds.size.z ? bounds.extents.z : bounds.extents.x;
        Vector3 point = bounds.center + side * (thickness + 1.25f);
        var hits = Physics.RaycastAll(point, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore)
            .Where(h => h.normal.y > .6f && !h.collider.transform.IsChildOf(door.transform) &&
                h.collider.GetComponentInParent<FirstPersonController>() == null)
            .OrderBy(h => h.distance).ToArray();
        if (hits.Length == 0) throw new InvalidOperationException("No floor beside Door .002");
        point.y = hits[0].point.y + .08f;
        var player = Object.FindFirstObjectByType<FirstPersonController>();
        var body = player.GetComponent<CharacterController>();
        float radius = body.radius * Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.z);
        float height = body.height * player.transform.lossyScale.y;
        Vector3 tangent = Vector3.Cross(Vector3.up, side);
        bool clear = false;
        for (int outward = 0; outward < 6 && !clear; outward++)
            foreach (float lateral in new[] { 0f, .5f, -.5f, 1f, -1f, 1.5f, -1.5f })
            {
                Vector3 candidate = point + side * (outward * .4f) + tangent * lateral;
                if (!Physics.Raycast(candidate + Vector3.up, Vector3.down, out var floor, 2f, ~0, QueryTriggerInteraction.Ignore) || floor.normal.y < .6f) continue;
                candidate.y = floor.point.y + .08f;
                var obstacles = Physics.OverlapCapsule(candidate + Vector3.up * (radius + .03f),
                    candidate + Vector3.up * Mathf.Max(radius + .03f, height - radius), radius + .04f, ~0, QueryTriggerInteraction.Ignore);
                if (obstacles.Any(c => !c.transform.IsChildOf(player.transform))) continue;
                point = candidate;
                clear = true;
                break;
            }
        if (!clear) throw new InvalidOperationException("No clear player-sized checkpoint beside Door .002");
        var marker = GameObject.Find("Enemy Catch Checkpoint - Door .002");
        if (marker == null) { marker = new GameObject("Enemy Catch Checkpoint - Door .002"); Undo.RegisterCreatedObjectUndo(marker, "Create enemy checkpoint"); }
        marker.transform.SetPositionAndRotation(point, Quaternion.LookRotation(-side));
        var enemy = Object.FindFirstObjectByType<HighGravityEnemy>();
        Undo.RecordObject(enemy, "Set enemy catch checkpoint");
        enemy.ConfigureCatchCheckpoint(marker.transform);
        EditorUtility.SetDirty(enemy);
        Debug.Log("Enemy checkpoint at " + point + " beside " + door.name + "; low-gravity platforms now use Blender crates.");
    }
}
