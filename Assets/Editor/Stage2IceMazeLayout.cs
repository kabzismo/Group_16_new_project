using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FPSStarter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Native scene authoring only; the routes use the project's existing meshes and materials.</summary>
internal static class Stage2IceMazeLayout
{
    internal const string RootName = "Ice and High Gravity - Cohesive Routes";
    internal const string IceName = "Ice Room - Frostbound Switchbacks";
    internal const string MazeName = "High Gravity Room - Looping Crate Maze";
    private const float Floor = Stage2ObstacleCourseLayout.Floor;
    private const int Columns = 5;
    private const int Rows = 9;
    private const float CellWidth = 4.4f;
    private const float CellDepth = 4.2f;
    private const float MazeMinX = 50.2f;
    private const float MazeMinZ = 28.3f;
    private const float WallThickness = 0.55f;
    private const float WallHeight = 2.8f;

    internal static Vector3 IceKeyCenter => new Vector3(1.5f, Floor + 0.95f, 66.3f);
    internal static Vector3 MazeKeyCenter => new Vector3(53.1f, Floor + 0.95f, 29.5f);
    internal static Vector3 EnemyHome => Ground(61.6f, 48.2f);

    // Continuous floor route: alternate wide braking turns and tighter slalom lanes.
    internal static Vector3[] IceWalkRoute => new[]
    {
        Ground(-8.64f, 30.5f), Ground(-17.7f, 33.5f), Ground(-17.7f, 38.5f),
        Ground(-12f, 39f), Ground(-5f, 39f), Ground(0.5f, 39f), Ground(0.5f, 44.5f),
        Ground(-5f, 45f), Ground(-12f, 45f), Ground(-17.7f, 45f), Ground(-17.7f, 50.5f),
        Ground(-12f, 51f), Ground(-5f, 51f), Ground(0.5f, 51f), Ground(0.5f, 56.5f),
        Ground(-5f, 57f), Ground(-12f, 57f), Ground(-17.7f, 57f), Ground(-17.7f, 63f),
        Ground(-12f, 63f), Ground(-7f, 64.5f), Ground(-3f, 66.3f),
        Ground(1.5f, 66.3f), Ground(2.4f, 67.4f)
    };

