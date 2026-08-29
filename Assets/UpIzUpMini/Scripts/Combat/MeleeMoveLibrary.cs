using UnityEngine;

namespace UpIzUpMini.Combat
{
    /// <summary>
    /// MINI-120 (combat bug-fix pass + combo request). User: "i want you
    /// to remember the combat like a system" - a data-driven list of
    /// moves a generic combo driver reads, matching the pattern already
    /// used for shop items (DealerSpecs et al. in Mini011PhaseBSetup),
    /// not a hardcoded single punch. Adding a new move later means
    /// adding one more row here, not touching SimpleMeleeCombat's logic.
    ///
    /// Each entry's clip is a real Mixamo motion-capture punch (the
    /// user's own free account, confirmed by the user watching each one
    /// in Mixamo's own preview) - see Docs/ASSET-REGISTER.md MINI-AST-122.
    /// Reach/radius/bodyRadiusBonus are deliberately tighter than the
    /// project's old single-punch defaults (reach 1.65, +0.35 bonus),
    /// which Mini120HitDetectionCheck.cs measured as landing hits up to
    /// 2.3m away - a lunging-weapon-scale volume never retuned for a
    /// real arm's-length jab.
    /// </summary>
    public static class MeleeMoveLibrary
    {
        // Action ids baked into the shared controller's upper-body
        // "Action" layer by HumanoidAnimationLayerBuilder, same
        // convention SimpleMeleeCombat.ActionId already used.
        public const string JabId = "Melee";
        public const string HookId = "MeleeHook";
        public const string RightHookId = "MeleeRightHook";
        public const string FinisherId = "MeleeFinisher";

        // Asset paths, not Resources paths - the clips live under
        // Assets/Mixamo, loaded via the same AssetDatabase-based
        // LoadClip helper Mini011PhaseBSetup already uses for every
        // other baked action clip. Kept here as the single source of
        // truth so the Editor-side baking code and this runtime data
        // table can't drift apart.
        public const string JabClipPath = "Assets/Mixamo/Animations/Mixamo_JabPunch.fbx";
        public const string HookClipPath = "Assets/Mixamo/Animations/Mixamo_Hook.fbx";
        public const string RightHookClipPath = "Assets/Mixamo/Animations/Mixamo_RightHook.fbx";
        // MINI-120 combo request, user: "i like the idea for this punch
        // first and then the second time would be a hook and then
        // something else and then the combo etc." The 4th and final
        // step is the real multi-strike "Punch Combo" clip (already
        // downloaded, MINI-AST-124) as the payoff finisher, instead of
        // one more single punch.
        public const string FinisherClipPath = "Assets/Mixamo/Animations/Mixamo_PunchCombo4.fbx";

        /// <summary>
        /// The 4-hit escalating combo chain: Jab -> Hook -> Right Hook ->
        /// Finisher (the real multi-strike "Punch Combo" clip), looping
        /// back to Jab. SimpleMeleeCombat advances one step per landed
        /// button press within the combo window and resets to index 0
        /// if the player waits too long between hits.
        ///
        /// Damage ramps up through the chain (a real "combo payoff" -
        /// finishing the sequence hits harder than throwing one jab and
        /// stopping) - tune these together with the user's own feel
        /// feedback, not just once from a rule of thumb.
        /// </summary>
        public static readonly ComboMove[] Chain =
        {
            new ComboMove
            {
                id = JabId,
                clipPath = JabClipPath,
                damage = 22f,
                windupSeconds = 0.14f,
                activeSeconds = 0.10f,
                recoverySeconds = 0.28f,
                forwardOffset = 0.22f,
                reach = 0.95f,
                radius = 0.22f,
                arcDegrees = 55f,
                bodyRadiusBonus = 0.15f,
            },
            new ComboMove
            {
                id = HookId,
                clipPath = HookClipPath,
                damage = 26f,
                windupSeconds = 0.18f,
                activeSeconds = 0.12f,
                recoverySeconds = 0.32f,
                forwardOffset = 0.22f,
                reach = 1.0f,
                radius = 0.24f,
                arcDegrees = 65f,
                bodyRadiusBonus = 0.15f,
            },
            new ComboMove
            {
                id = RightHookId,
                clipPath = RightHookClipPath,
                damage = 34f,
                windupSeconds = 0.16f,
                activeSeconds = 0.12f,
                recoverySeconds = 0.4f,
                forwardOffset = 0.22f,
                reach = 1.05f,
                radius = 0.26f,
                arcDegrees = 60f,
                bodyRadiusBonus = 0.15f,
            },
            new ComboMove
            {
                // The multi-strike clip itself covers several hits over
                // its own ~1.1s length - windup/active/recovery here
                // describe the SWING TIMELINE (when ResolveContact's one
                // damage instance fires and when the next Attack() is
                // allowed), not an attempt to key every individual
                // strike inside the clip to its own hit. A single,
                // bigger finisher hit is the intended payoff.
                id = FinisherId,
                clipPath = FinisherClipPath,
                damage = 48f,
                windupSeconds = 0.2f,
                activeSeconds = 0.15f,
                recoverySeconds = 0.55f,
                forwardOffset = 0.24f,
                reach = 1.1f,
                radius = 0.28f,
                arcDegrees = 60f,
                bodyRadiusBonus = 0.15f,
            },
        };

        public struct ComboMove
        {
            public string id;
            public string clipPath;
            public float damage;
            public float windupSeconds;
            public float activeSeconds;
            public float recoverySeconds;
            public float forwardOffset;
            public float reach;
            public float radius;
            public float arcDegrees;
            public float bodyRadiusBonus;

            public MeleeAttackProfile BuildProfile() => new MeleeAttackProfile(
                windupSeconds, activeSeconds, recoverySeconds,
                forwardOffset, reach, radius, arcDegrees, bodyRadiusBonus);

            public float TotalSeconds => windupSeconds + activeSeconds + recoverySeconds;
        }
    }
}
