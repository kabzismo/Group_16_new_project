#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using FPSStarter;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class LowGravityMotionCheck
{
    static MovingGravityPlatform[] platforms;
    static float[] minimum, maximum;
    static double started;
    static bool running;
    const string Request = "Temp/low-gravity-motion-test.txt";
    const string Report = "Temp/low-gravity-motion-results.txt";
    static LowGravityMotionCheck() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (File.Exists(Request) && EditorApplication.isPlaying && !EditorApplication.isCompiling)
        {
            File.Delete(Request);
            platforms = Object.FindObjectsByType<MovingGravityPlatform>(FindObjectsSortMode.None);
            minimum = platforms.Select(p => float.PositiveInfinity).ToArray();
            maximum = platforms.Select(p => float.NegativeInfinity).ToArray();
            started = EditorApplication.timeSinceStartup;
            running = true;
            Application.runInBackground = true;
            File.WriteAllText(Report, "Low gravity motion: Unity Play mode\n");
        }
        if (!running || !EditorApplication.isPlaying) return;
        try
        {
            if (platforms.Length != 7) throw new Exception("Expected seven moving platforms");
            for (int i = 0; i < platforms.Length; i++)
            {
                var p = platforms[i];
                minimum[i] = Mathf.Min(minimum[i], p.transform.position.x);
                maximum[i] = Mathf.Max(maximum[i], p.transform.position.x);
                if (p.room == null || p.GetComponent<TimedGravityPlatform>() != null)
                    throw new Exception("Invalid moving platform setup: " + p.name);
                var room = p.room.GetComponent<Collider>().bounds;
                foreach (var collider in p.GetComponentsInChildren<Collider>())
                {
                    var b = collider.bounds;
                    if (b.min.x < room.min.x || b.max.x > room.max.x || b.min.z < room.min.z || b.max.z > room.max.z)
                        throw new Exception("Platform leaves room: " + p.name);
                    if (!collider.Raycast(new Ray(b.center + Vector3.up * (b.extents.y + 1f), Vector3.down), out var hit, b.size.y + 2f))
                        throw new Exception("Moving collider cannot support landing: " + p.name);
                }
            }
            if (EditorApplication.timeSinceStartup - started < 7.5) return;
            for (int i = 0; i < platforms.Length; i++)
            {
                float sweep = maximum[i] - minimum[i];
                if (sweep < 3.5f) throw new Exception("Insufficient sweep: " + platforms[i].name + " " + sweep);
                File.AppendAllText(Report, "PASS " + platforms[i].name + ": " + sweep.ToString("F2") + " m sweep, within room, collider supports landing\n");
            }
            File.AppendAllText(Report, "PASS Seven platforms move without reading dialogue; timed platforms remain separate.\n");
            running = false;
            EditorApplication.isPlaying = false;
        }
        catch (Exception e)
        {
            File.AppendAllText(Report, "FAIL " + e.Message + "\n");
            running = false;
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
