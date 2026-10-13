using System.Collections.Generic;
using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// Moves this object (Kate's rig) smoothly along a path of waypoints at a steady,
    /// comfortable speed, easing in at the start and out at the end.
    /// The rig turns gently to face the direction of travel; the viewer's head is free to look around.
    /// </summary>
    public class GlidePath : MonoBehaviour
    {
        [Tooltip("Waypoints, in order. Uses this object's children of 'Path' if left empty.")]
        public List<Transform> waypoints = new List<Transform>();

        [Tooltip("Walking speed in metres per second. ~1.0 is a slow, comfortable walk.")]
        [Range(0.3f, 2f)] public float speed = 1.0f;

        [Tooltip("Seconds to wait before Kate starts walking.")]
        public float startDelay = 3f;

        [Tooltip("Seconds to ease in from standing still to full speed (and to ease out at the end).")]
        public float easeSeconds = 2.5f;

        [Tooltip("How quickly the rig turns to follow the path. Lower = gentler turns.")]
        [Range(0.5f, 5f)] public float turnSmoothing = 1.5f;

        public bool playOnStart = true;

        public bool IsFinished { get; private set; }

        // Pre-sampled smooth curve, so speed stays constant through corners.
        readonly List<Vector3> _samples = new List<Vector3>();
        readonly List<float> _distances = new List<float>();
        float _totalLength;
        float _travelled;
        float _time;
        bool _playing;

        void Start()
        {
            BuildCurve();
            if (_samples.Count > 0)
            {
                transform.position = _samples[0];
                Vector3 dir = SampleDirection(0.5f);
                if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
            }
            if (playOnStart) Play();
        }

        public void Play()
        {
            _travelled = 0f;
            _time = 0f;
            IsFinished = false;
            _playing = true;
        }

        void Update()
        {
            if (!_playing || _totalLength <= 0f) return;

            _time += Time.deltaTime;
            if (_time < startDelay) return;

            // Speed profile: ease in at the start, ease out near the end.
            float easeDistance = speed * easeSeconds * 0.5f;
            float remaining = _totalLength - _travelled;
            float k = 1f;
            float sinceStart = _time - startDelay;
            if (sinceStart < easeSeconds) k = Mathf.SmoothStep(0.05f, 1f, sinceStart / easeSeconds);
            if (remaining < easeDistance) k = Mathf.Min(k, Mathf.SmoothStep(0.05f, 1f, remaining / easeDistance));

            _travelled = Mathf.Min(_totalLength, _travelled + speed * k * Time.deltaTime);
            transform.position = SamplePosition(_travelled);

            Vector3 ahead = SampleDirection(_travelled);
            if (ahead.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(ahead);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));
            }

            if (_travelled >= _totalLength - 0.001f)
            {
                _playing = false;
                IsFinished = true;
            }
        }

        // ---------- curve helpers ----------

        void BuildCurve()
        {
            _samples.Clear();
            _distances.Clear();
            var pts = new List<Vector3>();
            foreach (var w in waypoints) if (w) pts.Add(w.position);
            if (pts.Count < 2) return;

            // Catmull-Rom spline through the waypoints, sampled densely.
            const int stepsPerSegment = 40;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
                Vector3 p1 = pts[i];
                Vector3 p2 = pts[i + 1];
                Vector3 p3 = pts[Mathf.Min(i + 2, pts.Count - 1)];
                for (int s = 0; s < stepsPerSegment; s++)
                    _samples.Add(CatmullRom(p0, p1, p2, p3, s / (float)stepsPerSegment));
            }
            _samples.Add(pts[pts.Count - 1]);

            _totalLength = 0f;
            _distances.Add(0f);
            for (int i = 1; i < _samples.Count; i++)
            {
                _totalLength += Vector3.Distance(_samples[i - 1], _samples[i]);
                _distances.Add(_totalLength);
            }
        }

        Vector3 SamplePosition(float distance)
        {
            if (_samples.Count == 0) return transform.position;
            distance = Mathf.Clamp(distance, 0f, _totalLength);
            int i = _distances.BinarySearch(distance);
            if (i >= 0) return _samples[i];
            i = ~i;
            if (i <= 0) return _samples[0];
            if (i >= _samples.Count) return _samples[_samples.Count - 1];
            float d0 = _distances[i - 1], d1 = _distances[i];
            return Vector3.Lerp(_samples[i - 1], _samples[i], (distance - d0) / Mathf.Max(d1 - d0, 0.0001f));
        }

        Vector3 SampleDirection(float distance)
        {
            // Look a little ahead so turns begin smoothly before the corner.
            Vector3 a = SamplePosition(distance);
            Vector3 b = SamplePosition(Mathf.Min(distance + 2.5f, _totalLength));
            if ((b - a).sqrMagnitude < 0.0001f) b = a + transform.forward;
            Vector3 d = b - a;
            d.y = 0f;
            return d.normalized;
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            for (int i = 0; i < waypoints.Count - 1; i++)
                if (waypoints[i] && waypoints[i + 1])
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
