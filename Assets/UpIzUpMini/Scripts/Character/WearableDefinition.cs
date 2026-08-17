using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-064. Data-driven definition for one wearable accessory/garment.
    /// The slot-based equipment system uses these to know which body slot the
    /// item occupies, which bone to attach to, and (for static primitives)
    /// where to place it. A real mesh can be assigned too - the system parents
    /// it to the bone and applies the local pose here.
    ///
    /// This is the "boxes then customise" structure the user described: each
    /// body slot is a box, and the owned items you pick customise what shows.
    /// </summary>
    [CreateAssetMenu(fileName = "Wearable_", menuName = "Up Iz Up Mini/Wearable", order = 1)]
    public class WearableDefinition : ScriptableObject
    {
        // Which body slot this occupies - only one item per slot can be shown.
        public enum BodySlot { Head, Face, Chest, Arm, Legs, Feet }
        [Tooltip("The body slot this wearable occupies. Only one equipped item per slot shows.")]
        public BodySlot slot = BodySlot.Chest;

        [Tooltip("The economy item id that must be owned for this wearable to show (e.g. chain_gold).")]
        public string requiresItemId = "chain_gold";

        [Tooltip("Humanoid bone to attach to - e.g. Chest, Head, LeftLowerArm, Hips, LeftFoot.")]
        public HumanBodyBones bone = HumanBodyBones.Chest;

        // Local pose relative to the bone.
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localEulerAngles = Vector3.zero;
        public Vector3 localScale = Vector3.one;

        [Tooltip("Optional: if set, this mesh is instantiated instead of a runtime primitive. Leave null to use a simple marker box.")]
        public Mesh mesh;
        [Tooltip("Optional material for the mesh/marker. Ignored if mesh + its own materials are preferred.")]
        public Material material;

        [Tooltip("Whether this wearable should dangle/swing (e.g. the chain) via AccessorySwing.")]
        public bool swings = false;
    }
}
