using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Economy;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// MINI-053. A dedicated dialogue box for buying seeds from a boss or
    /// strain teacher. Unlike the full shop panel, this shows a single
    /// strain's seed with its price and an in-character confirmation line,
    /// then buys on the confirm key. Reuses the same panel wiring pattern
    /// as ShopPanelController so it works on desktop now and on a phone
    /// later (the confirm line becomes a button).
    /// </summary>
    public class SeedBuyDialogue : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text bodyText;

        private CropDefinition _crop;
        private int _price;
        private string _offerLine;   // the boss's sell pitch
        private string _successLine; // shown after purchase
        private bool _built;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>Show the buy dialogue for a strain's seeds.</summary>
        public void Open(CropDefinition crop, int price, string offerLine, string successLine)
        {
            if (panel == null) return;
            _crop = crop;
            _price = price;
            _offerLine = offerLine;
            _successLine = successLine;
            _built = false;
            panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }

        public void Close()
        {
            if (panel == null) return;
            panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!IsOpen) return;

            // E buys/confirms, Esc backs out (no purchase).
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.E)) ConfirmBuy();
        }

        private void ConfirmBuy()
        {
            var economy = EconomyManager.Instance;
            if (economy == null) { Close(); return; }

            if (_price > 0 && economy.Money < _price)
            {
                _offerLine = $"{_crop.displayName} seed cost ${_price}. Allu short, nuh. Come back when you have it.";
                _built = true;
                Refresh();
                return;
            }

            if (_price > 0) economy.AddMoney(-_price);
            economy.AddSeeds(_crop.cropId, 3);
            _offerLine = _successLine + $" That cost you ${_price}.";
            _built = true; // show confirmation, don't close on the same key

            Missions.MissionSystem.Instance?.Notify(
                Missions.ObjectiveKind.BuySeeds, _crop.cropId);
            Refresh();
        }

        private void Refresh()
        {
            if (bodyText == null || _crop == null) return;

            // First open: offer + price, E to buy. After a purchase: keep
            // the confirmation visible until the player leaves with Esc.
            string line = _built
                ? _offerLine
                : $"{_offerLine}\n\n<b>{_crop.displayName} seeds x3 — ${_price}</b>\n(E to buy • Esc to leave)";

            bodyText.text = line;
        }
    }
}
