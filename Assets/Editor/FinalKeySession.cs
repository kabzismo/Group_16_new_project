using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using FPSStarter;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class FinalKeySession
{
    static FinalKeySession(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        const string request="Temp/final-key-request.txt";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        string cmd;
        try{cmd=File.ReadAllText(request).Trim();File.Delete(request);}catch(IOException){return;}
        try
        {
            if(cmd=="play"){EditorApplication.isPlaying=true;return;}
            if(cmd=="stop"){EditorApplication.isPlaying=false;return;}
            if(cmd=="bind")
            {
                if(EditorApplication.isPlaying)throw new Exception("Exit play first");
                var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene,"Temp/BeforeFinalKeyBinding.unity",true);
                var table=GameObject.Find("Objects/3 keys table");
                if(table==null)throw new Exception("Exit table not found");
                if(table.GetComponent<Stage2ExitInteractable>()==null)Undo.AddComponent<Stage2ExitInteractable>(table);
                EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Temp/final-key-result.txt","Exit completion bound and saved; live scene edits preserved.");
                return;
            }
            if(cmd=="test")
            {
                new GameObject("Temporary final key input check").AddComponent<FinalKeyProbe>();return;
            }
            Physics.SyncTransforms();
            var lines=Object.FindObjectsByType<CarryableObject>(FindObjectsSortMode.None).Select(k=>$"KEY {k.name} id={k.ItemId} root={k.transform.position} colliders={string.Join(";",k.GetComponentsInChildren<Collider>().Select(c=>c.name+":"+c.enabled+":"+c.isTrigger+":"+c.bounds))}").ToList();
            lines.AddRange(Object.FindObjectsByType<HintTrigger>(FindObjectsSortMode.None).Select(h=>$"HINT {h.name} room={h.entranceRoom?.Mechanic} bounds={h.GetComponent<Collider>().bounds} prompt={h.promptUI?.activeInHierarchy}"));
            lines.AddRange(Object.FindObjectsByType<DialogueTrigger>(FindObjectsSortMode.None).Select(h=>$"DIALOGUE {h.name} bounds={h.GetComponent<Collider>()?.bounds} prompt={h.promptUI?.activeInHierarchy}"));
            var player=Object.FindFirstObjectByType<FirstPersonController>();
            var hunt=Object.FindFirstObjectByType<Stage2KeyHunt>();
            lines.Add($"PLAYER {player?.transform.position} room={player?.CurrentMechanic} hunt={hunt?.ClaimedCount} hintReading={Hint.IsReading}");
            var interactor=Object.FindFirstObjectByType<PlayerInteractor>();
            lines.Add("PROMPT "+interactor?.CurrentPrompt);
            lines.AddRange(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(t=>t.name.IndexOf("key",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("table",StringComparison.OrdinalIgnoreCase)>=0)
                .Select(t=>$"PROP {t.name} parent={t.parent?.name} pos={t.position} active={t.gameObject.activeInHierarchy} components={string.Join(",",t.GetComponents<Component>().Select(c=>c?.GetType().Name))} renderers={string.Join(";",t.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+":"+r.enabled+":"+r.bounds))}"));
            if(cmd=="reproduce")
            {
                var keys=Object.FindObjectsByType<CarryableObject>(FindObjectsSortMode.None).Where(k=>k.IsStage2Key).OrderBy(k=>k.name,StringComparer.Ordinal).ToArray();
                foreach(var key in keys){lines.Add("CLAIM "+key.name+" id="+key.ItemId+" result="+hunt.Claim(key.gameObject)+" count="+hunt.ClaimedCount);}
            }
            File.WriteAllLines("Temp/final-key-result.txt",lines);
        }
        catch(Exception e){File.WriteAllText("Temp/final-key-result.txt",e.ToString());}
    }
}
