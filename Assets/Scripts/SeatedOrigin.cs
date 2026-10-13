using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace KateVR
{
    /// <summary>
    /// Makes tracking "seated": the viewer's head starts at the rig's eye point, facing down the street,
    /// whatever height they sit at or whichever way their chair faces.
    /// </summary>
    public class SeatedOrigin : MonoBehaviour
    {
        static readonly List<XRInputSubsystem> Subsystems = new List<XRInputSubsystem>();
        bool _done;
        float _timer;

        void Update()
        {
            if (_done) return;
            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.5f) return; // give XR a moment to start
            _timer = 0f;

            SubsystemManager.GetSubsystems(Subsystems);
            foreach (var s in Subsystems)
            {
                if (s == null || !s.running) continue;
                if (s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device))
                {
                    s.TryRecenter();
                    _done = true;
                }
            }
        }
    }
}
