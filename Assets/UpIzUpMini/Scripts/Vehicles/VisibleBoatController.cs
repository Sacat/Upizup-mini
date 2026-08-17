using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-061. Makes the Gwada run VISIBLE: the moored boat at the jetty
    /// actually departs when a dispatch starts (slides out to sea and fades),
    /// stays gone while the trip is active, then reappears and glides back
    /// in as the return time approaches/arrives.
    ///
    /// Reads GuadeloupeTrade.TripActive / SecondsRemaining and moves the
    /// boat's visual root along the jetty axis. Works with the placeholder
    /// MooredBoat mesh; scale/offsets are wired by the scene builder.
    /// </summary>
    public class VisibleBoatController : MonoBehaviour
    {
        [Header("Where the boat sits idle (jetty) and where it goes")]
        [SerializeField] private Vector3 dockPosition;
        [SerializeField] private Vector3 offshorePosition = new Vector3(-90f, 0.5f, 62f);
        [SerializeField] private float travelSeconds = 3f;

        [Header("Return lead-in (boat comes back before arrival)")]
        [SerializeField] private float returnLeadSeconds = 4f;

        private Transform _visual;
        private bool _away;
        private float _departStartedAt = -1f;

        private void Awake()
        {
            // The visual is the first non-logic child (the MooredBoat's
            // visual root in the scene, created by the scene builder).
            _visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            transform.position = dockPosition;
        }

        private void Update()
        {
            var trade = GuadeloupeTrade.Instance;
            if (trade == null) return;

            if (trade.TripActive)
            {
                if (!_away)
                {
                    // Just departed — animate out to sea over travelSeconds.
                    _departStartedAt = Time.time;
                    _away = true;
                }
                // Driving out, hold offshore, then ease back near return.
                float t = Mathf.Clamp01((Time.time - _departStartedAt) / travelSeconds);
                float remaining = trade.SecondsRemaining;

                if (remaining <= returnLeadSeconds)
                {
                    // Returning: interpolate offshore -> dock over travelSeconds.
                    float rt = 1f - Mathf.Clamp01(remaining / returnLeadSeconds);
                    transform.position = Vector3.Lerp(offshorePosition, dockPosition, rt);
                }
                else
                {
                    transform.position = Vector3.Lerp(dockPosition, offshorePosition, t);
                }
            }
            else if (_away)
            {
                // Trip finished — snap the boat fully back to the dock.
                transform.position = dockPosition;
                _away = false;
            }
        }
    }
}
