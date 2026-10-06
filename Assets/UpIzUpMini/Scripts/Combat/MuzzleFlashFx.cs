using UnityEngine;

namespace UpIzUpMini.Combat
{
    /// <summary>
    /// MINI-195: a visible muzzle flash for the sidearm (player and NPC). Two crossed additive quads with a star texture, a hot core quad and a short point
    /// light, for about 0.07 s. The material lives in Resources so the additive shader is guaranteed to be in the player build.
    /// </summary>
    public static class MuzzleFlashFx
    {
        static Material material;
        static Mesh quad;

        public static Material FlashMaterial { get { if (material == null) material = Resources.Load<Material>("Weapons/MuzzleFlash"); return material; } }

        public static void Play(Transform origin, Vector3 direction)
        {
            if (origin == null) return;
            var go = new GameObject("Muzzle Flash");
            go.transform.position = origin.position + direction.normalized * .02f;
            go.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > .0001f ? direction : origin.forward, Vector3.up);
            var fx = go.AddComponent<FlashBehaviour>();
            fx.Build(FlashMaterial, Random.Range(0f, 360f));
        }

        class FlashBehaviour : MonoBehaviour
        {
            float born, life = .075f;
            Light flashLight;
            Transform core;

            public void Build(Material mat, float roll)
            {
                born = Time.time;
                if (quad == null) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad); quad = tmp.GetComponent<MeshFilter>().sharedMesh; Destroy(tmp); }
                if (mat != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var q = new GameObject("Plane" + i); q.transform.SetParent(transform, false);
                        // forward-facing blade: the quad lies along the barrel axis and spreads sideways; two blades cross at 90 degrees
                        q.transform.localRotation = Quaternion.Euler(0f, 0f, roll + i * 90f) * Quaternion.Euler(0f, 90f, 0f); q.transform.localPosition = new Vector3(0f, 0f, .09f);
                        q.transform.localScale = new Vector3(.2f, .09f, 1f);
                        q.AddComponent<MeshFilter>().sharedMesh = quad; var r = q.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    }
                    var c = new GameObject("Core"); c.transform.SetParent(transform, false); core = c.transform;
                    c.AddComponent<MeshFilter>().sharedMesh = quad; var cr = c.AddComponent<MeshRenderer>(); cr.sharedMaterial = mat; cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; cr.receiveShadows = false;
                    c.transform.localScale = Vector3.one * .16f;
                }
                var lg = new GameObject("Light"); lg.transform.SetParent(transform, false); lg.transform.localPosition = new Vector3(0, 0, .15f);
                flashLight = lg.AddComponent<Light>(); flashLight.type = LightType.Point; flashLight.range = 7f; flashLight.intensity = 3.2f; flashLight.color = new Color(1f, .78f, .42f); flashLight.shadows = LightShadows.None;
            }

            void LateUpdate()
            {
                float t = (Time.time - born) / life;
                if (t >= 1f) { Destroy(gameObject); return; }
                transform.localScale = Vector3.one * Mathf.Lerp(1.15f, .55f, t);
                if (flashLight != null) flashLight.intensity = 3.2f * (1f - t);
                if (core != null && Camera.main != null) core.rotation = Camera.main.transform.rotation;   // the hot core always faces the camera
            }
        }
    }
}
