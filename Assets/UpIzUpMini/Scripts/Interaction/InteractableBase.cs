using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Common base for world interactables. Auto-registers into a static
    /// registry so <see cref="InteractionDetector"/> can find nearby
    /// interactables without a per-frame scene scan.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        public static readonly List<IInteractable> All = new List<IInteractable>();

        [SerializeField] private float promptHeightOffset = 2f;
        private Transform _promptAnchor;

        public virtual Transform PromptAnchor
        {
            get
            {
                if (_promptAnchor == null)
                {
                    var anchorGo = new GameObject("PromptAnchor");
                    anchorGo.transform.SetParent(transform, false);
                    anchorGo.transform.localPosition = Vector3.up * promptHeightOffset;
                    _promptAnchor = anchorGo.transform;
                }
                return _promptAnchor;
            }
        }

        public abstract string PromptLabel { get; }

        /// <summary>Optional short feedback line shown after a successful interact.</summary>
        public virtual string GetInteractionFeedback() => null;

        protected virtual void OnEnable() => All.Add(this);

        protected virtual void OnDisable() => All.Remove(this);

        public virtual bool CanInteract(GameObject interactor) => true;

        public abstract void Interact(GameObject interactor);
    }
}
