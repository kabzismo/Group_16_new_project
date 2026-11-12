using UnityEngine;

public class OSTManager : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("OST Playlist (4 Tracks)")]
    [SerializeField] private AudioClip[] ostTracks = new AudioClip[4];

    private int currentTrack = 0;

    private void Start()
    {
        PlayNextTrack();
    }

    private void Update()
    {
        if (!musicSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    private void PlayNextTrack()
    {
        if (ostTracks == null || ostTracks.Length == 0)
            return;

        musicSource.clip = ostTracks[currentTrack];
        musicSource.Play();

        currentTrack++;

        if (currentTrack >= ostTracks.Length)
        {
            currentTrack = 0;
        }
    }
}
