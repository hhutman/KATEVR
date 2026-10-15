using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// The last beat: when Kate stops at the door, her gloved hand rises into view and presses flat
    /// against it; the door begins to swing inward into darkness; the scene fades to black.
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        public GlidePath glide;
        public Transform hand;          // glove; its local +Z is the palm's facing, +Y the fingers
        public Transform sleeve;        // stretched between shoulder and wrist every frame
        public Transform doorHinge;     // rotates about Y to open the door
        public Renderer fade;           // black quad in front of the eyes

        [Header("Where the palm lands")]
        public Vector3 palmPoint;       // on the door's outside face
        public Vector3 palmFacing = Vector3.right; // direction the palm pushes (into the door)

        [Header("Timing (seconds)")]
        public float pauseBeforeReach = 0.8f;
        public float reachTime = 1.9f;
        public float holdTime = 1.3f;
        public float doorTime = 5f;
        public float fadeDelay = 1.5f;
        public float fadeTime = 3f;

        [Header("Door")]
        public float doorOpenAngle = -70f;

        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock _block;
        float _t = -1f;

        void Start()
        {
            _block = new MaterialPropertyBlock();
            Show(false);
            SetFade(0f);
        }

        void Show(bool on)
        {
            if (hand) hand.gameObject.SetActive(on);
            if (sleeve) sleeve.gameObject.SetActive(on);
        }

        void SetFade(float a)
        {
            if (!fade) return;
            fade.enabled = a > 0.001f;
            fade.GetPropertyBlock(_block);
            _block.SetColor(ColorId, new Color(0f, 0f, 0f, a));
            fade.SetPropertyBlock(_block);
        }

        void Update()
        {
            if (_t < 0f)
            {
                if (glide == null || !glide.IsFinished) return;
                _t = 0f;
            }
            _t += Time.deltaTime;

            var rig = glide.transform;
            float a = _t - pauseBeforeReach;
            if (a < 0f) return;
            Show(true);

            Vector3 shoulder = rig.TransformPoint(new Vector3(0.2f, 1.38f, 0.05f));
            Vector3 rest = rig.TransformPoint(new Vector3(0.24f, 0.8f, 0.2f));
            Vector3 press = palmPoint - palmFacing.normalized * 0.025f;

            // Reach: a gentle arc up from the hip to the door.
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a / reachTime));
            Vector3 ctrl = Vector3.Lerp(rest, press, 0.5f) + Vector3.up * 0.12f - palmFacing.normalized * 0.12f;
            Vector3 pos = Vector3.Lerp(Vector3.Lerp(rest, ctrl, k), Vector3.Lerp(ctrl, press, k), k);
            Quaternion restRot = Quaternion.LookRotation(Vector3.down + rig.forward * 0.4f, rig.forward);
            Quaternion pressRot = Quaternion.LookRotation(palmFacing.normalized, Vector3.up);
            Quaternion rot = Quaternion.Slerp(restRot, pressRot, k);

            // The door begins to open; the hand follows it a little, then lets go.
            float doorStart = reachTime + holdTime;
            float dk = Mathf.Clamp01((a - doorStart) / doorTime);
            float open = dk * dk * (3f - 2f * dk);
            open = Mathf.Lerp(open * open, open, open); // slow to start, then gives way
            if (doorHinge) doorHinge.localRotation = Quaternion.Euler(0f, doorOpenAngle * open, 0f);
            pos += palmFacing.normalized * Mathf.Min(open * 0.6f, 0.12f);

            hand.position = pos;
            hand.rotation = rot;

            if (sleeve)
            {
                Vector3 wrist = hand.position - hand.up * 0.08f;
                Vector3 d = wrist - shoulder;
                sleeve.position = (shoulder + wrist) * 0.5f;
                sleeve.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                Vector3 s = sleeve.localScale;
                sleeve.localScale = new Vector3(s.x, s.y, d.magnitude);
            }

            float f = Mathf.Clamp01((a - doorStart - fadeDelay) / fadeTime);
            SetFade(Mathf.SmoothStep(0f, 1f, f));
        }
    }
}
