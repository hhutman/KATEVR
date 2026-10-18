using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// Keeps the horizon glow far ahead of Kate, at the end of the street she's facing,
    /// turning with her body (smoothly), always facing her.
    /// </summary>
    public class HorizonGlow : MonoBehaviour
    {
        public Transform follow;          // Kate's rig
        public float distance = 34f;
        public float height = 5f;
        public float turnSmoothing = 0.6f;

        Vector3 _dir;

        void Start()
        {
            _dir = follow ? Flat(follow.forward) : Vector3.forward;
            Place(1f);
        }

        void LateUpdate() => Place(1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));

        void Place(float k)
        {
            if (!follow) return;
            _dir = Vector3.Slerp(_dir, Flat(follow.forward), k).normalized;
            Vector3 p = follow.position + _dir * distance;
            p.y = height;
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(_dir, Vector3.up); // quad faces back toward Kate
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized; }
    }
}
