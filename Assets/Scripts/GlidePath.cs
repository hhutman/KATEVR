using System.Collections.Generic;
using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// Moves Kate's rig along a path of waypoints like a person walking, not a camera on a rail:
    /// it eases in and out, slows down through turns, turns its body a little lazily,
    /// drifts in pace, and carries a very small sway. The viewer's head stays free to look around.
    /// </summary>
    public class GlidePath : MonoBehaviour
    {
        [Tooltip("Waypoints, in order.")]
        public List<Transform> waypoints = new List<Transform>();

        [Tooltip("Walking speed on the straight, metres per second.")]
        [Range(0.3f, 2f)] public float speed = 1.0f;

        [Tooltip("Speed through the tightest part of a turn, as a fraction of walking speed.")]
        [Range(0.3f, 1f)] public float turnSpeed = 0.55f;

        [Tooltip("Seconds to wait before Kate starts walking.")]
        public float startDelay = 3f;

        [Tooltip("Seconds to ease in from standing still (and out at the end).")]
        public float easeSeconds = 2.5f;

        [Tooltip("How quickly the body turns to follow the path. Lower = lazier, more human.")]
        [Range(0.3f, 5f)] public float turnSmoothing = 0.8f;

        [Tooltip("How far ahead (metres) Kate looks along the path when deciding where to face.")]
        [Range(1f, 8f)] public float lookAhead = 4f;

        [Tooltip("Gentle drift in walking pace (0 = metronome).")]
        [Range(0f, 0.2f)] public float paceVariation = 0.07f;

        [Tooltip("Tiny step sway, in metres. Set to 0 if anyone feels queasy.")]
        [Range(0f, 0.03f)] public float sway = 0.008f;

        public bool playOnStart = true;

        public bool IsFinished { get; private set; }

        readonly List<Vector3> _samples = new List<Vector3>();
        readonly List<float> _distances = new List<float>();
        float _totalLength, _travelled, _time, _turnFactor = 1f;
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

            // Ease in at the start and out at the end.
            float easeDistance = speed * easeSeconds * 0.5f;
            float remaining = _totalLength - _travelled;
            float k = 1f;
            float sinceStart = _time - startDelay;
            if (sinceStart < easeSeconds) k = Mathf.SmoothStep(0.05f, 1f, sinceStart / easeSeconds);
            if (remaining < easeDistance) k = Mathf.Min(k, Mathf.SmoothStep(0.03f, 1f, remaining / easeDistance));

            // Slow down where the path bends (measured a little ahead, so she slows before the corner).
            float bend = Vector3.Angle(SampleDirection(_travelled), SampleDirection(_travelled + 3f)) / 45f;
            float targetTurn = Mathf.Lerp(1f, turnSpeed, Mathf.SmoothStep(0f, 1f, bend));
            _turnFactor = Mathf.Lerp(_turnFactor, targetTurn, 1f - Mathf.Exp(-1.5f * Time.deltaTime));

            float pace = 1f + paceVariation * (Mathf.PerlinNoise(_time * 0.17f, 3.7f) * 2f - 1f);
            _travelled = Mathf.Min(_totalLength, _travelled + speed * k * _turnFactor * pace * Time.deltaTime);

            // Body turns lazily toward where the path is heading.
            Vector3 ahead = SampleDirection(_travelled);
            if (ahead.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(ahead);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));
            }

            // A very small sway with each step, fading out as she stops.
            float step = _travelled / 0.75f * Mathf.PI;
            Vector3 swayOffset = transform.right * Mathf.Sin(step) * sway * k
                               + Vector3.up * (Mathf.Abs(Mathf.Cos(step)) - 0.5f) * sway * k;
            transform.position = SamplePosition(_travelled) + swayOffset;

            if (_travelled >= _totalLength - 0.001f)
            {
                transform.position = SamplePosition(_totalLength);
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
            Vector3 a = SamplePosition(distance);
            Vector3 b = SamplePosition(Mathf.Min(distance + lookAhead, _totalLength));
            if ((b - a).sqrMagnitude < 0.01f)
            {
                // Near the end: keep facing the last stretch of the path.
                a = SamplePosition(Mathf.Max(0f, _totalLength - lookAhead));
                b = SamplePosition(_totalLength);
            }
            Vector3 d = b - a;
            d.y = 0f;
            return d.sqrMagnitude < 0.0001f ? transform.forward : d.normalized;
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
