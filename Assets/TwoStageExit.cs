using FPSStarter;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class TwoStageExit : MonoBehaviour
{
    public enum Stage { Stage1, Stage2 }

    [SerializeField] private Stage thisStage;
    [SerializeField] private string otherStageScene;
    [SerializeField] private string gameCompleteScene = "MainMenu";

    private static bool stage1Completed;
    private static bool stage2Completed;
    private bool used;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used || other.GetComponentInParent<FirstPersonController>() == null)
            return;

        used = true;

        if (thisStage == Stage.Stage1)
            stage1Completed = true;
        else
            stage2Completed = true;

        // Both stages are done: end the run rather than repeat it.
        if (stage1Completed && stage2Completed)
        {
            SceneManager.LoadScene(gameCompleteScene);
            return;
        }

        SceneManager.LoadScene(otherStageScene);
    }

    // Call this when starting a fresh run without stopping Play mode.
    public static void ResetProgress()
    {
        stage1Completed = false;
        stage2Completed = false;
    }
}