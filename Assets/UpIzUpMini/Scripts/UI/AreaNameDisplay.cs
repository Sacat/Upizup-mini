using System;
using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;

namespace UpIzUpMini.UI
{
    [Serializable]
    public class AreaZone
    {
        public string areaName;
        public Vector3 center;
        public float radius = 40f;
    }

    /// <summary>
    /// Shows the name of the area the active character is in (GTA-style),
    /// then fades it out. Only re-triggers when the area actually changes,
    /// so it doesn't flicker while walking near a boundary.
    /// </summary>
    public class AreaNameDisplay : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private AreaZone[] zones;
        [SerializeField] private float holdSeconds = 2.2f;
        [SerializeField] private float fadeSeconds = 1.2f;

        private string _current = string.Empty;
        private float _shownAt = -999f;

        private void Update()
        {
            if (label == null) return;

            Transform player = GetActiveCharacter();
            if (player != null)
            {
                string area = ResolveArea(player.position);
                if (!string.IsNullOrEmpty(area) && area != _current)
                {
                    _current = area;
                    _shownAt = Time.time;
                    label.text = area;
                }
            }

            float age = Time.time - _shownAt;
            float alpha =
                age < holdSeconds ? 1f :
                age < holdSeconds + fadeSeconds ? 1f - (age - holdSeconds) / fadeSeconds :
                0f;

            var c = label.color;
            c.a = alpha;
            label.color = c;
        }

        private static Transform GetActiveCharacter()
        {
            var switcher = CharacterSwitchManager.Instance;
            if (switcher != null && switcher.Active?.root != null) return switcher.Active.root.transform;
            var tagged = GameObject.FindWithTag("Player");
            return tagged != null ? tagged.transform : null;
        }

        private string ResolveArea(Vector3 pos)
        {
            if (zones == null) return string.Empty;

            string best = string.Empty;
            float bestDist = float.MaxValue;
            foreach (var z in zones)
            {
                if (z == null) continue;
                float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(z.center.x, z.center.z));
                if (d <= z.radius && d < bestDist)
                {
                    bestDist = d;
                    best = z.areaName;
                }
            }
            return best;
        }

        public void SetZones(AreaZone[] newZones) => zones = newZones;
    }
}
