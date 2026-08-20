using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// MINI-073. "make the phramacy items be able to store in your inventory
    /// and able to use after even food as well" + "you can have a main button
    /// for crops and suboptions same for pharmacy and food and then we will
    /// use that for the weapons later."
    ///
    /// A category menu (CROPS / FOOD / PHARMACY / WEAPONS) opened with I, each
    /// category listing its held items as numbered sub-options. Food/Pharmacy
    /// rows are USABLE (number key applies the item's effect and consumes one
    /// - see EconomyManager.UseConsumable); Crops is read-only (crops already
    /// have their own plant/sell flow via CropSelectionController and the
    /// Produce Buyer, this is just a place to SEE what you're holding);
    /// Weapons is a reserved, empty placeholder category for later, per the
    /// user's own explicit "we will use that for the weapons later."
    ///
    /// Deliberately legacy IMGUI (OnGUI), matching InteractionDetector's own
    /// prompt/feedback boxes rather than a UGUI Canvas - same reasoning: no
    /// mouse-driven UI exists yet in the moment-to-moment gameplay HUD, only
    /// in modal shop panels, and this is closer in spirit to those number-key
    /// list menus (see ShopPanelController) than to a shop screen.
    /// </summary>
    public class InventoryPanelController : MonoBehaviour
    {
        private enum Category { None, Crops, Food, Pharmacy, Weapons }

        [SerializeField] private KeyCode toggleKey = KeyCode.I;
        [SerializeField] private CropDefinition[] allCrops = new CropDefinition[0];

        private bool _open;
        private Category _category = Category.None;
        private string _message;
        private float _messageTime;

        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _open = !_open;
                if (_open) { _category = Category.None; }
                Cursor.lockState = _open ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = _open;
            }

            if (!_open) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_category != Category.None) { _category = Category.None; return; }
                _open = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                return;
            }

            if (_category == Category.None)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) _category = Category.Crops;
                else if (Input.GetKeyDown(KeyCode.Alpha2)) _category = Category.Food;
                else if (Input.GetKeyDown(KeyCode.Alpha3)) _category = Category.Pharmacy;
                else if (Input.GetKeyDown(KeyCode.Alpha4)) _category = Category.Weapons;
                return;
            }

            // Backspace steps back to the category menu without closing the
            // whole panel - Escape from here does the same (handled above).
            if (Input.GetKeyDown(KeyCode.Backspace)) { _category = Category.None; return; }

            if (_category == Category.Food || _category == Category.Pharmacy)
            {
                var shown = ShownItems();
                for (int i = 0; i < shown.Count && i < 9; i++)
                {
                    if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
                    if (EconomyManager.Instance == null) continue;

                    EconomyManager.Instance.UseConsumable(shown[i].item.itemId, out string msg);
                    _message = msg;
                    _messageTime = Time.unscaledTime;
                }
            }
        }

        private List<(ShopItemDefinition item, int count)> ShownItems()
        {
            var economy = EconomyManager.Instance;
            if (economy == null) return new List<(ShopItemDefinition, int)>();

            ShopCategory cat = _category == Category.Food ? ShopCategory.Food : ShopCategory.Enhancement;
            return economy.GetConsumablesInCategory(cat);
        }

        private void EnsureStyles()
        {
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    normal = { textColor = Color.white },
                };
            }
            _boxStyle.fontSize = Mathf.RoundToInt(22 * scale);

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                };
            }
            _titleStyle.fontSize = Mathf.RoundToInt(28 * scale);
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 1080f);
            float w = 560f * scale;
            float h = 480f * scale;
            var rect = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>INVENTORY</b>  [ Esc ] close");
            sb.AppendLine();

            if (_category == Category.None)
            {
                sb.AppendLine("[ 1 ] Crops");
                sb.AppendLine("[ 2 ] Food");
                sb.AppendLine("[ 3 ] Pharmacy");
                sb.AppendLine("[ 4 ] Weapons  (coming soon)");
            }
            else
            {
                sb.AppendLine($"<b>{_category.ToString().ToUpperInvariant()}</b>  [ Backspace ] back");
                sb.AppendLine();

                switch (_category)
                {
                    case Category.Crops:
                        var economy = EconomyManager.Instance;
                        bool any = false;
                        foreach (var crop in allCrops)
                        {
                            if (crop == null) continue;
                            int count = economy != null ? economy.GetCount(crop.cropId) : 0;
                            if (count <= 0) continue;
                            any = true;
                            sb.AppendLine($"{crop.displayName}  x{count}");
                        }
                        if (!any) sb.AppendLine("(none held)");
                        sb.AppendLine();
                        sb.AppendLine("Crops are planted/sold in the field, not used here.");
                        break;

                    case Category.Food:
                    case Category.Pharmacy:
                        var shown = ShownItems();
                        if (shown.Count == 0) sb.AppendLine("(none held)");
                        for (int i = 0; i < shown.Count && i < 9; i++)
                        {
                            sb.AppendLine($"[ {i + 1} ] {shown[i].item.displayName}  x{shown[i].count}");
                        }
                        break;

                    case Category.Weapons:
                        sb.AppendLine("Nothing here yet.");
                        break;
                }
            }

            if (!string.IsNullOrEmpty(_message) && Time.unscaledTime - _messageTime < 3f)
            {
                sb.AppendLine();
                sb.AppendLine(_message);
            }

            GUI.Box(rect, sb.ToString(), _boxStyle);
        }
    }
}
