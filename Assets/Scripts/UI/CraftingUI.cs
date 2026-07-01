using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private CraftingRecipe[] recipes;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Interactor interactor;
        [SerializeField] private float spawnForwardDistance = 4f;

        private const float RowH    = 72f;
        private const float RowGap  = 6f;
        private const float PadTop  = 8f;
        private const float PadBot  = 8f;

        private GameObject    _panel;
        private Transform     _contentRoot;
        private RectTransform _contentRT;

        public bool IsOpen => _panel.activeSelf;

        void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        void Update()
        {
            if (_panel.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame)
                Hide();
        }

        public void Show()
        {
            _panel.SetActive(true);
            Refresh();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            playerController?.SetInputEnabled(false);
            playerCamera?.SetInputEnabled(false);
            if (interactor != null) interactor.enabled = false;
        }

        public void Hide()
        {
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            playerController?.SetInputEnabled(true);
            playerCamera?.SetInputEnabled(true);
            if (interactor != null) interactor.enabled = true;
        }

        private void Refresh()
        {
            var old = new List<GameObject>();
            foreach (Transform child in _contentRoot)
                old.Add(child.gameObject);
            foreach (var go in old)
                go.transform.SetParent(null);

            int count = 0;
            if (recipes != null)
                for (int i = 0; i < recipes.Length; i++)
                    if (recipes[i] != null)
                    {
                        float yPos = -(PadTop + count * (RowH + RowGap) + RowH * 0.5f);
                        BuildRow(recipes[i], i, yPos);
                        count++;
                    }

            float totalH = PadTop + PadBot + count * RowH + Mathf.Max(0, count - 1) * RowGap;
            _contentRT.sizeDelta = new Vector2(_contentRT.sizeDelta.x, totalH);

            foreach (var go in old)
                Destroy(go);
        }

        private void BuildRow(CraftingRecipe recipe, int index, float yPos)
        {
            var row = new GameObject("Row_" + recipe.displayName);
            row.transform.SetParent(_contentRoot, false);

            // Anchor: stretch horizontally, pin to top
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin        = new Vector2(0f, 1f);
            rowRT.anchorMax        = new Vector2(1f, 1f);
            rowRT.pivot            = new Vector2(0.5f, 0.5f);
            rowRT.anchoredPosition = new Vector2(0f, yPos);
            rowRT.sizeDelta        = new Vector2(-16f, RowH);   // 8px inset each side

            row.AddComponent<Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

            // Name + type label
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(row.transform, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin        = new Vector2(0f, 0.5f);
            nameRT.anchorMax        = new Vector2(0f, 0.5f);
            nameRT.pivot            = new Vector2(0f, 0.5f);
            nameRT.anchoredPosition = new Vector2(10f, 0f);
            nameRT.sizeDelta        = new Vector2(150f, 60f);
            var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.text     = $"<b>{recipe.displayName}</b>\n<size=10><color=#aaaaaa>{TypeLabel(recipe.resultType)}</color></size>";
            nameTxt.fontSize = 14;
            nameTxt.color    = Color.white;

            // Ingredients
            var ingGO = new GameObject("Ingredients");
            ingGO.transform.SetParent(row.transform, false);
            var ingRT = ingGO.AddComponent<RectTransform>();
            ingRT.anchorMin        = new Vector2(0f, 0.5f);
            ingRT.anchorMax        = new Vector2(0f, 0.5f);
            ingRT.pivot            = new Vector2(0f, 0.5f);
            ingRT.anchoredPosition = new Vector2(175f, 0f);
            ingRT.sizeDelta        = new Vector2(245f, 60f);
            var ingTxt = ingGO.AddComponent<TextMeshProUGUI>();
            ingTxt.text     = BuildIngredientsText(recipe);
            ingTxt.fontSize = 11;
            ingTxt.color    = Color.white;

            // Craft button
            var btnGO = new GameObject("CraftBtn");
            btnGO.transform.SetParent(row.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(1f, 0.5f);
            btnRT.anchorMax        = new Vector2(1f, 0.5f);
            btnRT.pivot            = new Vector2(1f, 0.5f);
            btnRT.anchoredPosition = new Vector2(-10f, 0f);
            btnRT.sizeDelta        = new Vector2(88f, 44f);
            var btnImg = btnGO.AddComponent<Image>();
            var btn    = btnGO.AddComponent<Button>();

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(4f,  4f);
            lblRT.offsetMax = new Vector2(-4f, -4f);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = "CRAFT";
            lbl.fontSize  = 13;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = Color.white;

            bool canAfford = recipe.CanAfford();
            Color btnColor = canAfford ? new Color(0.1f, 0.55f, 0.2f) : new Color(0.22f, 0.22f, 0.22f);
            btnImg.color     = btnColor;
            btn.interactable = canAfford;

            var colors = btn.colors;
            colors.normalColor      = btnColor;
            colors.highlightedColor = canAfford ? Color.Lerp(btnColor, Color.white, 0.25f) : btnColor;
            colors.pressedColor     = canAfford ? Color.Lerp(btnColor, Color.black, 0.2f)  : btnColor;
            colors.disabledColor    = new Color(0.22f, 0.22f, 0.22f);
            btn.colors = colors;

            int captured = index;
            btn.onClick.AddListener(() => OnCraft(captured));
        }

        private void OnCraft(int index)
        {
            if (recipes == null || index >= recipes.Length) return;
            var recipe = recipes[index];
            if (recipe == null || !recipe.CanAfford()) return;

            recipe.ConsumeIngredients();
            ProduceResult(recipe);
            Refresh();
        }

        private void ProduceResult(CraftingRecipe recipe)
        {
            switch (recipe.resultType)
            {
                case CraftingRecipe.ResultType.Tool:
                    if (recipe.resultTool != null)
                        QuickSlotInventory.current?.TryAdd(recipe.resultTool, out _);
                    break;

                case CraftingRecipe.ResultType.CollectorMachine:
                    if (recipe.resultMachine != null)
                        SpawnCollector(recipe.resultMachine);
                    break;

                case CraftingRecipe.ResultType.StorageMachine:
                    if (recipe.resultStorageMachine != null)
                        SpawnStorage(recipe.resultStorageMachine);
                    break;
            }
        }

        private void SpawnCollector(MachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null) return;

            Vector3    pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            var go      = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.2f, 0.6f, 1f));
            var machine = go.AddComponent<CollectorMachine>();
            machine.Init(data, planet);
        }

        private void SpawnStorage(StorageMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null) return;

            Vector3    pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            var go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.8f, 0.4f, 0f));
            go.AddComponent<StorageMachine>().Init(data, null);
        }

        private Vector3 FindSurfacePoint(Transform planet)
        {
            Vector3 playerPos = playerController.rig.position;
            Vector3 targetPos = playerPos + playerController.transform.forward * spawnForwardDistance;
            Vector3 snapDir   = (targetPos - planet.position).normalized;
            float   radius    = planet.localScale.x * 0.5f;
            Vector3 origin    = planet.position + snapDir * (radius + 20f);

            if (Physics.Raycast(origin, -snapDir, out RaycastHit hit, radius + 40f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point;

            return planet.position + snapDir * radius;
        }

        private static GameObject SpawnObject(GameObject prefab, Vector3 pos, Quaternion rot,
            string fallbackName, Color fallbackColor)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, rot);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(pos, rot);
                go.GetComponent<Renderer>().material.color = fallbackColor;
            }

            go.transform.localScale = Vector3.one * 300f;
            go.transform.rotation   = rot * Quaternion.Euler(-90f, 0f, 0f);
            go.name = fallbackName;

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();
            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            return go;
        }

        private string BuildIngredientsText(CraftingRecipe recipe)
        {
            if (recipe.ingredients == null || recipe.ingredients.Length == 0)
                return "<color=#aaaaaa>Besplatno</color>";

            var sb = new StringBuilder();
            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null) continue;
                var    inv  = InventorySystem.current?.Get(ing.item);
                int    have = inv?.GetStackSize() ?? 0;
                string col  = have >= ing.amount ? "#88ff88" : "#ff6666";
                sb.AppendLine($"<color={col}>{ing.amount}x {ing.item.displayName} ({have})</color>");
            }
            return sb.ToString().TrimEnd();
        }

        private static string TypeLabel(CraftingRecipe.ResultType type) => type switch
        {
            CraftingRecipe.ResultType.Tool             => "ALAT",
            CraftingRecipe.ResultType.CollectorMachine => "KOLEKTOR",
            CraftingRecipe.ResultType.StorageMachine   => "STORAGE",
            _                                           => ""
        };

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("Crafting_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(560f, 430f);

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            MakeLabel(_panel.transform, "CRAFTING", 20, new Vector2(0f, 196f), new Vector2(500f, 36f))
                .alignment = TextAlignmentOptions.Center;

            // Scroll view
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(_panel.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchoredPosition = new Vector2(0f, -22f);
            scrollRT.sizeDelta        = new Vector2(540f, 342f);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();

            // Content — no VerticalLayoutGroup or ContentSizeFitter; Refresh() sets size manually
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            scrollRect.viewport = viewportRT;
            scrollRect.content  = contentRT;
            _contentRoot = contentGO.transform;
            _contentRT   = contentRT;

            MakeLabel(_panel.transform, "ESC — zatvori", 11, new Vector2(0f, -200f), new Vector2(500f, 24f))
                .color = new Color(0.6f, 0.6f, 0.6f);
        }

        private TextMeshProUGUI MakeLabel(Transform parent, string text, float fontSize, Vector2 pos, Vector2 delta)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = delta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
    }
}
