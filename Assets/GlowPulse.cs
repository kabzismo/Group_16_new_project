using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    public float minIntensity = 1f;
    public float maxIntensity = 3f;
    public float speed = 2f;

    private Material material;
    private Color baseColor;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();

        material = renderer.material;
        baseColor = material.color;
    }

    void Update()
    {
        float glow = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            (Mathf.Sin(Time.time * speed) + 1f) / 2f
        );

        material.SetColor("_EmissionColor", baseColor * glow);
    }
}
