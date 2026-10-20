using System;
using System.IO;
using System.Linq;
using System.Text;
using FPSStarter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Small, reversible Stage 2 collision repairs. Does not rebuild Low Gravity.</summary>
internal static class Stage2ScenePolish
{
    [MenuItem("Tools/Stage 2/Polish Shared Collision Geometry")]
    internal static void Polish()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Stage 2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Stage 2 outside Play mode first.");
        if (!File.Exists("Temp/Stage2BeforeSharedPolish.unity"))
            EditorSceneManager.SaveScene(scene, "Temp/Stage2BeforeSharedPolish.unity", true);
        // These are proven exact copies: same mesh, world transform and materials.
        DisableExactDuplicate("Objects/New Level 2 grey box/Roofs (1)", "Objects/New Level 2 grey box/Roofs");
        DisableExactDuplicate("Objects/Level 2 grey box/Cube (47)", "Objects/Level 2 grey box/Cube (44)");
        DisableExactDuplicate("Objects/Level 2 grey box/Cube (56)", "Objects/Level 2 grey box/Cube (43)");
        foreach (BoxCollider box in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if (box.isTrigger || !box.enabled || !box.name.StartsWith("Cube", StringComparison.Ordinal)) continue;
            // A fitted cube needs one solid shape, not simultaneous box/triangle contacts.
            foreach (MeshCollider mesh in box.GetComponents<MeshCollider>())
            {
                if (!mesh.enabled || mesh.isTrigger || mesh.sharedMesh == null || mesh.sharedMesh.name != "Cube") continue;
                Bounds local = mesh.sharedMesh.bounds;
                if ((local.center - box.center).sqrMagnitude > .0001f || (local.size - box.size).sqrMagnitude > .0001f) continue;
                Undo.RecordObject(mesh, "Remove duplicate cube collision");
                mesh.enabled = false;
            }
        }
        GameObject bed = GameObject.Find("prison bed");
        if (bed != null)
            foreach (MeshCollider mesh in bed.GetComponentsInChildren<MeshCollider>())
            {
                if (!mesh.convex || mesh.sharedMesh == null || mesh.attachedRigidbody != null) continue;
                Undo.RecordObject(mesh, "Use exact static bed collision");
                mesh.convex = false;
            }
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void DisableExactDuplicate(string duplicatePath, string retainedPath)
    {
        GameObject duplicate = GameObject.Find(duplicatePath), retained = GameObject.Find(retainedPath);
        if (duplicate == null || retained == null) return;
        MeshFilter a = duplicate.GetComponent<MeshFilter>(), b = retained.GetComponent<MeshFilter>();
        Renderer ar = duplicate.GetComponent<Renderer>(), br = retained.GetComponent<Renderer>();
        if (a == null || b == null || a.sharedMesh != b.sharedMesh || ar == null || br == null ||
            !ar.sharedMaterials.SequenceEqual(br.sharedMaterials)) return;
        for (int i = 0; i < 16; i++)
            if (Mathf.Abs(duplicate.transform.localToWorldMatrix[i] - retained.transform.localToWorldMatrix[i]) > .0001f) return;
        Undo.RecordObject(duplicate, "Retain duplicate geometry disabled");
        duplicate.SetActive(false);
    }

    [MenuItem("Tools/Stage 2/Write Room Diagnostics")]
    internal static void WriteDiagnostics()
    {
        Physics.SyncTransforms();
        var log = new StringBuilder();
        foreach (var room in Object.FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None))
            log.AppendLine(room.Mechanic + " " + room.GetComponent<Collider>().bounds);
        foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (!collider.enabled || collider.isTrigger) continue;
            log.AppendLine(collider.GetType().Name + " " + Stage2ObstacleCourseLayout.FullPath(collider.transform) + " " + collider.bounds);
        }
        File.WriteAllText("Temp/Stage2RefreshGeometry.txt", log.ToString());
    }
}
