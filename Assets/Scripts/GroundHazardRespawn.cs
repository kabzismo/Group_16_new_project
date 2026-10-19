using UnityEngine;

namespace FPSStarter
{
    /// <summary>
    /// Makes the Hot Room floor lethal after a short continuous time on it.
    /// Leaving the ground, even briefly, resets the timer.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController), typeof(CharacterController))]
    public sealed class GroundHazardRespawn : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float maximumGroundTime = 3f;
        [Header("Respawn Door")]
        [Tooltip("The fixed door used after a Hot Room death. Set to Door .004 in Stage 2.")]
        [SerializeField] private Transform respawnDoor;
        [SerializeField] private string respawnDoorName = "Door .004";
        [Tooltip("Clearance from the closed door to the player's capsule, in world units.")]
        [SerializeField, Min(0f)] private float entranceRespawnOffset = 0.75f;

        [Header("Timer Appearance")]
        [SerializeField] private Font timerFont;
        [SerializeField] private string timerHeading = "HOT ROOM";
        [SerializeField] private Color timerPanelColor = new Color(0.055f, 0.065f, 0.08f, 0.95f);
        [SerializeField] private Color timerAccentColor = new Color(1f, 0.58f, 0.24f, 1f);
        [SerializeField] private Color timerDangerColor = new Color(1f, 0.2f, 0.15f, 1f);
        [SerializeField] private Color timerValueColor = Color.white;
        [SerializeField] private Color timerInstructionColor = new Color(0.83f, 0.85f, 0.88f);
        [SerializeField, Min(0f)] private float timerTopMargin = 72f;

        [Header("Timer Custom Artwork (Optional)")]
        [Tooltip("Your panel artwork, drawn behind the live text. Design for 400 x 116 pixels; leave empty for the default panel.")]
        [SerializeField] private Texture2D timerPanelArtwork;
        [Tooltip("Your progress-bar background, drawn in a 360 x 6 pixel area. Transparent PNGs are supported.")]
        [SerializeField] private Texture2D timerBarTrackArtwork;
        [Tooltip("Your full progress bar. It is revealed from left to right and cropped as time runs out, without stretching the design.")]
        [SerializeField] private Texture2D timerBarFillArtwork;
        [Tooltip("Apply the accent/danger colour to custom bar-fill artwork. Leave off to preserve your artwork's original colours.")]
        [SerializeField] private bool tintTimerBarArtwork;

        private FirstPersonController player;
        private RoomMechanicVolume iceRoom;
        private float groundTime;
        private float respawnMessageUntil;
        private string respawnMessage = "The lava got you. Try again!";
        private Bounds closedDoorBounds;
        private Vector3 doorRespawnPosition;
        private Quaternion doorRespawnRotation;
        private bool hasDoorRespawn;
        private bool reportedMissingDoor;
        private GUIStyle timerHeadingStyle;
        private GUIStyle timerValueStyle;
        private GUIStyle timerStatusStyle;
        private GUIStyle timerInstructionStyle;

        private void Awake()
        {
            player = GetComponent<FirstPersonController>();
        }

        private void Start()
        {
            CacheDoorRespawn();
        }

        private void Update()
        {
            if (player == null || !player.isActiveAndEnabled) return;
            if (iceRoom == null) iceRoom = FindIceRoom();
            if (!hasDoorRespawn) CacheDoorRespawn();

            if (player.CurrentMechanic != RoomMechanic.Hot || !player.IsGrounded)
            {
                groundTime = 0f;
                return;
            }

            groundTime += Time.deltaTime;
            if (groundTime >= maximumGroundTime) RespawnAtEntrance();
        }

        private void RespawnAtEntrance()
        {
            TryRespawnAtDoor("The lava got you. Try again!");
        }

        /// <summary>Shared death checkpoint for room hazards and enemy catches.</summary>
        public bool TryRespawnAtDoor(string message)
        {
            groundTime = 0f;
            if (!hasDoorRespawn && !CacheDoorRespawn()) return false;

            player.RespawnAt(GetDoorFloorPosition(), doorRespawnRotation, iceRoom);
            Physics.SyncTransforms();
            respawnMessage = message;
            respawnMessageUntil = Time.time + 1.5f;
            return true;
        }

