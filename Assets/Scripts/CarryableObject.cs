using UnityEngine;

namespace FPSStarter
{
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
        public bool IsStage2Key => itemId == "key" || itemId.StartsWith("stage2-key-");

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Configure(string displayName, string identifier)
        {
            itemName = displayName;
            itemId = identifier;
            gameObject.name = displayName;
        }

        public void SetItemId(string identifier)
        {
            itemId = identifier;
        }

        public void PickUp(Transform target)
        {
            if (IsHeld || IsPlaced || target == null) return;

            IsHeld = true;
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            transform.SetParent(target, false);
            transform.localPosition = Vector3.forward *
                Mathf.Clamp(holdDistance, minimumHoldDistance, maximumHoldDistance);
            transform.localRotation = Quaternion.identity;
        }

        public void Drop()
        {
            if (!IsHeld) return;

            transform.SetParent(null, true);
            body.isKinematic = false;
            body.useGravity = true;
            IsHeld = false;
        }

        public void Throw(Vector3 direction, float force)
        {
            if (!IsHeld) return;

            transform.SetParent(null, true);
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = direction.normalized * force;
            body.angularVelocity = Random.insideUnitSphere * 8f;
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

            holdDistance = Mathf.Clamp(
                holdDistance + amount,
                minimumHoldDistance,
                maximumHoldDistance);

            transform.localPosition = Vector3.forward * holdDistance;
        }

        public void RotateInHand(Vector2 mouseDelta, float sensitivity)
        {
            if (!IsHeld) return;

            transform.localRotation =
                Quaternion.Euler(
                    -mouseDelta.y * sensitivity,
                    mouseDelta.x * sensitivity,
                    0f) * transform.localRotation;
        }
    }
}
