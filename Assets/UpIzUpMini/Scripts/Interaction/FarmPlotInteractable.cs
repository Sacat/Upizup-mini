using UnityEngine;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// A tillable farm plot that shows a "[ E ] Plant" prompt.
    /// This is a MINI-001 stub: it only proves the prompt/interact loop.
    /// The real plant/water/grow/harvest/sell loop is MINI-003 scope.
    /// </summary>
    public class FarmPlotInteractable : InteractableBase
    {
        [SerializeField] private Renderer soilRenderer;
        [SerializeField] private Color plantedTint = new Color(0.22f, 0.16f, 0.09f);

        private bool _marked;

        public override string PromptLabel => "[ E ] Plant";

        public override void Interact(GameObject interactor)
        {
            if (_marked) return;
            _marked = true;

            if (soilRenderer != null)
            {
                soilRenderer.material.color = plantedTint;
            }

            Debug.Log("Farm plot marked for planting (full farming loop arrives in MINI-003).");
        }

        public override string GetInteractionFeedback() =>
            "Plot marked for planting. (Full farming loop is MINI-003.)";
    }
}
