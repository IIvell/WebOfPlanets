using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Attach to a Canvas (Screen Space – Overlay).
    // Assign ConnectionManager in Inspector.
    public class NetworkMapUI : MonoBehaviour
    {
        [SerializeField] private ConnectionManager connectionManager;

        [Header("Colours")]
        [SerializeField] private Color hubColour      = new Color(1f, 0.85f, 0f);
        [SerializeField] private Color planetColour   = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private Color bgColour       = new Color(0f, 0.05f, 0.1f, 0.92f);

        private GameObject _panel;
        private RectTransform _mapArea;
        private readonly List<GameObject> _nodes = new();
        private readonly List<GameObject> _lines = new();

        private bool _isOpen;

        void Awake()
        {
            BuildPanelHierarchy();
            _panel.SetActive(false);
        }

        void Update()
        {
            if (_isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open()
        {
            _isOpen = true;
            _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            Refresh();
        }

        public void Close()
        {
            _isOpen = false;
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        public bool IsOpen => _isOpen;

        // ── Build UI hierarchy once ───────────────────────────────────────────

        private void BuildPanelHierarchy()
        {
            // Full-screen panel
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
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot     = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -10f);
            titleRT.sizeDelta = new Vector2(0f, 50f);
            var titleTxt = title.AddComponent<TextMeshProUGUI>();
            titleTxt.text      = "MREŽNA MAPA";
            titleTxt.fontSize  = 28;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color     = Color.white;

            // Close button (top-right)
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(_panel.transform, false);
            var closeBtnRT = closeBtn.AddComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(1f, 1f);
            closeBtnRT.anchorMax = new Vector2(1f, 1f);
            closeBtnRT.pivot     = new Vector2(1f, 1f);
            closeBtnRT.anchoredPosition = new Vector2(-10f, -10f);
            closeBtnRT.sizeDelta = new Vector2(100f, 40f);
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

            // Map area (leaves margin for title and legend)
            var mapGO = new GameObject("MapArea");
            mapGO.transform.SetParent(_panel.transform, false);
            _mapArea = mapGO.AddComponent<RectTransform>();
            _mapArea.anchorMin = new Vector2(0.02f, 0.06f);
            _mapArea.anchorMax = new Vector2(0.98f, 0.90f);
            _mapArea.offsetMin = Vector2.zero;
            _mapArea.offsetMax = Vector2.zero;

            // Legend (bottom)
            BuildLegend(_panel.transform);
        }

        private void BuildLegend(Transform parent)
        {
            var legend = new GameObject("Legend");
            legend.transform.SetParent(parent, false);
            var rt = legend.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta = new Vector2(0f, 24f);
            var txt = legend.AddComponent<TextMeshProUGUI>();
            txt.text      = "<color=#FFD700>■</color> Hub   " +
                            "<color=#4DCCFF>■</color> Planet   " +
                            "<color=#00FF00>—</color> Veza zdrava   " +
                            "<color=#FF4444>—</color> Veza oštećena";
            txt.fontSize  = 13;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color     = Color.white;
        }

        // ── Refresh map content ───────────────────────────────────────────────

        private void Refresh()
        {
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

            // Avoid division by zero when all planets are on the same axis
            if (Mathf.Approximately(maxX, minX)) { minX -= 1f; maxX += 1f; }
            if (Mathf.Approximately(maxZ, minZ)) { minZ -= 1f; maxZ += 1f; }

            Rect area  = _mapArea.rect;
            float pad  = 50f;
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

            // Node circle
            var nodeGO = new GameObject($"Node_{planet.name}");
            nodeGO.transform.SetParent(_mapArea, false);
            var rt = nodeGO.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = nodeGO.AddComponent<Image>();
            img.color = planet.IsHub ? hubColour : planetColour;

            // Label below node
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(nodeGO.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(0f, -size * 0.5f - 12f);
            labelRT.sizeDelta = new Vector2(120f, 22f);
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
            lineGO.transform.SetParent(_mapArea, false);
            lineGO.transform.SetAsFirstSibling();

            var rt  = lineGO.AddComponent<RectTransform>();
            Vector2 dir  = to - from;
            float dist   = dir.magnitude;
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.sizeDelta        = new Vector2(dist, 3f);
            rt.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var img  = lineGO.AddComponent<Image>();
            img.color = Color.Lerp(new Color(1f, 0.27f, 0.27f), new Color(0.2f, 1f, 0.4f), health / 100f);

            _lines.Add(lineGO);
        }
    }
}
