using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// Kate's left arm in the hazmat suit. Something else (EndingSequence) moves the glove;
    /// this keeps the sleeve attached to it: shoulder fixed to her body, elbow bending down and
    /// out to the left (so the sleeve comes in from the bottom-left of the view), the forearm sleeve
    /// ending in its elastic band around the glove's cuff. It also poses the fingers:
    /// curl 1 = relaxed, naturally curved; curl 0 = flat against the wall.
    /// </summary>
    public class KateArm : MonoBehaviour
    {
        public Transform body;      // Kate's rig; the shoulder hangs off this
        public Transform hand;      // glove root: local +Z = palm facing, +Y = fingers
        public Transform upperArm, forearm, elbow, cuff;

        [Header("Arm")]
        public Vector3 shoulderLocal = new Vector3(-0.19f, 1.42f, -0.02f);
        public Vector3 elbowHint = new Vector3(-0.8f, -1f, -0.2f);  // body-local: where the elbow points
        public float upperLength = 0.33f;
        public float foreLength = 0.31f;  // elbow to the end of the sleeve
        public Vector3 wristLocal = new Vector3(0f, -0.052f, 0f); // hand-local: where glove meets cuff
        [Range(0, 1)] public float cuffBend = 0.3f; // how far the cuff turns toward the forearm
        public float sleeveTuck = 0.026f; // how far down the cuff the sleeve's band sits (hand-local metres)

        [Header("Fingers")]
        [Range(0, 1)] public float curl = 0.55f;
        [Range(0, 1)] public float spread = 0f;
        public Transform[] fingers;  // 4 fingers x 3 joints: index..pinky, knuckle..tip
        public Transform[] thumb;    // 3 joints, base..tip
        public float thumbSide = 1f; // +1: thumb on the hand's +X side (left hand, palm away)

        static readonly float[] JointCurl = { 30f, 40f, 24f };       // degrees at curl = 1
        static readonly float[] FingerCurl = { 0.8f, 1f, 1.12f, 1.3f }; // the little finger curls most
        static readonly float[] FingerSpread = { 7f, 2f, -2.5f, -8f };  // extra splay when pressing

        Quaternion[] _fingerRest, _thumbRest;

        void Awake()
        {
            _fingerRest = Rest(fingers);
            _thumbRest = Rest(thumb);
        }

        static Quaternion[] Rest(Transform[] joints)
        {
            if (joints == null) return new Quaternion[0];
            var r = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) r[i] = joints[i] ? joints[i].localRotation : Quaternion.identity;
            return r;
        }

        void LateUpdate()
        {
            PoseFingers();
            PoseArm();
        }

        void PoseFingers()
        {
            if (fingers != null)
                for (int f = 0; f < 4; f++)
                for (int j = 0; j < 3; j++)
                {
                    int k = f * 3 + j;
                    if (k >= fingers.Length || !fingers[k]) continue;
                    float bend = JointCurl[j] * FingerCurl[f] * curl;
                    float splay = j == 0 ? -thumbSide * FingerSpread[f] * spread : 0f;
                    fingers[k].localRotation = _fingerRest[k] * Quaternion.Euler(bend, 0f, splay);
                }
            if (thumb != null)
                for (int j = 0; j < thumb.Length; j++)
                {
                    if (!thumb[j]) continue;
                    float bend = (j == 0 ? 12f : j == 1 ? 22f : 28f) * curl;
                    float open = j == 0 ? -thumbSide * 8f * spread : 0f;
                    thumb[j].localRotation = _thumbRest[j] * Quaternion.Euler(bend, 0f, open);
                }
        }

        void PoseArm()
        {
            if (!body || !hand) return;
            float s = hand.lossyScale.x;
            Vector3 joint = hand.TransformPoint(wristLocal);
            Vector3 shoulder = body.TransformPoint(shoulderLocal);
            Vector3 hint = body.TransformDirection(elbowHint).normalized;

            // First guess at the cuff's direction, then refine once with the forearm's direction.
            Vector3 cuffAxis = hand.up;
            Vector3 wrist = joint - cuffAxis * sleeveTuck * s;
            Vector3 elbowPos = SolveElbow(ref shoulder, wrist, upperLength, foreLength, hint);
            Vector3 foreDir = (wrist - elbowPos).normalized;
            cuffAxis = Vector3.Slerp(hand.up, foreDir, cuffBend).normalized;
            wrist = joint - cuffAxis * sleeveTuck * s;
            elbowPos = SolveElbow(ref shoulder, wrist, upperLength, foreLength, hint);

            if (cuff)
            {
                cuff.position = joint;
                cuff.rotation = Quaternion.FromToRotation(hand.up, cuffAxis) * hand.rotation;
            }
            if (upperArm)
            {
                upperArm.position = shoulder;
                upperArm.rotation = Aim(elbowPos - shoulder, hint);
            }
            if (forearm)
            {
                forearm.position = elbowPos;
                forearm.rotation = Aim(wrist - elbowPos, hint);
            }
            if (elbow)
            {
                elbow.position = elbowPos;
                elbow.rotation = Aim(wrist - elbowPos, hint);
            }
        }

        // Local +Y along the bone; local -Z toward the elbow's outside... kept consistent so folds don't spin.
        static Quaternion Aim(Vector3 dir, Vector3 hint)
        {
            if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;
            Vector3 up = Vector3.ProjectOnPlane(hint, dir);
            if (up.sqrMagnitude < 1e-6f) up = Vector3.ProjectOnPlane(Vector3.up, dir);
            return Quaternion.LookRotation(dir.normalized, up.normalized) * Quaternion.Euler(90f, 0f, 0f);
        }

        // Two-bone reach. If the wrist is further than the arm can stretch, the shoulder leans in
        // (it's out of view), so the sleeve never visibly stretches or detaches.
        static Vector3 SolveElbow(ref Vector3 shoulder, Vector3 wrist, float a, float b, Vector3 hint)
        {
            Vector3 d = wrist - shoulder;
            float len = d.magnitude;
            Vector3 dir = len > 1e-5f ? d / len : Vector3.down;
            float maxLen = a + b - 0.003f;
            if (len > maxLen) { shoulder = wrist - dir * maxLen; len = maxLen; }
            len = Mathf.Max(len, Mathf.Abs(a - b) + 0.003f);
            float x = (a * a - b * b + len * len) / (2f * len);
            float h = Mathf.Sqrt(Mathf.Max(0f, a * a - x * x));
            Vector3 bend = Vector3.ProjectOnPlane(hint, dir);
            bend = bend.sqrMagnitude > 1e-6f ? bend.normalized : Vector3.down;
            return shoulder + dir * x + bend * h;
        }
    }
}
