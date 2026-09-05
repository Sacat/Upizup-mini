namespace UpIzUpMini.Combat
{
    public enum NpcFighterStyle { Police, Gang }

    /// <summary>
    /// One shared move table for hostile NPCs. It deliberately reuses the
    /// already-baked Mixamo player states, but gives NPCs longer windup/
    /// recovery and lower damage. Style changes the order, so police and
    /// gangs do not repeat the identical one-punch loop.
    /// </summary>
    public static class NpcCombatMoveLibrary
    {
        private static readonly Move[] PoliceChain =
        {
            MoveOf(MeleeMoveLibrary.JabId, 8f, 0.30f, 0.12f, 0.62f, 0.92f, 0.22f, 58f),
            MoveOf(MeleeMoveLibrary.HookId, 10f, 0.36f, 0.13f, 0.72f, 0.98f, 0.24f, 65f),
            MoveOf(MeleeMoveLibrary.JabId, 8f, 0.32f, 0.12f, 0.66f, 0.92f, 0.22f, 58f),
        };

        private static readonly Move[] GangChain =
        {
            MoveOf(MeleeMoveLibrary.HookId, 11f, 0.31f, 0.13f, 0.64f, 0.98f, 0.25f, 68f),
            MoveOf(MeleeMoveLibrary.RightHookId, 13f, 0.38f, 0.14f, 0.76f, 1.02f, 0.26f, 64f),
            MoveOf(MeleeMoveLibrary.JabId, 9f, 0.28f, 0.12f, 0.60f, 0.94f, 0.23f, 58f),
        };

        public static Move Get(NpcFighterStyle style, int step)
        {
            Move[] chain = style == NpcFighterStyle.Police ? PoliceChain : GangChain;
            int index = ((step % chain.Length) + chain.Length) % chain.Length;
            return chain[index];
        }

        public static int Count(NpcFighterStyle style) =>
            style == NpcFighterStyle.Police ? PoliceChain.Length : GangChain.Length;

        private static Move MoveOf(string id, float damage, float windup, float active,
            float recovery, float reach, float radius, float arc) => new Move
        {
            id = id,
            damage = damage,
            windup = windup,
            active = active,
            recovery = recovery,
            reach = reach,
            radius = radius,
            arc = arc,
        };

        public struct Move
        {
            public string id;
            public float damage;
            public float windup;
            public float active;
            public float recovery;
            public float reach;
            public float radius;
            public float arc;

            public float TotalSeconds => windup + active + recovery;
            public MeleeAttackProfile BuildProfile() => new MeleeAttackProfile(
                windup, active, recovery, 0.24f, reach, radius, arc, 0.10f);
        }
    }
}
