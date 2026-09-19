using System.Collections;
using UnityEngine;

namespace FPSStarter
{
    public sealed class Stage2DoorBootstrapper : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Wait for the Stage 2 scene and its doors to finish loading.
            yield return null;
            StageGameplayFixes.PrepareAnimatedDoors();
            StageGameplayFixes.ConfigureStage2KeyDoorLocks();
            Physics.SyncTransforms();

            yield return null;
            StageGameplayFixes.PrepareAnimatedDoors();
            StageGameplayFixes.ConfigureStage2KeyDoorLocks();
            Physics.SyncTransforms();
        }
    }
}
