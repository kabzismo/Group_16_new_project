using UnityEngine;
using UnityEngine.Rendering;

namespace FPSStarter
{
    public sealed class TimedGravityPlatform : MonoBehaviour
    {
        public RoomMechanicVolume room;
        public float secondsToCollapse = 1.6f;
        public float secondsToRestore = 4f;
        [Range(.05f, .6f)] public float ghostOpacity = .22f;
        public bool IsSolid => !collapsed;
        private float remaining;
        private bool counting;
        private bool collapsed;
        private Collider[] surfaces;
        private Renderer[] visuals;
        private Material[][] solidMaterials;
        private Material[][] ghostMaterials;
        private FirstPersonController player;

        void Awake()
        {
            surfaces = GetComponentsInChildren<Collider>();
            visuals = GetComponentsInChildren<Renderer>();
            player = FindFirstObjectByType<FirstPersonController>();
            solidMaterials = new Material[visuals.Length][];
            ghostMaterials = new Material[visuals.Length][];
            for (int i = 0; i < visuals.Length; i++)
            {
                solidMaterials[i] = visuals[i].sharedMaterials;
                ghostMaterials[i] = new Material[solidMaterials[i].Length];
                for (int j = 0; j < solidMaterials[i].Length; j++)
                {
                    var source = solidMaterials[i][j];
                    if (source == null) continue;
                    var ghost = new Material(source) { name = source.name + " (platform ghost)" };
                    ghost.SetOverrideTag("RenderType", "Transparent");
                    if (ghost.HasProperty("_Surface")) ghost.SetFloat("_Surface", 1f);
                    if (ghost.HasProperty("_Mode")) ghost.SetFloat("_Mode", 3f);
                    ghost.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    ghost.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    ghost.SetInt("_ZWrite", 0);
                    ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    ghost.EnableKeyword("_ALPHABLEND_ON");
                    ghost.DisableKeyword("_ALPHATEST_ON");
                    ghost.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    ghost.SetShaderPassEnabled("ShadowCaster", false);
                    ghost.renderQueue = (int)RenderQueue.Transparent;
                    foreach (string property in new[] { "_BaseColor", "_Color" })
                        if (ghost.HasProperty(property))
                        {
                            Color color = ghost.GetColor(property);
                            color.a = ghostOpacity;
                            ghost.SetColor(property, color);
                        }
                    ghostMaterials[i][j] = ghost;
                }
            }
        }

        public void Land()
        {
            if (counting || collapsed || Hint.IsReading || room == null || !room.EntranceHintCompleted) return;
            counting = true;
            remaining = secondsToCollapse;
        }

        void Update()
        {
            if (Hint.IsReading || !counting) return;
            remaining -= Time.deltaTime;
            if (remaining > 0f) return;
            if (!collapsed)
            {
                SetSolid(false);
                remaining = secondsToRestore;
            }
            else
            {
                var body = player != null ? player.GetComponent<CharacterController>() : null;
                // Never make a platform solid through the player's capsule.
                if (body != null)
                    foreach (var visual in visuals)
                        if (visual.bounds.Intersects(body.bounds)) return;
                SetSolid(true);
                counting = false;
            }
        }

        private void SetSolid(bool solid)
        {
            collapsed = !solid;
            foreach (var surface in surfaces) surface.enabled = solid;
            for (int i = 0; i < visuals.Length; i++)
                visuals[i].sharedMaterials = solid ? solidMaterials[i] : ghostMaterials[i];
        }

        void OnDestroy()
        {
            if (ghostMaterials == null) return;
            foreach (var materials in ghostMaterials)
                foreach (var material in materials)
                    if (material != null) Destroy(material);
        }
    }
}
