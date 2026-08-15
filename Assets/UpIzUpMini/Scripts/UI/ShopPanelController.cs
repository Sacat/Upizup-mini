using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Economy;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Simple keyboard-driven shop. Opened by talking to the shopkeeper;
    /// number keys buy the matching line, Esc/E closes. Deliberately
    /// list-based rather than a full mouse UI so it works the same on a
    /// phone later (the lines can become buttons without changing the
    /// underlying purchase flow).
    /// </summary>
    public class ShopPanelController : MonoBehaviour
    {
        /// <summary>
        /// The farm shop (seeds/tools) and the apparel shop (clothing,
        /// footwear, accessories) are deliberately separate shopfronts with
        /// separate stock, per the user's request - so this is a per-shop
        /// component, not a singleton. NPCs hold a reference to their own.
        /// </summary>
        [SerializeField] private string shopTitle = "SHOP";
        [SerializeField] private GameObject panel;
        [SerializeField] private Text bodyText;
        [SerializeField] private ShopItemDefinition[] stock;
        [SerializeField] private bool resaleMode;

        private string _message;
        private float _messageTime;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Open()
        {
            if (panel == null) return;
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

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                Close();
                return;
            }

            for (int i = 0; i < stock.Length && i < 9; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

                if (EconomyManager.Instance == null)
                {
                    _message = "Shop closed.";
                }
                else
                {
                    // TryPurchase reports success or the reason it failed,
                    // so it must only be called once per keypress.
                    string msg;
                    bool bought = resaleMode
                        ? EconomyManager.Instance.TryResell(stock[i], out msg)
                        : EconomyManager.Instance.TryPurchase(stock[i], out msg);
                    _message = msg;

                    if (bought && stock[i] != null && !resaleMode)
                    {
                        if (stock[i].category == ShopCategory.Seed)
                        {
                            Missions.MissionSystem.Instance?.Notify(
                                Missions.ObjectiveKind.BuySeeds, stock[i].itemId);
                        }
                        Missions.MissionSystem.Instance?.Notify(
                            Missions.ObjectiveKind.BuyItem, stock[i].itemId);
                    }
                }

                _messageTime = Time.unscaledTime;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (bodyText == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"<b>{shopTitle}</b>   (number key to {(resaleMode ? "sell" : "buy")}, E or Esc to leave)");
            sb.AppendLine();
            sb.AppendLine($"Money: ${(EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0)}");
            sb.AppendLine();

            for (int i = 0; i < stock.Length && i < 9; i++)
            {
                var item = stock[i];
                if (item == null) continue;

                string owned = item.category != ShopCategory.Seed
                               && EconomyManager.Instance != null
                               && EconomyManager.Instance.OwnsItem(item.itemId)
                    ? "  [owned]" : string.Empty;

                int shownPrice = resaleMode ? Mathf.Max(1, Mathf.RoundToInt(item.price * 0.55f)) : item.price;
                sb.AppendLine($"[{i + 1}]  {item.displayName,-26} ${shownPrice}{owned}");
            }

            if (!string.IsNullOrEmpty(_message) && Time.unscaledTime - _messageTime < 4f)
            {
                sb.AppendLine();
                sb.AppendLine(_message);
            }

            bodyText.text = sb.ToString();
        }
    }
}
