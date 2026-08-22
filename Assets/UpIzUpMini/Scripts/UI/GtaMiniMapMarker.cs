using UnityEngine;
using UpIzUpMini.Progression;

namespace UpIzUpMini.UI
{
    public enum MiniMapMarkerKind
    {
        Shop, Mission, Police, Gang, Farm, Safehouse, Church, Boat, Person,
        // MINI-113, user: "add minimap markers for owned/usable vehicles."
        Vehicle
    }

    /// <summary>Reusable data marker for this district and future maps.</summary>
    public sealed class GtaMiniMapMarker : MonoBehaviour
    {
        [SerializeField] private MiniMapMarkerKind kind;
        [SerializeField] private string displayName;
        [SerializeField] private string requiredMissionId;
        [SerializeField] private Color colour = Color.white;

        public MiniMapMarkerKind Kind => kind;
        public string DisplayName => displayName;
        public Color Colour => colour;
        public bool IsUnlocked => string.IsNullOrEmpty(requiredMissionId)
            || ProgressionGate.IsMissionReached(requiredMissionId);

        public void Configure(MiniMapMarkerKind markerKind, string label, Color markerColour, string missionId = null)
        {
            kind = markerKind;
            displayName = label;
            colour = markerColour;
            requiredMissionId = missionId ?? string.Empty;
        }
    }
}
