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
        public KateArm arm;             // sleeve + fingers that follow the glove
        public GameObject armRoot;      // whole arm (sleeve and glove), shown only during the reach

        [Header("Wall swap: solid until the touch, then the doorway takes its place")]
        public GameObject solidWall;
        public GameObject doorway;

        [Header("Door outline (a flash when the palm touches the wall)")]
        public Transform[] outlineSides;  // pivots at floor level; grow upward
        public Transform outlineTop;      // pivot at centre of the lintel; grows outward
        public float outlineTime = 0.15f; // how fast the flash appears
        public float flashHold = 0.3f;    // how long it shows
        public float flashFade = 0.25f;   // how fast it goes
        public float doorHeight = 2.2f, doorWidth = 1.2f, seamWidth = 0.025f;

        [Header("Where the palm lands")]
        public Vector3 palmPoint;       // on the door's outside face
        public Vector3 palmFacing = Vector3.right; // direction the palm pushes (into the door)

        [Header("Timing (seconds)")]
        public float pauseBeforeReach = 0f;
        [Tooltip("Seconds into the scene when the hand starts to rise (three beats after her last word). It also waits until Kate has reached the door.")]
        public float handStartTime = 37.2f;
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
            if (armRoot) armRoot.SetActive(on);
            else if (hand) hand.gameObject.SetActive(on);
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
                if (glide == null) return;
                bool arrived = glide.IsFinished || glide.RemainingDistance < 0.15f;
                if (!arrived || Time.timeSinceLevelLoad < handStartTime) return;
                _t = 0f;
            }
            _t += Time.deltaTime;

            var rig = glide.transform;
            float a = _t - pauseBeforeReach;
            if (a < 0f) return;

            // Her left arm rises from below the view (bottom-left), the palm presses flat, then drops away.
            Vector3 rest = rig.TransformPoint(new Vector3(-0.16f, 0.95f, 0.32f)); // out of sight below
            Vector3 press = palmPoint - palmFacing.normalized * 0.025f;
            Quaternion restRot = Quaternion.LookRotation(rig.forward, Vector3.up) * Quaternion.Euler(50f, 0f, 0f); // reaching: fingers forward
            Quaternion pressRot = Quaternion.LookRotation(palmFacing.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, -12f);

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
            if (arm)
            {
                // Relaxed, slightly curled while reaching; fingers flatten and spread on the wall.
                float flat = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, k));
                arm.curl = Mathf.Lerp(0.55f, 0.04f, flat);
                arm.spread = flat * 0.6f;
            }

            // On contact: the solid wall gives way to the doorway (they look identical),
            // and a thin line of light flashes around the door's edges, then goes.
            float sinceTouch = a - reachTime;
            if (sinceTouch >= 0f && doorway && !doorway.activeSelf)
            {
                doorway.SetActive(true);
                if (solidWall) solidWall.SetActive(false);
            }
            float flash = 0f;
            if (sinceTouch >= 0f)
            {
                if (sinceTouch < outlineTime) flash = sinceTouch / outlineTime;
                else if (sinceTouch < outlineTime + flashHold) flash = 1f;
                else flash = 1f - Mathf.Clamp01((sinceTouch - outlineTime - flashHold) / flashFade);
            }
            if (outlineSides != null)
                foreach (var side in outlineSides)
                    if (side) side.localScale = flash > 0f ? new Vector3(0.02f, doorHeight, seamWidth * flash) : Vector3.zero;
            if (outlineTop) outlineTop.localScale = flash > 0f ? new Vector3(0.02f, seamWidth * flash, doorWidth) : Vector3.zero;

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
