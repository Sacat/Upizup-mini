using UnityEngine;

namespace UpIzUpMini.Combat
{
    /// <summary>Anything that holds the sidearm: the two playable characters (FirearmController) and any NPC (NpcPistolUser).
    /// FirearmPose reads this and animates the body, so one animation system serves every humanoid.</summary>
    public interface IPistolUser
    {
        /// <summary>Weapon drawn and raised on target.</summary>
        bool IsAiming { get; }
        /// <summary>Weapon drawn but held in the lowered two-hand ready position (alert, not aiming).</summary>
        bool IsLowReady { get; }
        bool IsReloading { get; }
        /// <summary>0..1 through the current reload (drives the procedural magazine change).</summary>
        float ReloadProgress { get; }
        /// <summary>World direction the muzzle should point.</summary>
        Vector3 AimDirection { get; }
        /// <summary>The weapon visual root (grip, backstrap, shot origin and support anchors live under it).</summary>
        Transform WeaponRoot { get; }
    }
}
