using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Respawn točka: E na totemu ga aktivira, smrt vraća igrača na zadnji aktivirani
    // totem (GameManager.Respawn). Glavni totem na Hubu spawna GameManager na startu
    // i on je fallback kad se aktivni totem uništi.
    public class RespawnTotem : BaseInteractable
    {
        private static readonly Color TotemColor  = new(0.9f, 0.75f, 0.2f);
        private static readonly Color ActiveColor = new(0.35f, 1f, 0.45f);

        public static RespawnTotem Active   { get; private set; }
        public static RespawnTotem HubTotem { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = null; HubTotem = null; }

        [SerializeField] private RespawnTotemMachineData data;

        private Transform _planet;
        private readonly Dictionary<Renderer, Color> _originalColors = new();

        public Transform Planet => _planet;
        public RespawnTotemMachineData Data => data;

        public override float HoldTime => 0f;

        private string DisplayName => data != null ? data.displayName : "Respawn Totem";

        // Gradi totem na površini planeta; za hub totem data smije biti null (fallback vizual).
        public static RespawnTotem Spawn(RespawnTotemMachineData data, Transform planet, Vector3 pos,
            Quaternion rot, bool isHubTotem = false)
        {
            string name = (data != null ? data.displayName : "Respawn Totem") + (isHubTotem ? " (Hub)" : "");
            GameObject go = MachinePlacer.SpawnObject(data != null ? data.prefab : null, pos, rot,
                name, TotemColor, scale: 5f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true,
                planet: planet);

            var totem = go.AddComponent<RespawnTotem>();
            totem.Init(data, planet, isHubTotem);
            return totem;
        }

        public void Init(RespawnTotemMachineData machineData, Transform planet, bool isHubTotem = false)
        {
            data = machineData;
            _planet = planet;

            if (isHubTotem)
            {
                HubTotem = this;
                if (Active == null) SetActive();
            }
        }

        public override void Interact()
        {
            if (Active == this)
            {
                Debug.Log($"[{DisplayName}] Već je aktivna respawn točka.");
                return;
            }
            SetActive();
        }

        private void SetActive()
        {
            RespawnTotem previous = Active;
            Active = this;
            if (previous != null) previous.RefreshVisual();
            RefreshVisual();
            Debug.Log($"[{DisplayName}] Respawn točka postavljena — smrt te sada vraća ovdje.");
        }

        // Aktivni totem uništen izvana: respawn se vraća na glavni hub totem.
        void OnDestroy()
        {
            if (HubTotem == this) HubTotem = null;
            if (Active == this)
            {
                Active = HubTotem;
                if (Active != null) Active.RefreshVisual();
            }
        }

        private void RefreshVisual()
        {
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                if (!_originalColors.ContainsKey(rend))
                    _originalColors[rend] = rend.material.color;
                rend.material.color = Active == this ? ActiveColor : _originalColors[rend];
            }
        }
    }
}
