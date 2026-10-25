using UnityEngine;

namespace Shiftscape
{
    /// <summary>Third-person orbit camera that follows the player's current "up" (gravity flips rotate the camera too),
    /// with collision pull-in, screen shake, and a look-back when the Void is close.</summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }
        public Transform target;
        public float distance = 6.5f;
        public float height = 1.2f;
        public Camera Cam { get; private set; }

        float yaw, pitch = 18f, shake;
        Vector3 shakeOffset;
        Vector3 smoothUp = Vector3.up;
        float curDist;
        int mask;

        void Awake()
        {
            I = this;
            Cam = GetComponent<Camera>();
            if (Cam == null) Cam = gameObject.AddComponent<Camera>();
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 400f;
            Cam.fieldOfView = 62f;
            curDist = distance;
            mask = ~(1 << PlayerMotor.PlayerLayer);
            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
        }

        public void Snap(Vector3 forward)
        {
            smoothUp = Vector3.up;
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            pitch = 18f;
            curDist = distance;
            LateUpdate();
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        void LateUpdate()
        {
            if (target == null) return;
            var motor = PlayerMotor.I;
            Vector3 up = motor != null ? motor.Up : Vector3.up;
            smoothUp = Vector3.Slerp(smoothUp, up, Time.deltaTime * 5f).normalized;

            yaw += GameInput.Look.x;
            pitch = Mathf.Clamp(pitch - GameInput.Look.y, -35f, 70f);

            // Build a basis around the smoothed up vector.
            Vector3 refFwd = Mathf.Abs(Vector3.Dot(smoothUp, Vector3.forward)) < 0.9f ? Vector3.forward : Vector3.right;
            Quaternion basis = Quaternion.LookRotation(Vector3.ProjectOnPlane(refFwd, smoothUp).normalized, smoothUp);
            Quaternion rot = basis * Quaternion.Euler(pitch, yaw, 0);

            Vector3 pivot = target.position + smoothUp * height;
            Vector3 back = rot * Vector3.back;
            float wanted = distance;
            if (Physics.SphereCast(pivot, 0.3f, back, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(1.2f, hit.distance - 0.1f);
            curDist = wanted < curDist ? wanted : Mathf.Lerp(curDist, wanted, Time.deltaTime * 4f);

            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 2.5f);
            shakeOffset = shake > 0 ? Random.insideUnitSphere * shake * 0.25f : Vector3.zero;

            transform.position = pivot + back * curDist + shakeOffset;
            transform.rotation = rot;

            // Subtle FOV kick when dashing / fast
            float speed = motor != null ? motor.Velocity.magnitude : 0f;
            Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, 62f + Mathf.Clamp((speed - 8f) * 1.2f, 0, 12f), Time.deltaTime * 6f);
        }
    }
}
