using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;

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

        // MINI-082, user: "I still have to press E to close the shops box
        // I should just walk and it fades." The panel now fades itself out
        // (via CanvasGroup) both on E/Esc AND the moment the player walks
        // more than autoCloseRange from the NPC who opened it - no more
        // hard requirement to press E just to leave.
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float autoCloseRange = 4.5f;
        [SerializeField] private float fadeSeconds = 0.35f;

        private Transform _anchor;
        private float _targetAlpha;
        private bool _pendingDeactivate;

        private string _message;
        private float _messageTime;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>Opens the shop, fading in. <paramref name="anchor"/> is
        /// the NPC's own transform - walking more than autoCloseRange from
        /// it fades the panel shut on its own. Null (e.g. legacy callers)
        /// disables the auto-close check, same as before this change.</summary>
        public void Open(Transform anchor = null)
        {
            if (panel == null) return;
            _anchor = anchor;
            _pendingDeactivate = false;
            _targetAlpha = 1f;
            panel.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }

        /// <summary>Closes instantly - used once a fade-out finishes, and
        /// still available directly for anything that wants no animation.</summary>
        public void Close()
        {
            if (panel == null) return;
            panel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            _pendingDeactivate = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void BeginClose()
        {
            _pendingDeactivate = true;
            _targetAlpha = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!IsOpen) return;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha, _targetAlpha,
                    Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
            }

            if (_pendingDeactivate)
            {
                if (canvasGroup == null || canvasGroup.alpha <= 0.001f) Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                BeginClose();
                return;
            }

            if (_anchor != null)
            {
                var player = Character.CharacterSwitchManager.Instance?.Active?.root;
                if (player != null
                    && Vector3.Distance(player.transform.position, _anchor.position) > autoCloseRange)
                {
                    BeginClose();
                    return;
                }
            }

            int visibleIndex = 0;
            for (int i = 0; i < stock.Length && visibleIndex < 9; i++)
            {
                var item = stock[i];
                if (!ShouldShow(item)) continue;
                int keyIndex = visibleIndex++;
                if (!Input.GetKeyDown(KeyCode.Alpha1 + keyIndex)) continue;

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

                        // MINI-065: a vehicle purchase (currently just the
                        // TMAX) also spawns the real thing in the world -
                        // SpawnPurchasedVehicle no-ops (returns null) for
                        // every item it doesn't recognise, so this is a
                        // no-op for every other shop's stock.
                        string spawnFeedback = Vehicles.VehicleSpawnController.Instance?.SpawnPurchasedVehicle(stock[i].itemId);
                        if (!string.IsNullOrEmpty(spawnFeedback))
                        {
                            _message = $"{_message} {spawnFeedback}";
                        }

                        _message = $"{_message} {PurchaseReaction(stock[i])}";
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
            sb.AppendLine($"<b>{shopTitle}</b>   (number key to {(resaleMode ? "sell" : "buy")} - walk away, E, or Esc to leave)");
            sb.AppendLine();
            sb.AppendLine($"Money: ${(EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0)}");
            sb.AppendLine();

            int visibleIndex = 0;
            for (int i = 0; i < stock.Length && visibleIndex < 9; i++)
            {
                var item = stock[i];
                if (!ShouldShow(item)) continue;

                string owned = item.category != ShopCategory.Seed
                               && IsOwnedByActiveCharacter(item)
                    ? "  [owned]" : string.Empty;

                int shownPrice = resaleMode ? Mathf.Max(1, Mathf.RoundToInt(item.price * 0.55f)) : item.price;
                sb.AppendLine($"[{visibleIndex + 1}]  {item.displayName,-26} ${shownPrice}{owned}");
                visibleIndex++;
            }

            if (visibleIndex == 0)
            {
                sb.AppendLine(resaleMode
                    ? "Nothing from your current outfit to sell."
                    : "More stock unlocks as you complete missions.");
            }

            if (!string.IsNullOrEmpty(_message) && Time.unscaledTime - _messageTime < 4f)
            {
                sb.AppendLine();
                sb.AppendLine(_message);
            }

            bodyText.text = sb.ToString();
        }

        private bool ShouldShow(ShopItemDefinition item)
        {
            if (item == null || !ProgressionGate.IsItemUnlocked(item)) return false;
            return !resaleMode || IsOwnedByActiveCharacter(item);
        }

        private static bool IsOwnedByActiveCharacter(ShopItemDefinition item)
        {
            var economy = EconomyManager.Instance;
            if (economy == null || item == null) return false;
            int characterIndex = Character.CharacterSwitchManager.Instance != null
                ? Character.CharacterSwitchManager.Instance.ActiveIndex : 0;
            bool wearable = item.category == ShopCategory.Clothing
                            || item.category == ShopCategory.Footwear
                            || item.category == ShopCategory.Accessory;
            return wearable
                ? economy.OwnsItem(item.itemId, characterIndex)
                : economy.OwnsItem(item.itemId);
        }

        private static string PurchaseReaction(ShopItemDefinition item)
        {
            if (item == null) return string.Empty;
            return item.category switch
            {
                ShopCategory.Clothing or ShopCategory.Footwear or ShopCategory.Accessory
                    => "Yah, I looking more fresh now.",
                ShopCategory.Vehicle or ShopCategory.Boat
                    => "Yah, I can move better now.",
                ShopCategory.Food
                    => "Yah, I can put something in my stomach now.",
                ShopCategory.Land or ShopCategory.Property
                    => "Yah, I can do something for myself now.",
                _ => string.Empty,
            };
        }
    }
}
