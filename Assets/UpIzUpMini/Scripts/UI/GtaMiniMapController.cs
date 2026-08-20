using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Mobile-conscious GTA-style radar. A small orthographic camera follows
    /// and rotates with the active hero; data markers become uncluttered blips.
    /// At 50% heat the radar receives a transparent red/blue wanted wash.
    /// </summary>
    public sealed class GtaMiniMapController : MonoBehaviour
    {
        [SerializeField] private Camera mapCamera;
        [SerializeField] private RawImage mapImage;
        [SerializeField] private RectTransform blipRoot;
        [SerializeField] private Image wantedOverlay;
        [SerializeField] private Text wantedLabel;
        [SerializeField] private float cameraHeight = 75f;
        [SerializeField] private float orthographicSize = 52f;
        [SerializeField] private int textureSize = 256;

        private readonly List<(GtaMiniMapMarker marker, RectTransform icon)> _blips = new();
        private RenderTexture _texture;
        private Sprite _dotSprite;

        private void Start()
        {
            if (mapCamera == null || mapImage == null || blipRoot == null) return;
            textureSize = Mathf.Clamp(textureSize, 128, 512);
            _texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "GrandBayMiniMapRT",
                filterMode = FilterMode.Bilinear,
                useMipMap = false
            };
            mapCamera.targetTexture = _texture;
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = orthographicSize;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.06f, 0.11f, 0.08f, 1f);
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;
            mapImage.texture = _texture;
            BuildBlips();
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
            if (_dotSprite != null)
            {
                Destroy(_dotSprite.texture);
                Destroy(_dotSprite);
            }
        }

        private void LateUpdate()
        {
            Transform player = CharacterSwitchManager.Instance?.Active?.root?.transform;
            if (player == null || mapCamera == null) return;

            float heading = player.eulerAngles.y;
            mapCamera.transform.SetPositionAndRotation(
                player.position + Vector3.up * cameraHeight,
                Quaternion.Euler(90f, heading, 0f));
            UpdateBlips();
            UpdateWantedOverlay();
        }

        private void BuildBlips()
        {
            _dotSprite = CreateCircleSprite();
            foreach (GtaMiniMapMarker marker in FindObjectsByType<GtaMiniMapMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject iconObject = new GameObject("Blip_" + marker.DisplayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform icon = iconObject.GetComponent<RectTransform>();
                icon.SetParent(blipRoot, false);
                float size = marker.Kind is MiniMapMarkerKind.Police or MiniMapMarkerKind.Mission ? 15f : 11f;
                icon.sizeDelta = new Vector2(size, size);
                Image image = iconObject.GetComponent<Image>();
                image.sprite = _dotSprite;
                image.color = marker.Colour;
                image.raycastTarget = false;
                _blips.Add((marker, icon));
            }
        }

        private void UpdateBlips()
        {
            foreach ((GtaMiniMapMarker marker, RectTransform icon) in _blips)
            {
                bool visible = marker != null && marker.gameObject.activeInHierarchy && marker.IsUnlocked;
                if (visible)
                {
                    Vector3 viewport = mapCamera.WorldToViewportPoint(marker.transform.position + Vector3.up * 1.5f);
                    visible = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
                    if (visible)
                    {
                        Rect rect = blipRoot.rect;
                        icon.anchoredPosition = new Vector2((viewport.x - 0.5f) * rect.width, (viewport.y - 0.5f) * rect.height);
                    }
                }
                icon.gameObject.SetActive(visible);
            }
        }

        private void UpdateWantedOverlay()
        {
            float heat = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
            bool wanted = heat >= 50f;
            if (wantedOverlay != null)
            {
                wantedOverlay.gameObject.SetActive(wanted);
                if (wanted)
                {
                    float pulse = Mathf.PingPong(Time.unscaledTime * 2.4f, 1f);
                    Color red = new Color(0.95f, 0.04f, 0.04f, Mathf.Lerp(0.08f, 0.22f, heat / 100f));
                    Color blue = new Color(0.05f, 0.25f, 1f, red.a);
                    wantedOverlay.color = Color.Lerp(red, blue, pulse);
                }
            }
            if (wantedLabel != null)
            {
                wantedLabel.gameObject.SetActive(wanted);
                wantedLabel.text = wanted ? $"POLICE HEAT  {Mathf.RoundToInt(heat)}%" : string.Empty;
            }
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MiniMapBlipCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Vector2 centre = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.45f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), centre) <= radius ? Color.white : Color.clear;
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 32f);
        }
    }
}
