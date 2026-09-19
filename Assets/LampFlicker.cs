using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class LampFlicker : MonoBehaviour, IPointerEnterHandler
{
    public Light lampLight;

    public float minIntensity = 0.5f;
    public float maxIntensity = 2f;

    public float flickerSpeed = 0.05f;
    public float flickerDuration = 0.8f;

    private bool isFlickering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isFlickering)
        {
            StartCoroutine(Flicker());
        }
    }

    IEnumerator Flicker()
    {
        isFlickering = true;

        float timer = 0f;

        while (timer < flickerDuration)
        {
            lampLight.intensity = Random.Range(minIntensity, maxIntensity);

            yield return new WaitForSeconds(flickerSpeed);

            timer += flickerSpeed;
        }

        
        lampLight.intensity = maxIntensity;

        isFlickering = false;
    }
}