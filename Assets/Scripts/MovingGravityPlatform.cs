using UnityEngine;

namespace FPSStarter
{
    [DefaultExecutionOrder(-100)]
    public sealed class MovingGravityPlatform : MonoBehaviour
    {
        public Vector3 travel = new Vector3(0.7f, 0f, 0f);
        public float period = 3.5f;
        public RoomMechanicVolume room;
        public Vector3 FrameDelta { get; private set; }
        private Vector3 origin;
        private float elapsed;
        void Awake() => origin = transform.position;
        void Update()
        {
            FrameDelta = Vector3.zero;
            if (Hint.IsReading || room == null || !room.EntranceHintCompleted) return;
            elapsed += Time.deltaTime;
            Vector3 next = origin + travel * Mathf.Sin(elapsed * Mathf.PI * 2f / Mathf.Max(1f, period));
            FrameDelta = next - transform.position;
            transform.position = next;
        }
    }
}
