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
        if (renderer == null || renderer.sharedMaterial == null)
        {
            enabled = false;
            return;
        }
        material = renderer.material;
        baseColor = material.color;
        if (material.HasProperty("_EmissionColor")) material.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        if (material == null) return;
        float glow = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            (Mathf.Sin(Time.time * speed) + 1f) / 2f
        );

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", baseColor * glow);
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