        private bool CacheDoorRespawn()
        {
            if (iceRoom == null) iceRoom = FindIceRoom();
            Collider iceCollider = iceRoom != null ? iceRoom.GetComponent<Collider>() : null;
            if (iceCollider == null) return false;

            // The explicit scene reference distinguishes the two Door .004
            // models. Name lookup also supports older scenes and live reloads.
            if (respawnDoor == null)
            {
                float nearest = float.PositiveInfinity;
                foreach (Renderer candidate in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (!string.Equals(candidate.name, respawnDoorName, System.StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(candidate.name, respawnDoorName + " Panel", System.StringComparison.OrdinalIgnoreCase)) continue;
                    float distance = (candidate.bounds.center - iceCollider.bounds.center).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    respawnDoor = candidate.transform;
                }
            }

            Renderer doorRenderer = respawnDoor != null ? respawnDoor.GetComponent<Renderer>() : null;
            if (doorRenderer == null && respawnDoor != null) doorRenderer = respawnDoor.GetComponentInChildren<Renderer>();
            if (doorRenderer == null)
            {
                if (!reportedMissingDoor)
                    Debug.LogWarning("Hot Room respawn door not found. Assign Door .004 to Respawn Door on the player.", this);
                reportedMissingDoor = true;
                return false;
            }

            // Capture the doorway before its panel swings. Respawning must not
            // move with the open door or with the player's route into the room.
            Physics.SyncTransforms();
            closedDoorBounds = doorRenderer.bounds;
            bool widthAlongX = closedDoorBounds.size.x >= closedDoorBounds.size.z;
            Vector3 safeSide = widthAlongX ? Vector3.forward : Vector3.right;
            if (Vector3.Dot(iceCollider.bounds.center - closedDoorBounds.center, safeSide) < 0f) safeSide = -safeSide;
            float halfThickness = widthAlongX ? closedDoorBounds.extents.z : closedDoorBounds.extents.x;
            float radius = GetComponent<CharacterController>().radius;
            doorRespawnPosition = closedDoorBounds.center + safeSide * (halfThickness + radius + entranceRespawnOffset);
            doorRespawnPosition.y = closedDoorBounds.min.y + 0.08f;
            doorRespawnRotation = Quaternion.LookRotation(-safeSide, Vector3.up);
            hasDoorRespawn = true;
            return true;
        }

        private static RoomMechanicVolume FindIceRoom()
        {
            foreach (RoomMechanicVolume room in FindObjectsByType<RoomMechanicVolume>(FindObjectsSortMode.None))
            {
                if (room.Mechanic == RoomMechanic.Ice) return room;
            }
            return null;
        }

        private Vector3 GetDoorFloorPosition()
        {
            Vector3 origin = new Vector3(doorRespawnPosition.x, closedDoorBounds.center.y, doorRespawnPosition.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 50f, ~0, QueryTriggerInteraction.Ignore);

            RaycastHit floorHit = default;
            bool foundFloor = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<DoorOutward>() != null ||
                    hit.normal.y < 0.6f || hit.point.y >= origin.y - 0.02f || IsRoof(hit.collider)) continue;
                if (!foundFloor || hit.point.y > floorHit.point.y)
                {
                    floorHit = hit;
                    foundFloor = true;
                }
            }

