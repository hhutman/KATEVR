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
        public Transform doorHinge;     // rotates about Y to open the door
        public Renderer fade;           // black quad in front of the eyes

        [Header("Door outline (appears when the palm touches the wall)")]
        public Transform[] outlineSides;  // pivots at floor level; grow upward
        public Transform outlineTop;      // pivot at centre of the lintel; grows outward
        public float outlineTime = 0.3f; // a quick flash of light around the edges
        public float doorHeight = 2.2f, doorWidth = 1.2f, seamWidth = 0.025f;

        [Header("Where the palm lands")]
        public Vector3 palmPoint;       // on the door's outside face
        public Vector3 palmFacing = Vector3.right; // direction the palm pushes (into the door)

        [Header("Timing (seconds)")]
        public float pauseBeforeReach = 0.8f;
        public float reachTime = 1.0f;
        public float holdTime = 1.1f;
        public float withdrawTime = 0.8f;
        public float doorTime = 5f;
        public float fadeDelay = 1.8f;
        public float fadeTime = 3f;

        [Header("Light inside")]
        public Light doorLight;
        public float doorLightIntensity = 12f;
        public Color fadeColor = Color.white; // the light floods everything at the end

        [Header("Door")]
        public float doorOpenAngle = 70f;

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
        }

        void SetFade(float a)
        {
            if (!fade) return;
            fade.enabled = a > 0.001f;
            fade.GetPropertyBlock(_block);
            _block.SetColor(ColorId, new Color(fadeColor.r, fadeColor.g, fadeColor.b, a));
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

            // Only the glove: it rises from below the view, presses flat, then drops away. No arm.
            Vector3 rest = rig.TransformPoint(new Vector3(0.12f, 0.95f, 0.35f)); // out of sight below
            Vector3 press = palmPoint - palmFacing.normalized * 0.025f;
            Quaternion restRot = Quaternion.LookRotation(rig.forward, Vector3.up) * Quaternion.Euler(-35f, 0f, 0f);
            Quaternion pressRot = Quaternion.LookRotation(palmFacing.normalized, Vector3.up);

            float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a / reachTime));                    // rise and touch
            float down = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((a - reachTime - holdTime) / withdrawTime)); // drop away
            float k = up * (1f - down);
            Vector3 ctrl = Vector3.Lerp(rest, press, 0.5f) - palmFacing.normalized * 0.08f;
            Vector3 pos = Vector3.Lerp(Vector3.Lerp(rest, ctrl, k), Vector3.Lerp(ctrl, press, k), k);
            Quaternion rot = Quaternion.Slerp(restRot, pressRot, Mathf.Clamp01(k * 1.3f));
            bool handVisible = up > 0f && down < 1f;
            Show(handVisible);
            if (handVisible)
            {
                hand.position = pos;
                hand.rotation = rot;
            }

            // On contact, the outline of a door draws itself into the wall.
            float ok = Mathf.Clamp01((a - reachTime + 0.1f) / outlineTime);
            float sides = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ok * 1.5f));
            float top = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ok - 0.6f) / 0.4f));
            if (outlineSides != null)
                foreach (var side in outlineSides)
                    if (side) side.localScale = new Vector3(0.02f, doorHeight * sides, seamWidth);
            if (outlineTop) outlineTop.localScale = new Vector3(0.02f, seamWidth, doorWidth * top);

            // As the hand drops away, the door begins to open inward.
            float doorStart = reachTime + holdTime + withdrawTime * 0.3f;
            float dk = Mathf.Clamp01((a - doorStart) / doorTime);
            float open = dk * dk * (3f - 2f * dk);
            open = Mathf.Lerp(open * open, open, open); // slow to start, then gives way
            if (doorHinge) doorHinge.localRotation = Quaternion.Euler(0f, doorOpenAngle * open, 0f);
            if (doorLight) doorLight.intensity = doorLightIntensity * Mathf.Clamp01(dk * 2.5f);

            float f = Mathf.Clamp01((a - doorStart - fadeDelay) / fadeTime);
            SetFade(Mathf.SmoothStep(0f, 1f, f));
        }
    }
}
