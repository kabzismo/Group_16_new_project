using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

internal static class Stage2HotRoomFinish
{
    private const string Shell = "Objects/New Level 2 grey box/";
    private const string SceneAssets = "Assets/Scenes/Stage 2/";
    private const float West = 4.11f;
    private const float East = 24.11f;
    private const float South = 27.56f;
    private const float North = 71.26f;
    private const float Ceiling = 17.65f;

    internal static void Apply(Transform course, Material rust)
    {
        foreach (Renderer renderer in course.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, _ => rust);

        MeshFilter roof = Stage2ObstacleCourseLayout.Find(Shell + "Roofs").GetComponent<MeshFilter>();
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        AddCeilingFaces(roof, vertices, uv);

        var mesh = new Mesh { name = "Lava Room Rust Lining" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        int[] indices = new int[vertices.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();

        // The shared shell retains its original wall materials and collision.
        var lining = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
        Undo.RegisterCreatedObjectUndo(lining, "Add Lava room rust lining");
        lining.transform.SetParent(course, false);
        lining.GetComponent<MeshFilter>().sharedMesh = SaveAsset(mesh, SceneAssets + "Lava Room Rust Lining.asset");
        MeshRenderer liningRenderer = lining.GetComponent<MeshRenderer>();
        liningRenderer.sharedMaterial = rust;
        liningRenderer.shadowCastingMode = ShadowCastingMode.Off;

        var lava = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Lava tiles.mat");
        var floorMaterial = new Material(lava) { name = "Lava Room Rust Floor" };
        // Keep the lava emission and normal map with the same rust base texture as the platforms.
        floorMaterial.SetTexture("_BaseMap", rust.GetTexture("_BaseMap"));
        Renderer floor = Stage2ObstacleCourseLayout.Find(Shell + "Plane.003").GetComponent<Renderer>();
        Undo.RecordObject(floor, "Texture the Lava room floor");
        floor.sharedMaterial = SaveAsset(floorMaterial, SceneAssets + "Lava Room Rust Floor.mat");
        AssetDatabase.SaveAssets();
    }

    private static void AddCeilingFaces(MeshFilter source, List<Vector3> vertices, List<Vector2> uv)
    {
        Vector3[] points = source.sharedMesh.vertices;
        for (int i = 0; i < points.Length; i++) points[i] = source.transform.TransformPoint(points[i]);
        int[] triangles = source.sharedMesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = points[triangles[i]];
            Vector3 b = points[triangles[i + 1]];
            Vector3 c = points[triangles[i + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (normal.y >= -.98f || Mathf.Abs(a.y - Ceiling) >= .035f) continue;

            var polygon = new List<Vector3> { a, b, c };
            polygon = Clip(polygon, 0, West - .03f, 1f);
            polygon = Clip(polygon, 0, East + .03f, -1f);
            polygon = Clip(polygon, 2, South - .03f, 1f);
            polygon = Clip(polygon, 2, North + .03f, -1f);
            for (int j = 1; j + 1 < polygon.Count; j++)
            {
                if (Vector3.Cross(polygon[j] - polygon[0], polygon[j + 1] - polygon[0]).sqrMagnitude < .000001f) continue;
                foreach (Vector3 point in new[] { polygon[0], polygon[j], polygon[j + 1] })
                {
                    vertices.Add(point + normal * .012f);
                    uv.Add(new Vector2(point.x, point.z) / 4f);
                }
            }
        }
        if (vertices.Count == 0)
            throw new InvalidOperationException("Could not locate the Lava room ceiling in " + source.name);
    }

    private static List<Vector3> Clip(List<Vector3> polygon, int axis, float edge, float side)
    {
        var clipped = new List<Vector3>();
        if (polygon.Count == 0) return clipped;
        Vector3 previous = polygon[polygon.Count - 1];
        foreach (Vector3 current in polygon)
        {
            bool previousInside = (previous[axis] - edge) * side >= 0f;
            bool currentInside = (current[axis] - edge) * side >= 0f;
            if (currentInside != previousInside)
                clipped.Add(Vector3.Lerp(previous, current, (edge - previous[axis]) / (current[axis] - previous[axis])));
            if (currentInside) clipped.Add(current);
            previous = current;
        }
        return clipped;
    }

    private static T SaveAsset<T>(T generated, string path) where T : Object
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(generated, path);
            return generated;
        }
        EditorUtility.CopySerialized(generated, existing);
        Object.DestroyImmediate(generated);
        return existing;
    }
}
