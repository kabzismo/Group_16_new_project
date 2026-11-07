using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using FPSStarter;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class FinalKeyProbe : MonoBehaviour
{
    Keyboard keyboard;
    bool pressed;
    const string Report="Temp/final-key-tests.txt";
    void Feed()=>InputSystem.QueueStateEvent(keyboard,pressed?new KeyboardState(Key.E):new KeyboardState());
    void Check(bool condition,string message)
    {
        if(!condition){File.AppendAllText(Report,"FAIL "+message+"\n");throw new Exception(message);}
        File.AppendAllText(Report,"PASS "+message+"\n");
    }
    IEnumerator Start()
    {
        File.WriteAllText(Report,"Final exit interaction regression, Unity Play mode\n");
        Application.runInBackground=true;
        var hunt=FindFirstObjectByType<Stage2KeyHunt>();
        var exit=FindFirstObjectByType<Stage2ExitInteractable>();
        Check(hunt!=null&&exit!=null&&hunt.ClaimedCount==0,"Fresh scene has exit and empty key inventory");
        var player=FindFirstObjectByType<FirstPersonController>();
        var interactor=FindFirstObjectByType<PlayerInteractor>();
        player.enabled=false;
        var camera=player.GetComponentInChildren<Camera>();
        var bounds=exit.GetComponent<Collider>().bounds;
        camera.transform.position=bounds.center+new Vector3(2.5f,1.5f,0f);
        camera.transform.LookAt(bounds.center+Vector3.up*.4f);
        Physics.SyncTransforms();
        Check(Physics.Raycast(camera.transform.position,camera.transform.forward,out var hit,4.5f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<Stage2ExitInteractable>()==exit,"Actual interaction ray reaches exit table");
        keyboard=InputSystem.AddDevice<Keyboard>("Final exit QA keyboard");keyboard.MakeCurrent();InputSystem.onBeforeUpdate+=Feed;
        yield return null;yield return null;
        Check(interactor.CurrentPrompt.Contains("Exit locked"),"Exit displays locked-key prompt before room keys");
        pressed=true;yield return null;yield return null;pressed=false;yield return null;
        Check(!hunt.IsEnding&&hunt.ClaimedCount==0,"E at locked exit does not finish or grant keys");
        var keys=FindObjectsByType<CarryableObject>(FindObjectsSortMode.None).Where(k=>k.IsStage2Key).OrderBy(k=>k.name,StringComparer.Ordinal).ToArray();
        Check(keys.Length==4,"Exactly four room keys configured");
        for(int i=0;i<keys.Length;i++)
        {
            Check(hunt.Claim(keys[i].gameObject),"Room key accepted: "+keys[i].ItemId);
            Destroy(keys[i].gameObject);yield return null;
            typeof(StageGameplayFixes).GetMethod("ConfigureStage2KeyDoorLocks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        }
        Check(hunt.ClaimedCount==4&&!hunt.IsEnding,"All four distinct keys recorded; exit awaits final interaction");
        var hintObject=new GameObject("Temporary overlapping entrance hint");
        var hint=hintObject.AddComponent<HintTrigger>();
        hint.hint=FindFirstObjectByType<Hint>();hint.hintLines=new[]{"This hint must not replace the exit."};
        hint.promptUI=new GameObject("Temporary speak prompt");
        yield return null;
        typeof(HintTrigger).GetField("playerInRange",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hint,true);
        yield return null;yield return null;
        Check(interactor.CurrentPrompt.Contains("finish game")&&!hint.promptUI.activeSelf,"Final-key prompt takes priority over overlapping speak prompt");
        pressed=true;yield return null;yield return null;pressed=false;yield return null;
        Check(hunt.IsEnding&&!Hint.IsReading,"Pressing E at unlocked exit starts ending without dialogue");
        var screen=FindFirstObjectByType<Stage2CompletionScreen>();
        Check(screen!=null&&(bool)typeof(Stage2CompletionScreen).GetField("visible",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(screen),"Existing challenge-complete screen is visible");
        Check(!player.enabled&&!interactor.enabled&&interactor.CurrentPrompt==string.Empty,"Completion freezes controls and clears interaction prompt");
        SessionState.SetBool("FinalKeyExpectedStop",true);
        SessionState.SetFloat("FinalKeyShownAt",(float)EditorApplication.timeSinceStartup);
        yield return new WaitForSecondsRealtime(.5f);
        Check(hunt.IsEnding,"Completion remains visible before timed shutdown");
    }
    void OnDestroy()
    {
        InputSystem.onBeforeUpdate-=Feed;
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
    }
}

[InitializeOnLoad]
static class FinalKeyShutdownCheck
{
    static FinalKeyShutdownCheck()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("FinalKeyExpectedStop",false))return;
            SessionState.SetBool("FinalKeyExpectedStop",false);
            double elapsed=EditorApplication.timeSinceStartup-SessionState.GetFloat("FinalKeyShownAt",0f);
            File.AppendAllText("Temp/final-key-tests.txt","PASS Game exits Play mode after completion countdown ("+elapsed.ToString("F1")+" seconds)\n");
        };
    }
}
