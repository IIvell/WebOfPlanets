using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    public class MachineTeleporterUI : MonoBehaviour
    {
        public static MachineTeleporterUI Instance { get; private set; }

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Interactor interactor;

        private GameObject _panel;
        private Transform _buttonContainer;
        private TextMeshProUGUI _titleText;

        private Action<Transform> _onPlanetPicked;
        private Action _onCancelled;

        void Awake()
        {
            Instance = this;
            BuildUI();
            _panel.SetActive(false);
        }

        void Update()
        {
            if (_panel.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cancel();
        }

        public void Show(string title, List<Transform> planets,
            Action<Transform> onPicked, Action onCancelled = null)
        {
            _onPlanetPicked = onPicked;
            _onCancelled    = onCancelled;
            _titleText.text = title;

            foreach (Transform child in _buttonContainer)
                Destroy(child.gameObject);

            foreach (var planet in planets)
            {
                Transform captured = planet;
                CreateButton(planet.name, () =>
                {
                    Hide();
                    _onPlanetPicked?.Invoke(captured);
                });
            }

            _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            playerController?.SetInputEnabled(false);
            playerCamera?.SetInputEnabled(false);
            if (interactor != null) interactor.enabled = false;
        }

        private void Cancel()
        {
            Hide();
            _onCancelled?.Invoke();
        }

        private void Hide()
        {
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            playerController?.SetInputEnabled(true);
            playerCamera?.SetInputEnabled(true);
            if (interactor != null) interactor.enabled = true;
        }

        private void CreateButton(string label, Action onClick)
        {
            var go = new GameObject("PlanetBtn");
            go.transform.SetParent(_buttonContainer, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 50f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.35f, 0.6f);

            var btn = go.AddComponent<Button>();
            var block = btn.colors;
            block.normalColor      = new Color(0.1f, 0.35f, 0.6f);
            block.highlightedColor = new Vector4(0.2f, 0.55f, 0.85f, 1f);
            block.pressedColor     = new Color(0.05f, 0.2f, 0.4f);
            block.selectedColor    = block.normalColor;
            btn.colors = block;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8f, 4f);
            txtRT.offsetMax = new Vector2(-8f, -4f);

            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 15;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
        }

        private void BuildUI()
        {
            _panel = new GameObject("MachineTeleporter_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta        = new Vector2(400f, 400f);

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_panel.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -12f);
            titleRT.sizeDelta        = new Vector2(0f, 36f);

            _titleText           = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.fontSize  = 18;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color     = Color.white;

            // Button container — simple vertical list, no ScrollRect
            var container = new GameObject("ButtonContainer");
            container.transform.SetParent(_panel.transform, false);
            var containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin        = new Vector2(0f, 0f);
            containerRT.anchorMax        = new Vector2(1f, 1f);
            containerRT.offsetMin        = new Vector2(24f, 50f);
            containerRT.offsetMax        = new Vector2(-24f, -60f);

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing               = 10f;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;
            layout.padding               = new RectOffset(0, 0, 4, 4);

            _buttonContainer = container.transform;

            // Hint
            var hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(_panel.transform, false);
            var hintRT = hintGO.AddComponent<RectTransform>();
            hintRT.anchorMin        = new Vector2(0f, 0f);
            hintRT.anchorMax        = new Vector2(1f, 0f);
            hintRT.pivot            = new Vector2(0.5f, 0f);
            hintRT.anchoredPosition = new Vector2(0f, 12f);
            hintRT.sizeDelta        = new Vector2(0f, 28f);

            var hint           = hintGO.AddComponent<TextMeshProUGUI>();
            hint.text          = "ESC — odustani (stroj neće biti izgrađen)";
            hint.fontSize      = 11;
            hint.alignment     = TextAlignmentOptions.Center;
            hint.color         = new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
