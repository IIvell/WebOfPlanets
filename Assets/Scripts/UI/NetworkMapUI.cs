using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Attach to a Canvas (Screen Space – Overlay).
    // Assign ConnectionManager, PlayerController, PlayerCamera, Interactor in Inspector.
    public class NetworkMapUI : MonoBehaviour
    {
        [SerializeField] private ConnectionManager connectionManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Interactor interactor;

        [Header("Colours")]
        [SerializeField] private Color hubColour    = new Color(1f, 0.85f, 0f);
        [SerializeField] private Color planetColour = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private Color bgColour     = new Color(0f, 0.05f, 0.1f, 0.92f);

        [Header("Zoom & Pan")]
        [SerializeField] private float zoomMin      = 0.3f;
        [SerializeField] private float zoomMax      = 3f;
        [SerializeField] private float zoomStep     = 0.15f;

        private GameObject     _panel;
        private RectTransform  _mapArea;
        private RectTransform  _mapContent;
        private TextMeshProUGUI _zoomLabel;

        private readonly List<GameObject> _nodes = new();
        private readonly List<GameObject> _lines = new();

        private bool    _isOpen;
        private bool    _subscribed;
        private bool    _refreshQueued;
        private float   _lastRefreshTime;
        private const float HealthRefreshInterval = 0.25f;
        private float   _zoom = 1f;
        private Vector2 _pan  = Vector2.zero;

        // Pan drag state
        private bool    _dragging;
        private Vector2 _dragStartMouse;
        private Vector2 _dragStartPan;

        void Awake()
        {
            BuildPanelHierarchy();
            _panel.SetActive(false);
        }

        void Update()
        {
            if (!_isOpen) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            // Eventi samo zakažu refresh — izvršava se najviše jednom po frameu
            if (_refreshQueued)
            {
                _refreshQueued = false;
                Refresh();
            }

            HandleZoom();
            HandlePan();
            ApplyView();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open()
        {
            _zoom = 1f;
            _pan  = Vector2.zero;
            _isOpen = true;
            _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            playerController?.SetInputEnabled(false);
            playerCamera?.SetInputEnabled(false);
            if (interactor != null) interactor.enabled = false;
            Subscribe();
            Canvas.ForceUpdateCanvases();
            Refresh();
            ApplyView();
        }

        public void Close()
        {
            Unsubscribe();
            _isOpen = false;
            _refreshQueued = false;
            _dragging = false;
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            playerController?.SetInputEnabled(true);
            playerCamera?.SetInputEnabled(true);
            if (interactor != null) interactor.enabled = true;
        }

        public bool IsOpen => _isOpen;

        // ── Event-based osvježavanje dok je mapa otvorena ─────────────────────

        // Flag štiti od dvostruke pretplate ako se Open() pozove dvaput zaredom.
        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameEventBus.OnConnectionHealthChanged += OnConnectionHealthChangedEvent;
            GameEventBus.OnConnectionCreated       += OnConnectionChangedEvent;
            GameEventBus.OnConnectionDestroyed     += OnConnectionChangedEvent;
            GameEventBus.OnPlanetDiscovered        += OnPlanetDiscoveredEvent;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEventBus.OnConnectionHealthChanged -= OnConnectionHealthChangedEvent;
            GameEventBus.OnConnectionCreated       -= OnConnectionChangedEvent;
            GameEventBus.OnConnectionDestroyed     -= OnConnectionChangedEvent;
            GameEventBus.OnPlanetDiscovered        -= OnPlanetDiscoveredEvent;
        }

        // Health event se diže svaki frame po degradirajućoj vezi — throttle, inače bi
        // puni rebuild mape išao N puta po frameu. Strukturne promjene su rijetke pa
        // smiju zakazati refresh odmah.
        private void OnConnectionHealthChangedEvent(ConnectionHealthChangedEvent _)
        {
            if (Time.unscaledTime - _lastRefreshTime >= HealthRefreshInterval)
                _refreshQueued = true;
        }

        private void OnConnectionChangedEvent(ConnectionEvent _) => _refreshQueued = true;
        private void OnPlanetDiscoveredEvent(Transform _)        => _refreshQueued = true;

        void OnDestroy() => Unsubscribe();

        // ── Zoom & Pan ────────────────────────────────────────────────────────

        private void HandleZoom()
        {
            float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (Mathf.Abs(scroll) < 0.01f) return;

            float delta = scroll > 0f ? zoomStep : -zoomStep;
            _zoom = Mathf.Clamp(_zoom + delta, zoomMin, zoomMax);
        }

        private void HandlePan()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _dragging       = true;
                _dragStartMouse = mouse.position.ReadValue();
                _dragStartPan   = _pan;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
                _dragging = false;

            if (_dragging && mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.position.ReadValue() - _dragStartMouse;
                _pan = _dragStartPan + delta;
            }
        }

        private void ApplyView()
        {
            _mapContent.localScale      = new Vector3(_zoom, _zoom, 1f);
            _mapContent.anchoredPosition = _pan;
            if (_zoomLabel != null)
                _zoomLabel.text = $"Zoom: {_zoom * 100f:F0}%  |  Scroll = zoom  |  Drag = pan  |  ESC = zatvori";
        }

        // ── Build UI hierarchy once ───────────────────────────────────────────

        private void BuildPanelHierarchy()
        {
            _panel = new GameObject("NetworkMap_Panel");
            _panel.transform.SetParent(transform, false);
            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = bgColour;

            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(_panel.transform, false);
            var titleRT = title.AddComponent<RectTransform>();
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -10f);
            titleRT.sizeDelta        = new Vector2(0f, 50f);
            var titleTxt = title.AddComponent<TextMeshProUGUI>();
            titleTxt.text      = "MREŽNA MAPA";
            titleTxt.fontSize  = 28;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color     = Color.white;

            // Close button (top-right)
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(_panel.transform, false);
            var closeBtnRT = closeBtn.AddComponent<RectTransform>();
            closeBtnRT.anchorMin        = new Vector2(1f, 1f);
            closeBtnRT.anchorMax        = new Vector2(1f, 1f);
            closeBtnRT.pivot            = new Vector2(1f, 1f);
            closeBtnRT.anchoredPosition = new Vector2(-10f, -10f);
            closeBtnRT.sizeDelta        = new Vector2(110f, 40f);
            var closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.color = new Color(0.6f, 0.1f, 0.1f);
            var btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Close);
            var closeLabelGO = new GameObject("Label");
            closeLabelGO.transform.SetParent(closeBtn.transform, false);
            var closeLabelRT = closeLabelGO.AddComponent<RectTransform>();
            closeLabelRT.anchorMin = Vector2.zero;
            closeLabelRT.anchorMax = Vector2.one;
            closeLabelRT.offsetMin = Vector2.zero;
            closeLabelRT.offsetMax = Vector2.zero;
            var closeTxt = closeLabelGO.AddComponent<TextMeshProUGUI>();
            closeTxt.text      = "ESC / Zatvori";
            closeTxt.fontSize  = 14;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color     = Color.white;

            // Map area with Mask (clips zoomed/panned content)
            var mapGO = new GameObject("MapArea");
            mapGO.transform.SetParent(_panel.transform, false);
            _mapArea           = mapGO.AddComponent<RectTransform>();
            _mapArea.anchorMin = new Vector2(0.02f, 0.08f);
            _mapArea.anchorMax = new Vector2(0.98f, 0.90f);
            _mapArea.offsetMin = Vector2.zero;
            _mapArea.offsetMax = Vector2.zero;
            mapGO.AddComponent<RectMask2D>();

            // Map content — this is what zooms & pans
            var contentGO = new GameObject("MapContent");
            contentGO.transform.SetParent(_mapArea, false);
            _mapContent           = contentGO.AddComponent<RectTransform>();
            _mapContent.anchorMin = new Vector2(0.5f, 0.5f);
            _mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            _mapContent.pivot     = new Vector2(0.5f, 0.5f);
            _mapContent.sizeDelta = Vector2.zero;

            // Legend / hint bar (bottom)
            BuildLegend(_panel.transform);
        }

        private void BuildLegend(Transform parent)
        {
            var legend = new GameObject("Legend");
            legend.transform.SetParent(parent, false);
            var rt = legend.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta        = new Vector2(0f, 20f);
            _zoomLabel          = legend.AddComponent<TextMeshProUGUI>();
            _zoomLabel.text     = "Scroll = zoom  |  Drag = pan  |  ESC = zatvori";
            _zoomLabel.fontSize  = 12;
            _zoomLabel.alignment = TextAlignmentOptions.Center;
            _zoomLabel.color     = new Color(0.7f, 0.7f, 0.7f);
        }

        // ── Refresh map content ───────────────────────────────────────────────

        private void Refresh()
        {
            _lastRefreshTime = Time.unscaledTime;

            foreach (var n in _nodes) Destroy(n);
            foreach (var l in _lines) Destroy(l);
            _nodes.Clear();
            _lines.Clear();

            Planet[] planets = FindObjectsByType<Planet>(FindObjectsSortMode.None);
            if (planets.Length == 0) return;

            Dictionary<Planet, Vector2> positions = ComputeNodePositions(planets);

            // Lines first (behind nodes)
            if (connectionManager != null)
            {
                foreach (var conn in connectionManager.Connections)
                {
                    if (conn == null) continue;
                    var pA = conn.PlanetA?.GetComponent<Planet>();
                    var pB = conn.PlanetB?.GetComponent<Planet>();
                    if (pA == null || pB == null) continue;
                    if (!positions.TryGetValue(pA, out Vector2 posA)) continue;
                    if (!positions.TryGetValue(pB, out Vector2 posB)) continue;
                    DrawLine(posA, posB, conn.Health);
                }
            }

            // Nodes on top
            foreach (var planet in planets)
            {
                if (!positions.TryGetValue(planet, out Vector2 pos)) continue;
                DrawNode(planet, pos);
            }
        }

        private Dictionary<Planet, Vector2> ComputeNodePositions(Planet[] planets)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var p in planets)
            {
                Vector3 wp = p.transform.position;
                if (wp.x < minX) minX = wp.x;
                if (wp.x > maxX) maxX = wp.x;
                if (wp.z < minZ) minZ = wp.z;
                if (wp.z > maxZ) maxZ = wp.z;
            }

            if (Mathf.Approximately(maxX, minX)) { minX -= 1f; maxX += 1f; }
            if (Mathf.Approximately(maxZ, minZ)) { minZ -= 1f; maxZ += 1f; }

            // Use the actual map area size for layout, independent of zoom
            Rect area  = _mapArea.rect;
            float pad  = 60f;
            float halfW = area.width  * 0.5f - pad;
            float halfH = area.height * 0.5f - pad;

            var result = new Dictionary<Planet, Vector2>();
            foreach (var p in planets)
            {
                float nx = Mathf.InverseLerp(minX, maxX, p.transform.position.x);
                float nz = Mathf.InverseLerp(minZ, maxZ, p.transform.position.z);
                result[p] = new Vector2(
                    Mathf.Lerp(-halfW, halfW, nx),
                    Mathf.Lerp(-halfH, halfH, nz)
                );
            }
            return result;
        }

        private void DrawNode(Planet planet, Vector2 pos)
        {
            float size = planet.IsHub ? 28f : 20f;

            var nodeGO = new GameObject($"Node_{planet.name}");
            nodeGO.transform.SetParent(_mapContent, false);
            var rt = nodeGO.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(size, size);
            var img = nodeGO.AddComponent<Image>();
            img.color = planet.IsHub ? hubColour : planetColour;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(nodeGO.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(0f, -size * 0.5f - 12f);
            labelRT.sizeDelta        = new Vector2(120f, 22f);
            var txt = labelGO.AddComponent<TextMeshProUGUI>();
            txt.text      = planet.name;
            txt.fontSize  = planet.IsHub ? 13 : 11;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color     = planet.IsHub ? hubColour : Color.white;

            _nodes.Add(nodeGO);
        }

        private void DrawLine(Vector2 from, Vector2 to, float health)
        {
            var lineGO = new GameObject("Line");
            lineGO.transform.SetParent(_mapContent, false);
            lineGO.transform.SetAsFirstSibling();

            var rt       = lineGO.AddComponent<RectTransform>();
            Vector2 dir  = to - from;
            float dist   = dir.magnitude;
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.sizeDelta        = new Vector2(dist, 3f);
            rt.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var img = lineGO.AddComponent<Image>();
            img.color = Color.Lerp(new Color(1f, 0.27f, 0.27f), new Color(0.2f, 1f, 0.4f), health / 100f);

            _lines.Add(lineGO);
        }
    }
}