            Vector3 position = doorRespawnPosition;
            if (foundFloor) position.y = floorHit.point.y + 0.08f;
            else position = GetOrCreateRespawnPlatform(position, closedDoorBounds.min.y);
            return position;
        }

        private static Vector3 GetOrCreateRespawnPlatform(Vector3 roomCenter, float floorY)
        {
            const string platformName = "Door .004 Respawn Platform";
            GameObject platform = GameObject.Find(platformName);
            if (platform == null)
            {
                platform = new GameObject(platformName);
                platform.transform.position = new Vector3(roomCenter.x, floorY - 0.05f, roomCenter.z);
                BoxCollider collider = platform.AddComponent<BoxCollider>();
                collider.size = new Vector3(2f, 0.1f, 2f);
            }

            Collider platformCollider = platform.GetComponent<Collider>();
            return new Vector3(roomCenter.x, platformCollider.bounds.max.y + 0.08f, roomCenter.z);
        }

        private static bool IsRoof(Collider collider)
        {
            for (Transform current = collider != null ? collider.transform : null; current != null; current = current.parent)
            {
                string name = current.name.ToLowerInvariant();
                if (name.Contains("roof") || name.Contains("ceiling")) return true;
            }
            return false;
        }

        private void OnGUI()
        {
            if (player == null || !player.isActiveAndEnabled) return;
            bool showingRespawn = Time.time < respawnMessageUntil;
            if (!showingRespawn && player.CurrentMechanic != RoomMechanic.Hot) return;

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                float scale = RoomHudGraphics.Scale;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                float canvasWidth = Screen.width / scale;
                Rect panel = new Rect((canvasWidth - 400f) * 0.5f, timerTopMargin, 400f, showingRespawn ? 94f : 116f);
                float remaining = Mathf.Max(0f, maximumGroundTime - groundTime);
                float fraction = Mathf.Clamp01(remaining / Mathf.Max(0.5f, maximumGroundTime));
                bool airborne = !player.IsGrounded;
                bool urgent = !showingRespawn && !airborne && remaining <= Mathf.Min(1f, maximumGroundTime * 0.4f);
                Color accent = urgent ? timerDangerColor : timerAccentColor;
                if (urgent)
                    accent = Color.Lerp(timerDangerColor, timerAccentColor, (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.15f);
                if (timerPanelArtwork != null)
                    DrawTimerArtwork(panel, timerPanelArtwork, Color.white);
                else
                    RoomHudGraphics.Panel(panel, timerPanelColor, accent);
                EnsureTimerStyles(accent);
                GUI.Label(new Rect(panel.x + 20f, panel.y + 13f, showingRespawn ? 360f : 160f, 24f),
                    showingRespawn ? "BACK BY " + respawnDoorName.ToUpperInvariant() : timerHeading, timerHeadingStyle);

                if (showingRespawn)
                {
                    GUI.Label(new Rect(panel.x + 20f, panel.y + 44f, 360f, 34f),
                        respawnMessage, timerInstructionStyle);
                    return;
                }

                GUI.Label(new Rect(panel.x + 190f, panel.y + 15f, 190f, 20f),
                    airborne ? "AIRBORNE / RESET" : urgent ? "JUMP NOW" : "FLOOR TIMER", timerStatusStyle);
                GUI.Label(new Rect(panel.x + 20f, panel.y + 39f, 180f, 46f), remaining.ToString("0.0") + "s", timerValueStyle);
                GUI.Label(new Rect(panel.x + 210f, panel.y + 48f, 170f, 28f), "JUMP TO RESET", timerInstructionStyle);
                Rect track = new Rect(panel.x + 20f, panel.y + 96f, 360f, 6f);
                if (timerBarTrackArtwork != null)
                    DrawTimerArtwork(track, timerBarTrackArtwork, Color.white);
                else
                    RoomHudGraphics.Fill(track, new Color(1f, 1f, 1f, 0.1f));

                if (timerBarFillArtwork != null)
                    DrawTimerArtwork(track, timerBarFillArtwork, tintTimerBarArtwork ? accent : Color.white, fraction);
                else
                    RoomHudGraphics.Fill(new Rect(track.x, track.y, track.width * fraction, track.height), accent);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
            }
        }

        private static void DrawTimerArtwork(Rect rect, Texture2D artwork, Color tint, float fraction = 1f)
        {
            fraction = Mathf.Clamp01(fraction);
            if (fraction <= 0f) return;
            Color previousColor = GUI.color;
            GUI.color = tint;
            rect.width *= fraction;
            GUI.DrawTextureWithTexCoords(rect, artwork, new Rect(0f, 0f, fraction, 1f), true);
            GUI.color = previousColor;
        }

        private void EnsureTimerStyles(Color accent)
        {
            RoomHudGraphics.LabelStyle(ref timerHeadingStyle, 14, accent, TextAnchor.MiddleLeft, timerFont, true);
            RoomHudGraphics.LabelStyle(ref timerValueStyle, 34, timerValueColor, TextAnchor.MiddleLeft, timerFont, true);
            RoomHudGraphics.LabelStyle(ref timerStatusStyle, 12, accent, TextAnchor.MiddleRight, timerFont, true);
            RoomHudGraphics.LabelStyle(ref timerInstructionStyle, 14, timerInstructionColor, TextAnchor.MiddleLeft, timerFont);
        }
    }
}
