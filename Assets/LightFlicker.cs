using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    private Light flickerLight;

    public float minIntensity = 5.0f;
    public float maxIntensity = 10.0f;
    public float flickerSpeed = 0.1f;

    private void Start()
    {
        flickerLight = GetComponent<Light>();
        if (flickerLight != null) InvokeRepeating(nameof(Flicker), 0f, Mathf.Max(0.01f, flickerSpeed));
    }

    private void Flicker()
    {
        float randomIntensity = Random.Range(minIntensity, maxIntensity);
        if (flickerLight != null) flickerLight.intensity = randomIntensity;
    }
}
