using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.Vehicles
{
    /// <summary>Temporary player crash ragdoll with settle-aware recovery.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCrashRagdoll : MonoBehaviour
    {
        [SerializeField] private float minimumLieSeconds = 2.1f;
        [SerializeField] private float maximumLieSeconds = 3.6f;
        [SerializeField] private float settledSpeed = 0.7f;

        private NpcRagdoll _ragdoll;
        private PlayerController _controller;
        private float _recoverAt;
        private float _forceRecoverAt;
        private bool _crashed;

        public bool IsCrashed => _crashed;

        public static PlayerCrashRagdoll Trigger(
            GameObject character,
            Vector3 impactVelocity,
            Vector3 impactPoint)
        {
            if (character == null) return null;
            var recovery = character.GetComponent<PlayerCrashRagdoll>();
            if (recovery == null) recovery = character.AddComponent<PlayerCrashRagdoll>();
            recovery.Begin(impactVelocity, impactPoint);
            return recovery;
        }

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _ragdoll = GetComponent<NpcRagdoll>();
            if (_ragdoll == null) _ragdoll = gameObject.AddComponent<NpcRagdoll>();
        }

        private void Begin(Vector3 impactVelocity, Vector3 impactPoint)
        {
            if (_crashed || _ragdoll == null) return;
            _crashed = true;
            if (_controller != null) _controller.IsControlled = false;
            _ragdoll.Ragdoll(Vector3.ClampMagnitude(impactVelocity, 13f), impactPoint);
            _recoverAt = Time.time + minimumLieSeconds;
            _forceRecoverAt = Time.time + maximumLieSeconds;
        }

        private void Update()
        {
            if (!_crashed || Time.time < _recoverAt) return;
            if (Time.time < _forceRecoverAt && _ragdoll != null && !_ragdoll.IsSettled(settledSpeed)) return;

            _ragdoll?.Recover(true);
            _crashed = false;
            bool active = CharacterSwitchManager.Instance?.Active?.root == gameObject;
            if (_controller != null) _controller.IsControlled = active;
        }
    }
}
