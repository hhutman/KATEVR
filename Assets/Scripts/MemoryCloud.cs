using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// Plays one memory: at <see cref="startTime"/> seconds into the scene the points gather into the image,
    /// hold, then break apart over <see cref="duration"/> seconds (see Shaders/MemoryPoints.shader).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class MemoryCloud : MonoBehaviour
    {
        [Tooltip("Seconds after the scene starts when this memory begins to form.")]
        public float startTime = 10f;
        [Tooltip("Seconds from the first points gathering to the last point fading.")]
        public float duration = 13f;
        [Tooltip("Size of each point, in metres.")]
        public float pointSize = 0.1f;

        static readonly int ProgressId = Shader.PropertyToID("_Progress");
        static readonly int PointSizeId = Shader.PropertyToID("_PointSize");
        MeshRenderer _renderer;
        MaterialPropertyBlock _block;

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
            _renderer.enabled = false;
        }

        void Update()
        {
            float p = (Time.timeSinceLevelLoad - startTime) / Mathf.Max(duration, 0.1f);
            bool visible = p > 0f && p < 1f;
            _renderer.enabled = visible;
            if (!visible) return;
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(ProgressId, p);
            _block.SetFloat(PointSizeId, pointSize);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
