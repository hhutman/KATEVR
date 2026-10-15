using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// A figure hurrying along its forward direction: swings legs and arms (children named
    /// LegL/LegR/ArmL/ArmR, if present) and bobs slightly with each step.
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
        [Tooltip("Length of one stride (two steps), in metres.")]
        public float strideLength = 1.6f;

        Vector3 _start;
        float _walked;
        Transform _legL, _legR, _armL, _armR;

        void Start()
        {
            _start = transform.position;
            _walked = 0f;
            _legL = FindDeep("LegL"); _legR = FindDeep("LegR");
            _armL = FindDeep("ArmL"); _armR = FindDeep("ArmR");
        }

        Transform FindDeep(string n)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == n) return t;
            return null;
        }

        void Update()
        {
            if (_walked >= distance) return;
            _walked = Mathf.Min(distance, _walked + speed * Time.deltaTime);

            float phase = _walked / strideLength * Mathf.PI * 2f;
            float swing = Mathf.Sin(phase);
            float lift = Mathf.Abs(Mathf.Cos(phase)) * bob;
            transform.position = _start + transform.forward * _walked + Vector3.up * lift;

            float legAmp = 30f, armAmp = 24f;
            if (_legL) _legL.localRotation = Quaternion.Euler( swing * legAmp, 0, 0);
            if (_legR) _legR.localRotation = Quaternion.Euler(-swing * legAmp, 0, 0);
            if (_armL) _armL.localRotation = Quaternion.Euler(-swing * armAmp, 0, 0);
            if (_armR) _armR.localRotation = Quaternion.Euler( swing * armAmp, 0, 0);
        }
    }
}
