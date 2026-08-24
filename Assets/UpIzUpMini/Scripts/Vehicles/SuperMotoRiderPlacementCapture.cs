using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "can i place sacat on the bike manually
    /// so you can have an idea from the game project scene." Same
    /// "manual placement is authoritative" pattern already used for
    /// Sacat's chain (SacatChainPlacement.asset, captured via a dedicated
    /// read-only tool) - the user drags Sacat into position by eye in the
    /// Scene view while in Play Mode, presses a key, and the exact
    /// resulting local offset from the seat anchor is written to the
    /// Player.log for Claude to read back afterward, rather than the
    /// user having to type numbers out of the Inspector by hand.
    /// </summary>
    public class SuperMotoRiderPlacementCapture : MonoBehaviour
    {
        [SerializeField] private KeyCode captureKey = KeyCode.P;
        private Transform _seatAnchor;
        private Transform _rider;

        public void Configure(Transform seatAnchor, Transform rider)
        {
            _seatAnchor = seatAnchor;
            _rider = rider;
        }

        private void Update()
        {
            if (_seatAnchor == null || _rider == null) return;
            if (!Input.GetKeyDown(captureKey)) return;

            Vector3 localPos = _seatAnchor.InverseTransformPoint(_rider.position);
            Quaternion localRot = Quaternion.Inverse(_seatAnchor.rotation) * _rider.rotation;
            Vector3 localEuler = localRot.eulerAngles;

            Debug.Log($"MINI-119 RIDER PLACEMENT CAPTURE: localPosition=({localPos.x:F5}, {localPos.y:F5}, {localPos.z:F5})  localEulerAngles=({localEuler.x:F2}, {localEuler.y:F2}, {localEuler.z:F2})");
        }
    }
}
