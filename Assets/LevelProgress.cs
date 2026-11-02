
using UnityEngine;

public class LevelProgress : MonoBehaviour
{
    private void Start()
    {
        PlayerPrefs.SetInt("Level1Visited", 1);
        PlayerPrefs.Save();

        Debug.Log("STAGE 2 VISITED - CONTINUE UNLOCKED!");
    }
}
