using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// A placeholder figure hurrying along its forward direction, with a slight bob in its stride,
    /// so the street has a few others rushing to get home.
    /// Start them inside the fog so they emerge from (or vanish into) the haze.
    /// </summary>
    public class Wanderer : MonoBehaviour
    {
        [Tooltip("Metres per second. ~1.7 is a hurried walk.")]
        public float speed = 1.7f;
        [Tooltip("Stops after walking this far.")]
        public float distance = 150f;
        [Tooltip("Up-and-down bob per step, in metres.")]
        public float bob = 0.04f;

        Vector3 _start;
        float _walked;
        float _phase;

        void Start()
        {
            _start = transform.position;
            _phase = Random.Range(0f, 10f);
        }

        void Update()
        {
            if (_walked >= distance) return;
            _walked = Mathf.Min(distance, _walked + speed * Time.deltaTime);
            float stepsPerSecond = speed * 1.4f;
            float lift = Mathf.Abs(Mathf.Sin((Time.time + _phase) * stepsPerSecond * Mathf.PI)) * bob;
            transform.position = _start + transform.forward * _walked + Vector3.up * lift;
        }
    }
}
