using System;
using UnityEngine;

namespace FPSStarter
{
    /// <summary>Colours primitives without creating a unique Material for every object in the editor.</summary>
    public static class VisualColor
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();

        public static void Set(Renderer target, Color value)
        {
            if (target == null) return;
            target.GetPropertyBlock(Properties);
            Properties.SetColor(BaseColor, value);
            Properties.SetColor(ColorProperty, value);
            target.SetPropertyBlock(Properties);
        }
    }

    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(GameObject interactor);
    }

    public interface IItemReceiver
    {
        string HeldItemPrompt { get; }
        bool TryPlace(CarryableObject item);
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarryableObject : MonoBehaviour
    {
        [SerializeField] private string itemName = "stone relic";
        [SerializeField] private string itemId = "relic";
        [SerializeField, Min(0.2f)] private float minimumHoldDistance = 0.5f;
        [SerializeField, Min(0.2f)] private float maximumHoldDistance = 2.5f;
        private Rigidbody body;
        private float holdDistance = 1f;

        public bool IsHeld { get; private set; }
        public bool IsPlaced { get; private set; }
        public string ItemName => itemName;
        public string ItemId => itemId;

        private void Awake() => body = GetComponent<Rigidbody>();

        public void Configure(string displayName, string identifier)
        {
            itemName = displayName;
            itemId = identifier;
            gameObject.name = displayName;
        }

        public void PickUp(Transform target)
        {
            if (IsHeld || IsPlaced || target == null) return;
            IsHeld = true;
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetParent(target, false);
            transform.localPosition = Vector3.forward * Mathf.Clamp(holdDistance, minimumHoldDistance, maximumHoldDistance);
            transform.localRotation = Quaternion.identity;
        }

        public void Drop()
        {
            if (!IsHeld) return;
            transform.SetParent(null, true);
            body.isKinematic = false;
            IsHeld = false;
        }

        public void Throw(Vector3 direction, float force)
        {
            if (!IsHeld) return;
            transform.SetParent(null, true);
            body.isKinematic = false;
            body.linearVelocity = direction.normalized * force;
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
            IsHeld = false;
        }

        public void Place(Transform mount)
        {
            if (!IsHeld || mount == null) return;
            IsHeld = false;
            IsPlaced = true;
            body.isKinematic = true;
            transform.SetParent(mount, false);
            transform.localPosition = Vector3.up * 0.12f;
            transform.localRotation = Quaternion.identity;
        }

        public void AdjustHoldDistance(float amount)
        {
            if (!IsHeld) return;
            holdDistance = Mathf.Clamp(holdDistance + amount, minimumHoldDistance, maximumHoldDistance);
            transform.localPosition = Vector3.forward * holdDistance;
        }

        public void RotateInHand(Vector2 mouseDelta, float sensitivity)
        {
            if (IsHeld) transform.localRotation = Quaternion.Euler(-mouseDelta.y * sensitivity, mouseDelta.x * sensitivity, 0f) * transform.localRotation;
        }
    }

    public sealed class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform door;
        [SerializeField] private float openAngle = 90f;
        [SerializeField, Min(0.05f)] private float openSpeed = 180f;
        private Quaternion closedRotation;
        private bool isOpen;
        public string Prompt => isOpen ? "[E] Close door" : "[E] Open door";

        private void Awake()
        {
            if (door == null) door = transform;
            closedRotation = door.localRotation;
        }

        private void Update()
        {
            Quaternion target = isOpen ? closedRotation * Quaternion.Euler(0f, openAngle, 0f) : closedRotation;
            door.localRotation = Quaternion.RotateTowards(door.localRotation, target, openSpeed * Time.deltaTime);
        }

        public void Interact(GameObject interactor) => isOpen = !isOpen;
    }

    public sealed class SequencePuzzle : MonoBehaviour
    {
        private SequencePlate[] plates;
        private int[] order;
        private int progress;
        private bool complete;
        private GameObject reward;

        public void Configure(SequencePlate[] puzzlePlates, int[] solution, GameObject completionReward)
        {
            plates = puzzlePlates;
            order = solution;
            reward = completionReward;
            if (reward != null) reward.SetActive(false);
        }

        public void Press(int plateIndex)
        {
            if (complete || plates == null || order == null) return;
            if (plateIndex == order[progress])
            {
                plates[plateIndex].SetState(SequencePlate.State.Correct);
                progress++;
                if (progress == order.Length)
                {
                    complete = true;
                    if (reward != null) reward.SetActive(true);
                }
                return;
            }

            foreach (SequencePlate plate in plates) plate.SetState(SequencePlate.State.Wrong);
            Invoke(nameof(ResetPuzzle), 0.75f);
        }

        private void ResetPuzzle()
        {
            progress = 0;
            foreach (SequencePlate plate in plates) plate.SetState(SequencePlate.State.Neutral);
        }
    }

    public sealed class SequencePlate : MonoBehaviour, IInteractable
    {
        public enum State { Neutral, Correct, Wrong }
        private SequencePuzzle puzzle;
        private int index;
        private Renderer plateRenderer;
        private Color neutral;

        public string Prompt => "[E] Press picture plate";

        public void Configure(SequencePuzzle owner, int plateIndex, Color baseColour)
        {
            puzzle = owner;
            index = plateIndex;
            plateRenderer = GetComponent<Renderer>();
            neutral = baseColour;
            SetState(State.Neutral);
        }

        public void Interact(GameObject interactor) => puzzle?.Press(index);

        public void SetState(State state)
        {
            if (plateRenderer == null) plateRenderer = GetComponent<Renderer>();
            if (plateRenderer == null) return;
            VisualColor.Set(plateRenderer, state == State.Correct ? new Color(0.12f, 0.8f, 0.18f) :
                                               state == State.Wrong ? new Color(0.85f, 0.08f, 0.06f) : neutral);
        }
    }

    public sealed class RotatingTablePuzzle : MonoBehaviour
    {
        private RotatingTier[] tiers;
        private int activeTier;
        private bool complete;
        private SecretDoor secretDoor;

        public void Configure(RotatingTier[] puzzleTiers, SecretDoor door)
        {
            tiers = puzzleTiers;
            secretDoor = door;
        }

        public bool IsTierActive(int tier) => !complete && tier == activeTier;

        public void TryRotate(RotatingTier tier)
        {
            if (complete || tiers == null || tier != tiers[activeTier]) return;
            if (tier.RotateStep())
            {
                activeTier++;
                if (activeTier == tiers.Length)
                {
                    complete = true;
                    secretDoor?.Open();
                }
            }
        }
    }

    public sealed class RotatingTier : MonoBehaviour, IInteractable
    {
        private RotatingTablePuzzle puzzle;
        private int tierIndex;
        private float targetAngle;
        private Renderer ringRenderer;
        private bool locked;

        public string Prompt => locked ? "Plate aligned" : puzzle != null && puzzle.IsTierActive(tierIndex) ? "[E] Rotate this table plate" : "Align the lower plate first";

        public void Configure(RotatingTablePuzzle owner, int index, float target, Renderer renderer)
        {
            puzzle = owner;
            tierIndex = index;
            targetAngle = target;
            ringRenderer = renderer;
        }

        public void Interact(GameObject interactor) => puzzle?.TryRotate(this);

        public bool RotateStep()
        {
            if (locked) return false;
            transform.Rotate(0f, 45f, 0f, Space.Self);
            float angle = Mathf.Repeat(transform.localEulerAngles.y, 360f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, targetAngle)) > 1f) return false;
            locked = true;
            VisualColor.Set(ringRenderer, new Color(0.12f, 0.8f, 0.18f));
            return true;
        }
    }

    public sealed class SecretDoor : MonoBehaviour
    {
        private Transform panel;
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool open;

        public void Configure(Transform slidingPanel, Vector3 openOffset)
        {
            panel = slidingPanel;
            closedPosition = panel.localPosition;
            openPosition = closedPosition + openOffset;
        }

        public void Open() => open = true;

        private void Update()
        {
            if (open && panel != null) panel.localPosition = Vector3.MoveTowards(panel.localPosition, openPosition, 2.4f * Time.deltaTime);
        }
    }

    public sealed class StatuePuzzle : MonoBehaviour
    {
        [Tooltip("Drag all StatuePart children here. Leave empty to auto-collect child StatueParts on Start.")]
        [SerializeField] private StatuePart[] parts;

        [Tooltip("Optional GameObject to activate when all parts are aligned (e.g. a reward item or door trigger).")]
        [SerializeField] private GameObject completionReward;

        [Tooltip("Optional message shown on screen when the puzzle is solved. Requires a UI text element listening to OnPuzzleSolved.")]
        [SerializeField] private string solvedMessage = "The statue is aligned!";

        private int needed;
        private int solved;

        // Raised when every part is correctly aligned.
        public event System.Action OnPuzzleSolved;

        private void Start()
        {
            if (parts == null || parts.Length == 0)
                parts = GetComponentsInChildren<StatuePart>();

            needed = parts.Length;

            foreach (StatuePart part in parts)
                part.RegisterPuzzle(this);

            if (completionReward != null) completionReward.SetActive(false);
        }

        /// <summary>Called by runtime builders that construct puzzles in code.</summary>
        public void Configure(int partCount, GameObject reward)
        {
            needed = partCount;
            completionReward = reward;
            if (completionReward != null) completionReward.SetActive(false);
        }

        public void PartSolved()
        {
            solved++;
            if (solved < needed) return;
            if (completionReward != null) completionReward.SetActive(true);
            Debug.Log($"[StatuePuzzle] {solvedMessage}");
            OnPuzzleSolved?.Invoke();
        }
    }

    public sealed class StatuePart : MonoBehaviour, IInteractable
    {
        [Tooltip("Display name shown in the interaction prompt (e.g. 'Head', 'Torso', 'Base').")]
        [SerializeField] private string partName = "statue part";

        [Tooltip("The Y-axis angle (local) the part must be rotated to in order to be considered aligned.")]
        [SerializeField] private float targetAngle = 0f;

        [Tooltip("How many degrees to rotate each time the player interacts.")]
        [SerializeField, Range(15f, 180f)] private float rotationStep = 90f;

        [Tooltip("The renderer to recolour green when this part is solved. Auto-resolved from this GameObject if left blank.")]
        [SerializeField] private Renderer partRenderer;

        [Tooltip("The StatuePuzzle this part belongs to. Auto-resolved from parent if left blank.")]
        [SerializeField] private StatuePuzzle puzzle;

        private bool solved;

        public string Prompt => solved ? partName + " aligned" : "[E] Rotate " + partName;

        private void Awake()
        {
            if (partRenderer == null) partRenderer = GetComponent<Renderer>();
            if (puzzle == null) puzzle = GetComponentInParent<StatuePuzzle>();
        }

        /// <summary>Called by StatuePuzzle.Start() so inspector-placed parts are linked automatically.</summary>
        public void RegisterPuzzle(StatuePuzzle owner)
        {
            if (puzzle == null) puzzle = owner;
        }

        /// <summary>Called by runtime builders that construct puzzles in code.</summary>
        public void Configure(StatuePuzzle owner, string displayName, float target, Renderer renderer)
        {
            puzzle = owner;
            partName = displayName;
            targetAngle = target;
            partRenderer = renderer != null ? renderer : GetComponent<Renderer>();
        }

        public void Interact(GameObject interactor)
        {
            if (solved) return;
            transform.Rotate(0f, rotationStep, 0f, Space.Self);
            if (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.y, targetAngle)) > 1f) return;
            solved = true;
            VisualColor.Set(partRenderer, new Color(0.12f, 0.8f, 0.18f));
            puzzle?.PartSolved();
        }
    }

    public sealed class PlacementSlot : MonoBehaviour, IItemReceiver
    {
        private string requiredItemId;
        private Transform mount;
        private Renderer plateRenderer;
        private PlacementTable table;
        private bool filled;

        public string HeldItemPrompt => filled ? "This slot is filled" : "[E] Place item in this slot";

        public void Configure(string requiredId, Transform itemMount, Renderer plateRenderer, PlacementTable owner)
        {
            requiredItemId = requiredId;
            mount = itemMount;
            this.plateRenderer = plateRenderer;
            table = owner;
        }

        public bool TryPlace(CarryableObject item)
        {
            if (filled || item == null || item.ItemId != requiredItemId) return false;
            item.Place(mount);
            filled = true;
            VisualColor.Set(plateRenderer, new Color(0.12f, 0.8f, 0.18f));
            table?.SlotFilled();
            return true;
        }
    }

    public sealed class PlacementTable : MonoBehaviour
    {
        private int filled;
        private int total;
        public void Configure(int slotCount) => total = slotCount;
        public void SlotFilled() => filled++;
        public string Progress => filled + " / " + total + " relics placed";
    }
}
