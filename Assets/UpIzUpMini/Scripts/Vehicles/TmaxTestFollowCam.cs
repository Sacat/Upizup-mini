using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-065/066: a minimal chase camera for the isolated
    /// TMAX_Physics_Test scene only - not the game's real vehicle camera
    /// (that's later scope).
    ///
    /// MINI-066 added mouse orbit at the user's request ("can i use the
    /// controls in the test like the mouse to turn so i can see the front
    /// side and back"), because judging a riding ANIMATION needs to be able
    /// to walk around the bike - a fixed chase cam only ever shows the rider
    /// from behind, which is the one angle that hides hand placement on the
    /// bars and the rider's leg/peg contact.
    ///
    /// Hold right mouse to orbit (so a stray mouse move while driving doesn't
    /// swing the view), scroll to zoom, and the camera otherwise falls back
    /// to the plain chase behaviour behind the bike.
    /// </summary>
    public class TmaxTestFollowCam : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
        [SerializeField] private float followSpeed = 6f;

        [Header("Mouse orbit (test scene convenience)")]
        [Tooltip("Hold this to orbit with the mouse. 1 = right mouse button.")]
        [SerializeField] private int orbitMouseButton = 1;
        [SerializeField] private float orbitSensitivity = 4f;
        [SerializeField] private float zoomSensitivity = 3f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 18f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 75f;
        [Tooltip("Height above the bike's origin the camera aims at - roughly the rider's chest, so the rider (not the road) is centred.")]
        [SerializeField] private float lookHeight = 1f;

        [Tooltip("Returns to the automatic chase view after the mouse has been used - the camera otherwise stays exactly where it was parked.")]
        [SerializeField] private KeyCode resetViewKey = KeyCode.C;

        private float _yaw;
        private float _pitch = 12f;
        private float _distance;
        private bool _orbiting;
        private bool _userControlled;

        private void Start()
        {
            _distance = Mathf.Abs(offset.z);
            _pitch = 12f;
            if (target != null) _yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (Input.GetMouseButtonDown(orbitMouseButton)) _orbiting = true;
            if (Input.GetMouseButtonUp(orbitMouseButton)) _orbiting = false;

            if (_orbiting)
            {
                _yaw += Input.GetAxis("Mouse X") * orbitSensitivity;
                _pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
                _userControlled = true;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _distance = Mathf.Clamp(_distance - scroll * zoomSensitivity, minDistance, maxDistance);
                // Zooming counts as taking control too - otherwise framing a
                // close-up would be undone by the chase cam pulling back.
                _userControlled = true;
            }

            Vector3 focus = target.position + Vector3.up * lookHeight;

            // Once the user has orbited even once, the camera STAYS where
            // they put it and never drifts back on its own - they asked for
            // this explicitly so they can park the view and take screenshots
            // ("i will go back myself because i want to take screenshots").
            // An auto-return would swing the shot away the moment they let go
            // of the mouse, which is exactly when a screenshot gets taken.
            // Press the reset key to return to the chase view deliberately.
            if (Input.GetKeyDown(resetViewKey)) _userControlled = false;

            if (_userControlled)
            {
                // Keep the chosen world-space angle; only track the bike's
                // position, so a parked side-on view stays side-on as the
                // bike drives past.
                Quaternion held = Quaternion.Euler(_pitch, _yaw, 0f);
                transform.position = focus + held * new Vector3(0f, 0f, -_distance);
                transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
                return;
            }

            // Default chase view, until the user takes over.
            _yaw = Mathf.LerpAngle(_yaw, target.eulerAngles.y, followSpeed * Time.deltaTime);
            Quaternion chase = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = focus + chase * new Vector3(0f, 0f, -_distance);
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
