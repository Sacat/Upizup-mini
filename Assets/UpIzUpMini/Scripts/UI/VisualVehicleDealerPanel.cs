using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// MINI-160. User: "i want the vehicle buying to be just like the
    /// wardrobe changing... you can actually see the vehicle you want to
    /// choose it and buy it." Mirrors VisualWardrobePanel.cs's isolated
    /// render-stage pattern exactly (hidden-layer stage far off-scene,
    /// orthographic camera to a RenderTexture, IMGUI overlay) but shows a
    /// real vehicle prefab - the SAME one SpawnPurchasedVehicle places in
    /// the world, via VehicleSpawnController.GetPreviewPrefab - instead of
    /// a baked character mesh. Buying calls the existing
    /// EconomyManager.TryPurchase + VehicleSpawnController.SpawnPurchasedVehicle
    /// pipeline unchanged; this panel only adds the "see it first" step.
    /// </summary>
    public sealed class VisualVehicleDealerPanel : MonoBehaviour
    {
        static VisualVehicleDealerPanel instance;

        ShopPanelController dealerShop;
        Transform dealerAnchor;
        List<ShopItemDefinition> vehicles;
        int index;

        GameObject stage, model;
        Camera previewCamera;
        RenderTexture texture;
        float yaw;
        bool oldCursorVisible;
        CursorLockMode oldLock;
        float oldTimeScale;
        readonly List<Canvas> hiddenCanvases = new List<Canvas>();

        string message;
        float messageTime;

        GUIStyle title, label, small, button;

        public static bool IsOpen => instance != null && instance.dealerShop != null;

        public static void Open(ShopPanelController dealerShop, Transform anchor)
        {
            if (dealerShop == null || IsOpen || Time.timeScale == 0) return;
            if (instance == null) instance = new GameObject("Visual Vehicle Dealer").AddComponent<VisualVehicleDealerPanel>();
            instance.Begin(dealerShop, anchor);
        }

        void Begin(ShopPanelController shop, Transform anchor)
        {
            dealerShop = shop;
            dealerAnchor = anchor;
            vehicles = shop.Stock.Where(s => s != null && s.category == ShopCategory.Vehicle).ToList();
            index = 0;
            yaw = 35f;
            message = null;

            oldTimeScale = Time.timeScale; Time.timeScale = 0;
            oldCursorVisible = Cursor.visible; oldLock = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.enabled) { hiddenCanvases.Add(canvas); canvas.enabled = false; }

            stage = new GameObject("Vehicle dealer render stage");
            stage.transform.position = new Vector3(10000, -10000, 10000);
            texture = new RenderTexture(512, 512, 24) { antiAliasing = 2 };
            texture.Create();
            previewCamera = new GameObject("Vehicle dealer camera").AddComponent<Camera>();
            previewCamera.transform.SetParent(stage.transform, false);
            previewCamera.enabled = false;
            previewCamera.cullingMask = 1 << 31;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.09f, .12f, .15f);
            previewCamera.targetTexture = texture;
            previewCamera.nearClipPlane = .01f;
            foreach (float angle in new[] { -35f, 145f })
            {
                var light = new GameObject("Vehicle dealer light").AddComponent<Light>();
                light.transform.SetParent(stage.transform, false);
                light.type = LightType.Directional;
                light.intensity = angle < 0 ? 1.3f : .7f;
                light.cullingMask = 1 << 31;
                light.transform.rotation = Quaternion.Euler(35, angle, 0);
            }

            RebuildPreview();
        }

        void RebuildPreview()
        {
            if (model != null) { model.SetActive(false); Destroy(model); model = null; }
            if (vehicles == null || vehicles.Count == 0) return;

            var def = vehicles[index];
            var prefab = VehicleSpawnController.Instance != null ? VehicleSpawnController.Instance.GetPreviewPrefab(def.itemId) : null;
            if (prefab == null) return;

            model = Instantiate(prefab);
            model.name = "Preview_" + def.itemId;
            model.transform.SetParent(stage.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, yaw, 0);

            // Static preview only - no physics/input/AI should run on the
            // dealer floor. Every renderer moves to the hidden preview
            // layer so the main game camera never sees it.
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
            foreach (var col in model.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;

            Bounds bounds = default; bool found = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
            }
            if (!found) return;

            // Re-centre on the bounds without fighting the yaw rotation
            // already applied above (same approach VisualWardrobePanel
            // uses for the character preview).
            Vector3 localCenter = model.transform.InverseTransformPoint(bounds.center);
            model.transform.localPosition = -(model.transform.localRotation * localCenter);

            previewCamera.orthographic = true;
            previewCamera.orthographicSize = Mathf.Max(.6f, bounds.size.magnitude * .32f);
            previewCamera.transform.localPosition = new Vector3(0, bounds.size.y * .05f, bounds.size.magnitude * .8f + 2f);
            previewCamera.transform.LookAt(stage.transform.position);
            previewCamera.Render();
        }

        void Buy()
        {
            if (vehicles == null || index >= vehicles.Count) return;
            var def = vehicles[index];
            if (EconomyManager.Instance == null) { message = "Dealer closed."; messageTime = Time.unscaledTime; return; }

            bool bought = EconomyManager.Instance.TryPurchase(def, out string msg);
            message = msg;
            if (bought)
            {
                string spawnFeedback = VehicleSpawnController.Instance != null
                    ? VehicleSpawnController.Instance.SpawnPurchasedVehicle(def.itemId) : null;
                if (!string.IsNullOrEmpty(spawnFeedback)) message = $"{message} {spawnFeedback}";
                Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.BuyItem, def.itemId);
            }
            messageTime = Time.unscaledTime;
        }

        bool Owned(ShopItemDefinition def) => EconomyManager.Instance != null && EconomyManager.Instance.OwnsItem(def.itemId);

        public void Close()
        {
            if (dealerShop == null) return;
            dealerShop = null;
            Time.timeScale = oldTimeScale;
            Cursor.lockState = oldLock;
            Cursor.visible = oldCursorVisible;
            foreach (var canvas in hiddenCanvases) if (canvas != null) canvas.enabled = true;
            hiddenCanvases.Clear();
            if (stage != null) { stage.SetActive(false); Destroy(stage); stage = null; }
            model = null;
            if (texture != null) { texture.Release(); Destroy(texture); texture = null; }
        }

        void OnDestroy() { Close(); if (instance == this) instance = null; }

        void Update()
        {
            if (dealerShop == null) return;
            if (dealerAnchor != null)
            {
                var player = Character.CharacterSwitchManager.Instance?.Active?.root;
                if (player != null && Vector3.Distance(player.transform.position, dealerAnchor.position) > 6f) Close();
            }
        }

        void OnGUI()
        {
            if (dealerShop == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                label = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
                small = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 18, wordWrap = true };
            }

            int depth = GUI.depth; GUI.depth = -32000;
            var previousColor = GUI.color; GUI.color = Color.white;
            var oldMatrix = GUI.matrix; var safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / 1100f, safe.height / 700f);
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(.055f, .065f, .08f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(safe.x + (safe.width - 1100 * scale) / 2, Screen.height - safe.yMax + (safe.height - 700 * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));

            GUI.Label(new Rect(30, 20, 700, 45), "VEHICLE DEALER", title);
            GUI.Label(new Rect(30, 66, 1040, 28),
                $"Money: ${(EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0)}   •   walk away, E, or Esc to leave", small);
            GUI.DrawTexture(new Rect(30, 112, 410, 410), texture, ScaleMode.ScaleToFit);
            if (GUI.Button(new Rect(55, 532, 165, 40), "Rotate left", button)) { yaw -= 30; RebuildPreview(); }
            if (GUI.Button(new Rect(245, 532, 165, 40), "Rotate right", button)) { yaw += 30; RebuildPreview(); }

            if (vehicles == null || vehicles.Count == 0)
            {
                GUI.Label(new Rect(470, 175, 570, 90), "More vehicles unlock as you complete missions.", label);
            }
            else
            {
                for (int i = 0; i < vehicles.Count; i++)
                    if (GUI.Button(new Rect(470, 112 + i * 56, 575, 48),
                        $"{vehicles[i].displayName}  —  ${vehicles[i].price}{(Owned(vehicles[i]) ? "  [OWNED]" : "")}", button))
                    { index = i; yaw = 35f; RebuildPreview(); }

                var def = vehicles[index];
                float y = 112 + vehicles.Count * 56 + 20;
                GUI.Label(new Rect(470, y, 570, 45), def.displayName, title);
                GUI.Label(new Rect(470, y + 46, 570, 28), $"${def.price}" + (Owned(def) ? "  —  already owned" : ""), label);
                if (!string.IsNullOrEmpty(def.description))
                    GUI.Label(new Rect(470, y + 80, 570, 60), def.description, small);

                if (Owned(def))
                    GUI.Label(new Rect(470, y + 150, 570, 40), "Already yours — check where you parked it.", label);
                else if (GUI.Button(new Rect(470, y + 150, 300, 48), "Buy this vehicle", button))
                    Buy();
            }

            if (!string.IsNullOrEmpty(message) && Time.unscaledTime - messageTime < 4f)
                GUI.Label(new Rect(470, 600, 575, 40), message, small);

            if (GUI.Button(new Rect(855, 622, 190, 45), "Close", button)) Close();
            GUI.matrix = oldMatrix; GUI.color = previousColor; GUI.depth = depth;
        }
    }
}
