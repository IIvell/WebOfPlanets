using UnityEngine;
using UnityEngine.UI;

namespace xyz.germanfica.unity.planet.gravity
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
