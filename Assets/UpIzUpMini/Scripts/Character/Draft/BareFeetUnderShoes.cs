// MINI-206 (cloud design lane) - written without a Unity Editor, NOT COMPILED HERE. Review before use.
using UnityEngine;

namespace UpIzUpMini.Character.Draft
{
    /// <summary>
    /// The MINI-197 bare bodies keep the feet as their own mesh (SacatBare_Feet / FrankiBare_Feet). Measured in Blender, the
    /// toes of the bare feet poke 1-2 cm through the soles of the Mike 90/97/270 shoe meshes. Standard game fix: do not draw
    /// the bare feet while any shoe is worn. Add this to the character root; it checks a few times a second (cheap) and also
    /// when OnTransformChildrenChanged fires (wardrobe swaps add/remove children).
    /// </summary>
    public class BareFeetUnderShoes : MonoBehaviour
    {
        [Tooltip("Renderer name fragments that count as shoes.")]
        public string[] shoeNameFragments = { "shoe", "AM90", "AM97", "mike90", "mike97", "mike270" };
        [Tooltip("Renderer name fragment of the bare feet mesh.")]
        public string feetNameFragment = "_Feet";
        public float checkInterval = 0.25f;
        float _next;

        void OnEnable() => Apply();
        void OnTransformChildrenChanged() => Apply();
        void Update() { if (Time.unscaledTime >= _next) { _next = Time.unscaledTime + checkInterval; Apply(); } }

        public void Apply()
        {
            bool shoes = false;
            Renderer feet = null;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                if (n.Contains(feetNameFragment)) { feet = r; continue; }
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                foreach (var f in shoeNameFragments)
                    if (n.IndexOf(f, System.StringComparison.OrdinalIgnoreCase) >= 0) { shoes = true; break; }
            }
            if (feet != null && feet.enabled == shoes) feet.enabled = !shoes;
        }
    }
}
