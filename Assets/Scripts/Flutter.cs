using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// A tattered strip of cloth or tape hanging from a wire, fluttering gently in the wind.
    /// The pivot is at the top (where it's tied); the strip hangs down from it.
    /// </summary>
    public class Flutter : MonoBehaviour
    {
        public float swing = 18f;       // degrees of sway in the wind
        public float twist = 25f;       // degrees of twist
        public float speed = 1.1f;
        public Vector3 wind = new Vector3(0.6f, 0f, 0.2f); // leans the strip downwind

        Quaternion _rest;
        float _seed;

        void Start()
        {
            _rest = transform.localRotation;
            _seed = Random.Range(0f, 100f);
        }

        void Update()
        {
            float t = Time.time * speed + _seed;
            float gust = Mathf.PerlinNoise(t * 0.35f, _seed) * 1.6f;
            float sway = (Mathf.PerlinNoise(t, _seed + 3.1f) - 0.5f) * 2f * swing;
            float tw = (Mathf.PerlinNoise(t * 1.3f, _seed + 7.7f) - 0.5f) * 2f * twist;
            Quaternion lean = Quaternion.Euler(wind.z * 20f * gust, 0f, -wind.x * 20f * gust);
            transform.localRotation = _rest * lean * Quaternion.Euler(sway, tw, sway * 0.4f);
        }
    }
}
