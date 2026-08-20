using UnityEngine;

namespace UpIzUpMini.Economy
{
    /// <summary>MINI-060: the four "readings" Gardey Zafeh can grant.</summary>
    public enum GardeyZafehBuff { None, EnergyBoost, HealthBoost, PoliceImmunity, DoubleMoney }

    /// <summary>
    /// MINI-060: Gardey Zafeh's random future readings - one active buff
    /// at a time (never stacked, per the user's "make her future readings
    /// random" - a new reading replaces, rather than piles onto, an old
    /// one), each granted for a fixed duration. Plain static state, not a
    /// MonoBehaviour, so any system can check it in one line - the same
    /// idiom MINI-049's cheat code already established with
    /// CharacterVitals.GlobalInvincible/GlobalUnlimitedStamina.
    /// </summary>
    public static class GardeyZafehBuffState
    {
        public static GardeyZafehBuff Active { get; private set; } = GardeyZafehBuff.None;
        private static float _expiresAt;

        public static bool IsActive(GardeyZafehBuff buff) => Active == buff && Time.time < _expiresAt;
        public static bool AnyActive => Active != GardeyZafehBuff.None && Time.time < _expiresAt;
        public static float SecondsRemaining => Active == GardeyZafehBuff.None ? 0f : Mathf.Max(0f, _expiresAt - Time.time);

        public static void Grant(GardeyZafehBuff buff, float durationSeconds)
        {
            Active = buff;
            _expiresAt = Time.time + durationSeconds;
        }
    }
}
