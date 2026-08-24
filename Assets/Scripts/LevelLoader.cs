using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public void LoadLevel1()
    {
        SceneManager.LoadScene("Stage 1");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("Stage 2");
    }

}
