using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// A placeholder figure that walks slowly along its forward direction,
    /// so the street has a few others "making their way too".
    /// Start them inside the fog so they emerge from (or vanish into) the haze.
    /// </summary>
    public class Wanderer : MonoBehaviour
    {
        public float speed = 0.6f;
        [Tooltip("Stops after walking this far.")]
        public float distance = 60f;

        Vector3 _start;
        float _walked;

        void Start() => _start = transform.position;

        void Update()
        {
            if (_walked >= distance) return;
            _walked = Mathf.Min(distance, _walked + speed * Time.deltaTime);
            transform.position = _start + transform.forward * _walked;
        }
    }
}
