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
    private const int Columns = 10;
    private const int Rows = 16;
    private const float CellWidth = 2.2f;
    private const float CellDepth = 37.8f / Rows;
    private const float MazeMinX = 50.2f;
    private const float MazeMinZ = 28.3f;
    private const float WallThickness = 0.42f;
    private const float WallHeight = 1.9f;

    // The reference's staggered turns, traced onto the room's rectangular footprint.
    // Rows go from the far wall to the entrance. The second array runs along each vertical grid line.
    private static readonly string[] HorizontalWalls =
    {
        "##..######", ".#.#...#..", "..#.#.#..#", "..#.##....", "#..#....#.",
        ".....#....", "...#...#..", ".#..#.##.#", "..#...#..#", "...##..##.",
        "#.......#.", ".#..##....", "#.#.##.#.#", ".....#..#.", ".#....#.##",
        "...#......", "..########"
    };

    private static readonly string[] VerticalWalls =
    {
        "################", "..##...###....#.", "#..###..###..#..", ".#.#..##.#..##..",
        ".#.###....#..##.", "##..###..#.....#", "#.#.#.##...#.##.", "....##..##.###..",
        "#.#####...##..#.", ".#.##.#.#...#..#", "################"
    };

    internal static Vector3 IceKeyCenter => new Vector3(1.5f, Floor + 0.95f, 66.3f);
    internal static Vector3 MazeKeyCenter => CellCenter(Index(9, 0)) + Vector3.up * .87f;
    internal static Vector3 EnemyHome => CellCenter(Index(5, 8));

    // Three cross barriers make the route alternate sides of the courtyard.
    internal static Vector3[] IceWalkRoute => new[]
    {
        Ground(-8.64f, 30.5f), Ground(-8.64f, 33.6f),
        Ground(-18.55f, 33.6f), Ground(-18.55f, 42.8f), Ground(-.5f, 42.8f)
    }.Concat(IceZigzagTurns).Concat(new[]
    {
        Ground(-.5f, 63.35f), Ground(-18f, 63.35f), Ground(-18f, 65.75f),
        Ground(-8.6f, 65.75f), Ground(-8.6f, 68.05f), Ground(-.6f, 68.05f),
        Ground(1.5f, 66.3f), Ground(2.4f, 67.4f)
    }).ToArray();

    private static Vector3[] IceZigzagTurns => new[]
    {
        Ground(-.5f, 44.5f),
        Ground(-2f, 47.5f), Ground(-.5f, 50.5f), Ground(-2f, 53.5f),
        Ground(-.5f, 56.5f), Ground(-2f, 59.5f), Ground(-.5f, 62.5f)
    };

    internal static Vector3[] IceAlcoveRoute => new[]
    {
        Ground(-8.6f, 42.8f), Ground(-8.6f, 44.5f), Ground(-7.5f, 46f), Ground(-7.5f, 49f),
        Ground(-8.6f, 51.5f), Ground(-15.8f, 51.5f), Ground(-15.8f, 47.9f)
    };

    [MenuItem("Tools/Stage 2/Rebuild High Gravity Maze Walls Only")]
    internal static void RebuildMazeOnly()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode first.");
        Transform root = Find(RootName);
        Transform old = root.Find(MazeName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        BuildMaze(Group(MazeName, root), Find("Objects/Level 2 grey box/Cube (29)").gameObject);
        PlaceMazeActors();
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
    }

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
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    internal static Vector3[] MazeCellCenters => Enumerable.Range(0, Columns * Rows).Select(CellCenter).ToArray();
    internal static Vector3[] MazeEnemyTargets => new[]
    {
        CellCenter(Index(0, 15)), CellCenter(Index(9, 13)), CellCenter(Index(7, 11)),
        EnemyHome, CellCenter(Index(0, 4)), CellCenter(Index(8, 3)),
        CellCenter(Index(2, 1)), CellCenter(Index(9, 0)), CellCenter(Index(3, 0))
    };

    internal static Vector3[] MazePlayerRoute
    {
        get
        {
            bool[,] passages = BuildPassages();
            var route = new List<Vector3> { Ground(49.8f, 67.4f), Ground(CellCenter(Index(0, 15)).x, 67.4f) };
            route.AddRange(FindPath(passages, Index(0, 15), Index(9, 0)).Select(CellCenter));
            route.AddRange(FindPath(passages, Index(9, 0), Index(3, 0)).Skip(1).Select(CellCenter));
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
        Transform originalObstacles = Find("Objects/Level 2 grey box");
        Transform iceKey = Find("Big Key_1");
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
            BuildMaze(Group(MazeName, root), crate);
            MoveKey(iceKey, IceKeyCenter);
            PlaceMazeActors();

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

    private static void PlaceMazeActors()
    {
        MoveKey(Find("Big Key_4"), MazeKeyCenter);
        Transform enemy = GameObject.Find("High Gravity Chaser")?.transform;
        if (enemy == null) return;
        Undo.RecordObject(enemy, "Place enemy in the maze");
        enemy.position = EnemyHome;
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
        // Raise the masonry above eye level without narrowing the sliding lanes.
        const float wallHeight = 2.55f;
        Material ice = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ice surface.mat");
        void Wall(string name, float x1, float z1, float x2, float z2)
        {
            Vector3 start = new Vector3(x1, 0f, z1);
            Vector3 finish = new Vector3(x2, 0f, z2);
            Vector3 center = (start + finish) * .5f;
            float length = Vector3.Distance(start, finish);
            float yaw = -Mathf.Atan2(z2 - z1, x2 - x1) * Mathf.Rad2Deg;
            FittedBox(crate, parent, name, new Vector3(center.x, Floor + wallHeight * .5f, center.z), new Vector3(length + .08f, wallHeight, .55f), yaw);
            IceCap(crate, parent, name + " - Frozen Top", new Vector3(center.x, Floor + wallHeight + .04f, center.z), new Vector3(length + .16f, .08f, .67f), ice, yaw);
        }
        void Pier(string name, float x, float z, bool standalone = false)
        {
            float height = standalone ? 1.54f : wallHeight + .14f;
            FittedBox(crate, parent, name, new Vector3(x, Floor + height * .5f, z), new Vector3(.85f, height, .85f));
            IceCap(crate, parent, name + " - Frozen Top", new Vector3(x, Floor + height + .04f, z), new Vector3(.97f, .08f, .97f), ice);
        }

        // A broad entry opening and two parapets frame the icy courtyard.
        Wall("Entry Parapet West", -18.5f, 31.2f, -11.2f, 31.2f);
        Wall("Entry Parapet East", -6f, 31.2f, 1.5f, 31.2f);
        Pier("Entry West End", -11.2f, 31.2f);
        Pier("Entry East End", -6f, 31.2f);

        // Front L-shaped pockets leave the reference's central sliding spine open.
        Wall("West Front Pocket - Front", -17.1f, 36f, -12.2f, 36f);
        Wall("West Front Pocket - Return", -12.2f, 36f, -12.2f, 39.4f);
        Wall("West Front Pocket - Upper Bar", -14.7f, 39.4f, -10.6f, 39.4f);
        Wall("West Front Pocket - Outer Stub", -17.1f, 36f, -17.1f, 38.1f);
        Pier("West Front Corner", -12.2f, 36f);
        Pier("West Front Upper Corner", -10.6f, 39.4f);
        Wall("East Front Pocket - Front", -6.5f, 36f, .9f, 36f);
        Wall("East Front Pocket - Return", -6.5f, 36f, -6.5f, 39.1f);
        Pier("East Front Corner", -6.5f, 36f);
        Pier("East Front Stop", -6.5f, 39.1f);
        Pier("Central Stop 01", -8.6f, 38.1f, standalone: true);

        // Left alcove and staggered interior bars reproduce the reference's
        // recessed pocket and disconnected obstacles between the cross lanes.
        Wall("West Alcove - Front", -18.4f, 45f, -13.8f, 45f);
        Wall("West Alcove - Return", -13.8f, 45f, -13.8f, 49.3f);
        Wall("West Alcove - Outer Stub", -18.4f, 45f, -18.4f, 47.3f);
        Pier("West Alcove Front Corner", -13.8f, 45f);
        Wall("Middle West Bar", -13.8f, 48.8f, -10.6f, 48.8f);
        Pier("Middle West Stop", -10.6f, 48.8f);
        Wall("Middle East Bar", -6.5f, 46f, -4.1f, 46f);
        Wall("Middle East Return", -6.5f, 46f, -6.5f, 48.2f);
        Pier("Middle East Stop", -6.5f, 48.2f);
        Pier("Central Stop 02", -8.6f, 47.4f, standalone: true);

        Wall("Upper West Pocket - Back", -15.2f, 58.5f, -10.6f, 58.5f);
        Wall("Upper West Pocket - Return", -10.6f, 55.9f, -10.6f, 58.5f);
        Wall("Upper West Pocket - Outer Stub", -15.2f, 57f, -15.2f, 58.5f);
        Pier("Upper West Island A", -14.8f, 54.8f, standalone: true);
        Pier("Upper West Island B", -12f, 54.8f, standalone: true);
        Wall("Upper East Bar", -6.5f, 54f, -4.1f, 54f);
        Pier("Upper East Stop", -6.5f, 55.5f, standalone: true);
        Pier("Central Stop 03", -8.6f, 56.3f, standalone: true);
        Wall("Rear West Bar", -16f, 62.2f, -10.6f, 62.2f);
        Pier("Rear West Corner", -10.6f, 62.2f);
        Wall("Rear East Bar", -6.5f, 60.8f, -4.1f, 60.8f);
        Pier("Central Stop 04", -8.6f, 60.8f, standalone: true);

        // Parallel angled banks create the distinctive right-side zigzag lane.
        Vector3[] turns = IceZigzagTurns;
        for (int i = 1; i < turns.Length; i++)
        {
            Wall("Zigzag Inner " + i, turns[i-1].x - 1.05f, turns[i-1].z, turns[i].x - 1.05f, turns[i].z);
            Wall("Zigzag Outer " + i, turns[i-1].x + 1.05f, turns[i-1].z, turns[i].x + 1.05f, turns[i].z);
        }
        Wall("Zigzag Outer Entry End", .55f, 44.5f, 2.2f, 44.5f);
        Wall("Zigzag Outer Rear End", .55f, 62.5f, 2.2f, 62.5f);

        // Each barrier reaches the room wall or the zigzag bank. The only
        // opening in the middle barrier is through the angled sliding lane.
        Wall("Entry Cross Barrier", -14.9f, 41.3f, 3.6f, 41.3f);
        Pier("Entry Barrier Stop", -14.9f, 41.3f);
        Wall("Zigzag Cross Barrier West", -20.4f, 53.5f, -3.05f, 53.5f);
        Wall("Zigzag Cross Barrier East", -.95f, 53.5f, 3.6f, 53.5f);
        Wall("Rear Cross Barrier", -14.4f, 64.4f, 3.6f, 64.4f);
        Pier("Rear Barrier Stop", -14.4f, 64.4f);

        // Shallow walkable steps and a rear terrace echo the raised far end.
        // The real door and key remain at their existing scene positions.
        for (int i = 0; i < 4; i++)
        {
            float rise = .15f * (i + 1);
            IceCap(crate, parent, "Rear Stair " + (i + 1), new Vector3(-8.6f, Floor + rise * .5f, 65.5f + i * .75f),
                new Vector3(3.2f, rise, .8f), ice);
        }
        IceCap(crate, parent, "Rear Terrace", new Vector3(-10.2f, Floor + .3f, 68.1f), new Vector3(16.5f, .6f, 2f), ice);
        for (int i = 0; i < 3; i++)
        {
            float rise = .15f * (3 - i);
            IceCap(crate, parent, "Rear Stair Side Descent " + (i + 1), new Vector3(-1.6f + i * .65f, Floor + rise * .5f, 68.05f),
                new Vector3(.72f, rise, 1.55f), ice);
        }
        Pier("Rear Stair West Pier", -10.6f, 67f, standalone: true);
        Pier("Rear Stair East Pier", -6.5f, 67f, standalone: true);
        Wall("Rear Parapet West", -18.4f, 69.1f, -10.6f, 69.1f);
        Wall("Rear Parapet East", -6.5f, 69.1f, -1.9f, 69.1f);
        AddReferenceChest(parent);
    }

    private static void IceCap(GameObject crate, Transform parent, string name, Vector3 center, Vector3 size, Material ice, float yaw = 0f)
    {
        FittedBox(crate, parent, name, center, size, yaw);
        if (ice != null) parent.Find(name).GetComponentInChildren<MeshRenderer>().sharedMaterial = ice;
    }

    private static void AddReferenceChest(Transform parent)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blender assets/Furniture/Chest.blend");
        if (source == null) return;
        Transform anchor = Group("West Alcove - Existing Chest Mesh", parent);
        GameObject chest = Object.Instantiate(source, anchor);
        chest.SetActive(true);
        Undo.RegisterCreatedObjectUndo(chest, "Reuse chest in Ice alcove");
        foreach (Collider collider in chest.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (MonoBehaviour behaviour in chest.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        // URP's camera data depends on the Camera and is removed with the behaviours above.
        foreach (Camera camera in chest.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(camera);
        foreach (Animator animator in chest.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator);
        Renderer[] renderers = chest.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        chest.transform.position -= bounds.center;
        Vector3 size = new Vector3(1.4f, .9f, 1f);
        anchor.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        anchor.position = new Vector3(-17f, Floor + .45f, 47.9f);
        BoxCollider box = anchor.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
    }

    private static void BuildMaze(Transform parent, GameObject crate)
    {
        Material concrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Concrete material.mat");
        Material dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Dark grey.mat");
        Vector3 floorCenter = new Vector3(MazeMinX + Columns * CellWidth * .5f, Floor + .008f,
            MazeMinZ + Rows * CellDepth * .5f);
        FittedBox(crate, parent, "Dark Maze Floor", floorCenter,
            new Vector3(Columns * CellWidth, .014f, Rows * CellDepth), material: dark, solid: false);

        int wallNumber = 0;
        float centerY = Floor + WallHeight * .5f;
        for (int row = 0; row <= Rows; row++)
            foreach (Vector2Int run in Runs(HorizontalWalls[row].Select(tile => tile == '#').ToArray()))
                FittedBox(crate, parent, "Maze Wall " + (wallNumber++).ToString("00"),
                    new Vector3(MazeMinX + (run.x + run.y * .5f) * CellWidth, centerY, MazeMinZ + row * CellDepth),
                    new Vector3(run.y * CellWidth + WallThickness, WallHeight, WallThickness), material: concrete);
        for (int column = 0; column <= Columns; column++)
            foreach (Vector2Int run in Runs(VerticalWalls[column].Select(tile => tile == '#').ToArray()))
                FittedBox(crate, parent, "Maze Wall " + (wallNumber++).ToString("00"),
                    new Vector3(MazeMinX + column * CellWidth, centerY, MazeMinZ + (run.x + run.y * .5f) * CellDepth),
                    new Vector3(WallThickness, WallHeight, run.y * CellDepth + WallThickness), material: concrete);

        const float pillarHeight = 2.6f;
        var pillars = new List<Vector3>();
        foreach (int cell in new[] { Index(1, 12), Index(2, 5), Index(4, 1), Index(5, 8), Index(6, 5), Index(8, 3), Index(8, 12) })
        {
            Vector3 corner = CellCenter(cell) + new Vector3(.8f, 0f, -.85f);
            corner.y = Floor + pillarHeight * .5f;
            FittedBox(crate, parent, "Square Signal Pillar", corner, new Vector3(.55f, pillarHeight, .55f), material: dark);
            pillars.Add(corner + Vector3.up * (pillarHeight * .5f + .018f));
        }
        BuildMazeMarkings(parent, pillars);
    }

    private static void BuildMazeMarkings(Transform parent, List<Vector3> pillars)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        void Quad(Vector3 center, Vector3 right, Vector3 forward)
        {
            int start = vertices.Count;
            vertices.Add(center - right - forward);
            vertices.Add(center - right + forward);
            vertices.Add(center + right + forward);
            vertices.Add(center + right - forward);
            indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        void Ring(Vector3 center, float halfSize, float halfThickness)
        {
            float offset = halfSize - halfThickness;
            foreach (float sign in new[] { -1f, 1f })
            {
                Quad(center + Vector3.right * (sign * offset), Vector3.right * halfThickness, Vector3.forward * halfSize);
                Quad(center + Vector3.forward * (sign * offset), Vector3.right * (offset - halfThickness), Vector3.forward * halfThickness);
            }
        }

        // Paint only: the diagonal marks do not alter walking speed or collision.
        int[] stripedCells =
        {
            Index(0, 0), Index(0, 2), Index(2, 1), Index(4, 2), Index(7, 1), Index(9, 2), Index(9, 4),
            Index(1, 5), Index(3, 5), Index(6, 6), Index(8, 7), Index(0, 8), Index(5, 10), Index(9, 10),
            Index(1, 12), Index(3, 13), Index(7, 13), Index(9, 14), Index(2, 15), Index(6, 15)
        };
        for (int i = 0; i < stripedCells.Length; i++)
        {
            Vector3 center = CellCenter(stripedCells[i]);
            center.y = Floor + .024f;
            Vector3 direction = i % 3 == 0 ? Vector3.right : Vector3.forward;
            Vector3 across = new Vector3(direction.z, 0f, -direction.x);
            for (int stripe = -2; stripe <= 2; stripe++)
                Quad(center + direction * (stripe * .28f), across * .37f + direction * .19f, direction * .078f);
        }
        foreach (Vector3 top in pillars) Ring(top, .25f, .025f);
        int[] red = indices.ToArray();
        indices.Clear();
        Vector3 entrance = CellCenter(Index(0, 15));
        Vector3 key = CellCenter(Index(9, 0));
        entrance.y = key.y = Floor + .024f;
        Ring(entrance, .44f, .033f);
        Ring(key, .44f, .033f);

        var mesh = new Mesh { name = "High Gravity Maze Markings", subMeshCount = 2 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(red, 0);
        mesh.SetTriangles(indices, 1);
        mesh.RecalculateNormals();
        const string assetPath = "Assets/Scenes/Stage 2/High Gravity Maze Markings.asset";
        Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (saved == null)
        {
            AssetDatabase.CreateAsset(mesh, assetPath);
            saved = mesh;
        }
        else
        {
            EditorUtility.CopySerialized(mesh, saved);
            Object.DestroyImmediate(mesh);
        }
        var marking = Group(saved.name, parent);
        marking.gameObject.AddComponent<MeshFilter>().sharedMesh = saved;
        var renderer = marking.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[]
        {
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Enemy Eyes.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Light Grey.mat")
        };
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AssetDatabase.SaveAssets();
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
        var open = new bool[count, count];
        for (int row = 0; row < Rows; row++)
            for (int column = 0; column < Columns; column++)
            {
                int cell = Index(column, row);
                if (column + 1 < Columns && VerticalWalls[column + 1][row] == '.')
                    open[cell, cell + 1] = open[cell + 1, cell] = true;
                if (row + 1 < Rows && HorizontalWalls[row + 1][column] == '.')
                    open[cell, cell + Columns] = open[cell + Columns, cell] = true;
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

    private static void FittedBox(GameObject source, Transform parent, string name, Vector3 center, Vector3 size, float yaw = 0f,
        Material material = null, bool solid = true)
    {
        Transform anchor = Group(name, parent);
        Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        Transform visual = Group("Reused Crate Mesh", anchor);
        visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.gameObject.AddComponent<MeshRenderer>().sharedMaterials = material != null
            ? Enumerable.Repeat(material, mesh.subMeshCount).ToArray() : sourceRenderer.sharedMaterials;
        Vector3 scale = new Vector3(size.x / mesh.bounds.size.x, size.y / mesh.bounds.size.y, size.z / mesh.bounds.size.z);
        visual.localScale = scale;
        visual.localPosition = -Vector3.Scale(mesh.bounds.center, scale);
        if (solid) anchor.gameObject.AddComponent<BoxCollider>().size = size;
        anchor.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
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
