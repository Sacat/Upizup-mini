using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>Exact user-approved visual-root scale for a specific character.
    /// Kept separate from rig and collider settings so manual appearance can
    /// survive generated-scene rebuilds without broad character changes.</summary>
    [CreateAssetMenu(menuName = "Up Iz Up Mini/Character Visual Profile")]
    public class CharacterVisualProfile : ScriptableObject
    {
        public bool useManualScale;
        public Vector3 localScale = Vector3.one;
    }
}
