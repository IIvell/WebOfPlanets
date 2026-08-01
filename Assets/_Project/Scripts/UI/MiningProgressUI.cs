using UnityEngine;
using UnityEngine.UI;

namespace WebOfPlanets
{
    public class MiningProgressUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Image fillImage;
        [SerializeField] private float smoothSpeed = 10f;

        private float _targetProgress;

        void OnEnable()
        {
            GameEventBus.OnMiningProgress += OnMiningProgress;
        }

        void OnDisable()
        {
            GameEventBus.OnMiningProgress -= OnMiningProgress;
        }

        void Start()
        {
            // Vizualna tema (itch.io pack) — null-safe, bez sprite-ova ostaje
            // izgled iz scene. Jedini UI kojem su Image reference u sceni.
            UiTheme.StyleBarFrame(panel.GetComponent<Image>());
            UiTheme.StyleBarFill(fillImage);

            panel.SetActive(false);
            fillImage.fillAmount = 0f;
        }

        void Update()
        {
            if (!panel.activeSelf) return;

            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, _targetProgress, Time.deltaTime * smoothSpeed);
        }

        private void OnMiningProgress(MiningProgressEvent e)
        {
            if (e.IsMining)
            {
                panel.SetActive(true);
                _targetProgress = e.Progress;
            }
            else
            {
                panel.SetActive(false);
                fillImage.fillAmount = 0f;
                _targetProgress = 0f;
            }
        }
    }
}
