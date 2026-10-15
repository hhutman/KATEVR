using UnityEngine;

namespace KateVR
{
    /// <summary>
    /// Kate's voice-over, heard inside the suit: it stays with the viewer's head (not placed in the street),
    /// slightly muffled, and starts <see cref="startTime"/> seconds into the scene.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class KateVoice : MonoBehaviour
    {
        [Tooltip("Seconds after the scene starts when Kate begins speaking.")]
        public float startTime = 8f;

        void Start()
        {
            var src = GetComponent<AudioSource>();
            if (src.clip != null) src.PlayDelayed(Mathf.Max(0f, startTime - Time.timeSinceLevelLoad));
        }
    }
}