    [MenuItem("Tools/Stage 2/Rebuild Ice Ground Course Only")]
    internal static void RebuildIceOnly()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode first.");
        Transform root = Find(RootName);
        Transform old = root.Find(IceName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        BuildIce(Group(IceName, root), Find("Objects/Level 2 grey box/Cube (29)").gameObject);
        MoveKey(Find("Big Key_1"), IceKeyCenter);
        foreach (var hint in Object.FindObjectsByType<HintTrigger>(FindObjectsSortMode.None))
        {
            if (hint.entranceRoom == null || hint.entranceRoom.Mechanic != RoomMechanic.Ice) continue;
            Undo.RecordObject(hint, "Describe the ice ground course");
            hint.hintLines = new[] {
                "Stay on the icy floor and weave around the tall banks. The key is at ground level at the far end.",
                "You keep sliding after releasing movement. Brake before each turn; steer against your momentum to change direction.",
                "The gaps alternate sides. Take the slalom lanes slowly, then build speed on the straights. No jumping is needed."
            };
            EditorUtility.SetDirty(hint);
        }
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    internal static Vector3[] MazeCellCenters => Enumerable.Range(0, Columns * Rows).Select(CellCenter).ToArray();
    internal static Vector3[] MazeEnemyTargets => new[]
    {
        CellCenter(Index(0, 8)), CellCenter(Index(4, 8)), CellCenter(Index(3, 6)),
        EnemyHome, CellCenter(Index(0, 4)), CellCenter(Index(4, 3)),
        CellCenter(Index(2, 1)), CellCenter(Index(0, 0)), CellCenter(Index(1, 0))
    };

    internal static Vector3[] MazePlayerRoute
    {
        get
        {
            bool[,] passages = BuildPassages();
            var route = new List<Vector3> { Ground(49.8f, 67.4f), Ground(52.4f, 67.4f) };
            route.AddRange(FindPath(passages, Index(0, 8), Index(0, 0)).Select(CellCenter));
            route.Add(Ground(MazeKeyCenter.x, MazeKeyCenter.z));
            route.Add(CellCenter(Index(0, 0)));
            route.AddRange(FindPath(passages, Index(0, 0), Index(1, 0)).Skip(1).Select(CellCenter));
            route.Add(Ground(57.3f, 29.6f));
            route.Add(Ground(57.3f, 28.5f));
            return route.ToArray();
        }
    }

    [MenuItem("Tools/Stage 2/Rebuild Ice and High Gravity Maze")]
    internal static void RebuildIceAndMaze()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode before rebuilding Ice and the maze.");

        GameObject crate = Find("Objects/Level 2 grey box/Cube (29)").gameObject;
        GameObject shaft = Find("Objects/Bat/Cylinder.001").gameObject;
        GameObject cap = Find("Objects/Bat/Cylinder.002").gameObject;
        Transform originalObstacles = Find("Objects/Level 2 grey box");
        Transform iceKey = Find("Big Key_1");
        Transform highKey = Find("Big Key_4");
        if (crate.GetComponent<MeshFilter>()?.sharedMesh == null)
            throw new InvalidOperationException("The existing crate template has no mesh.");

        const string backup = "Temp/Stage2BeforeIceMaze20261019.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(scene, backup, true);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Author cohesive Ice switchbacks and High Gravity maze");
        try
        {
            GameObject previous = GameObject.Find(RootName);
            if (previous != null) Undo.DestroyObjectImmediate(previous);
            int disabled = DisableOldRoomCrates(originalObstacles);
            // The newer imported level also contains a second set of old Ice
            // blocks. Keep the room shell/planes/doors, but do not leave those
            // solid blocks underneath the replacement route.
            disabled += DisableOldRoomCrates(Find("Objects/New Level 2 grey box"), false);
            Transform root = Group(RootName, null);
            BuildIce(Group(IceName, root), crate);
            BuildMaze(Group(MazeName, root), crate, shaft, cap);
            MoveKey(iceKey, IceKeyCenter);
            MoveKey(highKey, MazeKeyCenter);

            Transform enemy = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == "High Gravity Chaser");
            if (enemy != null)
            {
                Undo.RecordObject(enemy, "Place enemy on the maze's connected floor");
                enemy.position = EnemyHome;
            }

            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Ice switchbacks and connected maze authored. Disabled " + disabled +
                " old room crates for recovery. Hot/Low objects, room geometry, doors and player spawn were not changed.");
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
        finally { Undo.CollapseUndoOperations(undoGroup); }
    }

    private static int DisableOldRoomCrates(Transform originalObstacles, bool requireCrateAsset = true)
    {
        int disabled = 0;
        foreach (MeshRenderer renderer in originalObstacles.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.gameObject.activeInHierarchy || !renderer.name.StartsWith("Cube", StringComparison.Ordinal)) continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null ||
                requireCrateAsset && !AssetDatabase.GetAssetPath(filter.sharedMesh).EndsWith("Crate box.blend", StringComparison.OrdinalIgnoreCase)) continue;
            Vector3 center = renderer.bounds.center;
            bool ice = center.x > -20f && center.x < 3f && center.z > 28f && center.z < 69.5f;
            bool high = center.x > 50f && center.x < 73f && center.z > 28f && center.z < 69.5f;
            if (!ice && !high) continue;
            Undo.RecordObject(renderer.gameObject, "Retain old Ice/High crate (disabled)");
            renderer.gameObject.SetActive(false);
            disabled++;
        }
        return disabled;
    }

    private static void BuildIce(Transform parent, GameObject crate)
    {
        // Banks reach the room edge at alternate ends. Their height prevents
        // bypassing the turns by hopping over them; every required path is flat.
        for (int i = 0; i < 5; i++)
        {
            bool leftGap = i % 2 == 0;
            float minX = leftGap ? -15.8f : -20f;
            float maxX = leftGap ? 3f : -1.6f;
            FittedBox(crate, parent, "Ice Bank " + (i + 1) + (leftGap ? " - Turn Left" : " - Turn Right"),
                new Vector3((minX + maxX) * .5f, Floor + 1.6f, 36f + i * 6f),
                new Vector3(maxX - minX, 3.2f, 1f));
        }
        // Offset posts form chicanes within the cross-room straights. These
        // leave more than a player capsule of clearance on the intended line.
        for (int lane = 0; lane < 4; lane++)
        {
            float z = 39f + lane * 6f;
            FittedBox(crate, parent, "Ice Slalom " + lane + " A", new Vector3(-12f, Floor + 1.6f, z + 1.65f), new Vector3(1.2f, 3.2f, 1.7f));
            FittedBox(crate, parent, "Ice Slalom " + lane + " B", new Vector3(-5f, Floor + 1.6f, z - 1.65f), new Vector3(1.2f, 3.2f, 1.7f));
        }
        FittedBox(crate, parent, "Ice Final Chicane", new Vector3(-7f, Floor + 1.6f, 62.6f), new Vector3(1.2f, 3.2f, 2f));
    }

    private static void BuildMaze(Transform parent, GameObject crate, GameObject shaft, GameObject cap)
    {
        bool[,] passages = BuildPassages();
        int wallNumber = 0;
        for (int boundary = 0; boundary <= Rows; boundary++)
        {
            bool[] closed = Enumerable.Range(0, Columns).Select(column =>
                boundary == 0 ? column != 1 : boundary == Rows ? column != 0 :
                !passages[Index(column, boundary - 1), Index(column, boundary)]).ToArray();
            foreach (Vector2Int run in Runs(closed))
                FittedBox(crate, parent, "Maze Screen " + (wallNumber++).ToString("00"),
                    new Vector3(MazeMinX + (run.x + run.y * 0.5f) * CellWidth, Floor + WallHeight * 0.5f,
                        MazeMinZ + boundary * CellDepth), new Vector3(run.y * CellWidth + 0.04f, WallHeight, WallThickness));
        }
        for (int boundary = 0; boundary <= Columns; boundary++)
        {
            bool[] closed = Enumerable.Range(0, Rows).Select(row => boundary == 0 || boundary == Columns ||
                !passages[Index(boundary - 1, row), Index(boundary, row)]).ToArray();
            foreach (Vector2Int run in Runs(closed))
                FittedBox(crate, parent, "Maze Screen " + (wallNumber++).ToString("00"),
                    new Vector3(MazeMinX + boundary * CellWidth, Floor + WallHeight * 0.5f,
                        MazeMinZ + (run.x + run.y * 0.5f) * CellDepth), new Vector3(WallThickness, WallHeight, run.y * CellDepth + 0.04f));
        }

        // Three off-centre existing-model landmarks distinguish the loops
        // without filling the clear cell-centre navigation paths.
        foreach (int cell in new[] { Index(4, 7), Index(0, 3), Index(4, 1) })
        {
            Vector3 point = CellCenter(cell) + new Vector3(1.25f, 0f, 1.1f);
            ReusedMesh(shaft, parent, "Maze Landmark - Reused Pillar", new Vector3(point.x, Floor + 1.1f, point.z), new Vector3(0.65f, 2.2f, 0.65f), true);
            ReusedMesh(cap, parent, "Maze Landmark - Reused Cap", new Vector3(point.x, Floor + 2.3f, point.z), new Vector3(0.85f, 0.2f, 0.85f), true);
        }
    }

    private static IEnumerable<Vector2Int> Runs(bool[] closed)
    {
        int start = -1;
        for (int i = 0; i <= closed.Length; i++)
        {
            if (i < closed.Length && closed[i]) { if (start < 0) start = i; }
            else if (start >= 0) { yield return new Vector2Int(start, i - start); start = -1; }
        }
    }

    private static bool[,] BuildPassages()
    {
        int count = Columns * Rows;
        bool[,] open = new bool[count, count];
        bool[] visited = new bool[count];
        var stack = new Stack<int>();
        // Selected for a 19-cell solution with thirteen turns across all five
        // columns; other seeds can accidentally create a straight edge highway.
        uint random = 20261027;
        int Next(int maximum) { random = unchecked(random * 1664525u + 1013904223u); return (int)(random % (uint)maximum); }
        int first = Index(0, Rows - 1);
        visited[first] = true;
        stack.Push(first);
        while (stack.Count > 0)
        {
            int current = stack.Peek();
            int[] candidates = Neighbours(current).Where(next => !visited[next]).ToArray();
            if (candidates.Length == 0) { stack.Pop(); continue; }
            int chosen = candidates[Next(candidates.Length)];
            open[current, chosen] = open[chosen, current] = true;
            visited[chosen] = true;
            stack.Push(chosen);
        }
        // Ensure a direct final-key-to-exit link, then create genuine loop
        // alternatives instead of a single dead-end snake.
        open[Index(0, 0), Index(1, 0)] = open[Index(1, 0), Index(0, 0)] = true;
        var closedEdges = new List<Vector2Int>();
        for (int current = 0; current < count; current++)
            foreach (int neighbour in Neighbours(current))
                if (current < neighbour && !open[current, neighbour]) closedEdges.Add(new Vector2Int(current, neighbour));
        for (int i = 0; i < 8 && closedEdges.Count > 0; i++)
        {
            int chosen = Next(closedEdges.Count);
            Vector2Int edge = closedEdges[chosen];
            closedEdges.RemoveAt(chosen);
            open[edge.x, edge.y] = open[edge.y, edge.x] = true;
        }
        return open;
    }

    private static IEnumerable<int> Neighbours(int cell)
    {
        int column = cell % Columns;
        int row = cell / Columns;
        if (column > 0) yield return Index(column - 1, row);
        if (column < Columns - 1) yield return Index(column + 1, row);
        if (row > 0) yield return Index(column, row - 1);
        if (row < Rows - 1) yield return Index(column, row + 1);
    }

    private static int[] FindPath(bool[,] passages, int start, int finish)
    {
        int[] previous = Enumerable.Repeat(-1, Columns * Rows).ToArray();
        var queue = new Queue<int>();
        previous[start] = start;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == finish) break;
            foreach (int neighbour in Neighbours(current))
            {
                if (!passages[current, neighbour] || previous[neighbour] >= 0) continue;
                previous[neighbour] = current;
                queue.Enqueue(neighbour);
            }
        }
        if (previous[finish] < 0) throw new InvalidOperationException("The authored maze route is disconnected.");
        var path = new List<int>();
        for (int current = finish; ; current = previous[current]) { path.Add(current); if (current == start) break; }
        path.Reverse();
        return path.ToArray();
    }

    private static int Index(int column, int row) => row * Columns + column;
    private static Vector3 CellCenter(int index) => Ground(MazeMinX + (index % Columns + 0.5f) * CellWidth,
        MazeMinZ + (index / Columns + 0.5f) * CellDepth);
    private static Vector3 Ground(float x, float z) => new Vector3(x, Floor + 0.08f, z);

    private static Transform Group(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, "Create Ice/maze course object");
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static void FittedBox(GameObject source, Transform parent, string name, Vector3 center, Vector3 size, float yaw = 0f)
    {
        Transform anchor = Group(name, parent);
        Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        Transform visual = Group("Reused Crate Mesh", anchor);
        visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.gameObject.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
        Vector3 scale = new Vector3(size.x / mesh.bounds.size.x, size.y / mesh.bounds.size.y, size.z / mesh.bounds.size.z);
        visual.localScale = scale;
        visual.localPosition = -Vector3.Scale(mesh.bounds.center, scale);
        BoxCollider collider = anchor.gameObject.AddComponent<BoxCollider>();
        collider.size = size;
        anchor.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
    }

    private static void ReusedMesh(GameObject source, Transform parent, string name, Vector3 center, Vector3 size, bool solid)
    {
        Transform anchor = Group(name, parent);
        GameObject clone = Object.Instantiate(source);
        clone.name = name + " Mesh";
        clone.SetActive(true);
        clone.transform.SetParent(anchor, true);
        Undo.RegisterCreatedObjectUndo(clone, "Reuse existing pillar mesh");
        foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (Rigidbody body in clone.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
        foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        foreach (Animator animator in clone.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator);
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>().Where(renderer => renderer.bounds.size.sqrMagnitude > 0.0001f).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException("Existing pillar template has no visible renderer: " + source.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        clone.transform.position -= bounds.center;
        if (solid)
            foreach (MeshFilter mesh in clone.GetComponentsInChildren<MeshFilter>())
            {
                if (mesh.sharedMesh == null) continue;
                MeshCollider collider = mesh.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh.sharedMesh;
                collider.convex = false;
            }
        anchor.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        anchor.position = center;
    }

    private static void MoveKey(Transform key, Vector3 center)
    {
        Renderer[] renderers = key.GetComponentsInChildren<Renderer>().Where(renderer => renderer.bounds.size.sqrMagnitude > 0.0001f).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException("The progression key has no visible mesh: " + key.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Undo.RecordObject(key, "Place key on the cohesive progression route");
        key.position += center - bounds.center;
    }

    private static Transform Find(string path) => Stage2ObstacleCourseLayout.Find(path);
}
