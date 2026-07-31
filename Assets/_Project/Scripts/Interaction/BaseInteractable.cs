using System.Collections;
using UnityEngine;

namespace WebOfPlanets
{
    // Sučelje + baza + runtime-only podrazredi u jednoj datoteci (čišćenje
    // malih datoteka, srpanj 2026.). Podrazredi koje scena/prefabi referenciraju
    // po GUID-u (Tool/NetworkComputer/HubStorage) MORAJU ostati u svojim
    // datotekama — Unity razriješi serijaliziranu referencu samo ako se razred
    // zove kao datoteka.
    public interface IInteractable
    {
        float HoldTime { get; }
        // Smije li interakcija krenuti (npr. ItemInteractable traži odgovarajući
        // alat) — u sučelju da Interactor ne mora downcastati na BaseInteractable.
        bool CanInteract { get; }
        void Interact();
    }

    public abstract class BaseInteractable : MonoBehaviour, IInteractable
    {
        public virtual float HoldTime => 0f;
        public virtual bool CanInteract => true;
        public abstract void Interact();
    }

    // Aktivna veza: interakcija teleportira igrača na drugi kraj. Dodaje se
    // runtime (PlanetConnection), nikad serijalizirano u sceni.
    public class ConnectionInteractable : BaseInteractable
    {
        private PlanetCreator _planetCreator;
        private Transform _sourcePlanet;
        private Transform _targetPlanet;
        private Transform _destinationMarker;

        public Transform SourcePlanet => _sourcePlanet;
        public Transform TargetPlanet => _targetPlanet;

        public void Init(PlanetCreator planetCreator, Transform sourcePlanet, Transform targetPlanet)
        {
            _planetCreator = planetCreator;
            _sourcePlanet = sourcePlanet;
            _targetPlanet = targetPlanet;
        }

        public void SetDestinationMarker(Transform destinationMarker)
        {
            _destinationMarker = destinationMarker;
        }

        public override void Interact()
        {
            _planetCreator.TeleportToPlanet(_targetPlanet, _sourcePlanet, _destinationMarker);
        }
    }

    // Marker potencijalne veze: interakcija otvara izbor tipa veze. Dodaje se
    // runtime (ConnectionManager), nikad serijalizirano u sceni.
    public class PotentialConnectionInteractable : BaseInteractable
    {
        private ConnectionManager _connectionManager;
        private Transform _sourcePlanet;
        private Transform _targetPlanet;

        public void Init(ConnectionManager connectionManager, Transform source, Transform target)
        {
            _connectionManager = connectionManager;
            _sourcePlanet = source;
            _targetPlanet = target;
        }

        public override void Interact()
        {
            ConnectionChoiceUI.Instance?.Show(_connectionManager, _sourcePlanet, _targetPlanet);
        }
    }

    // Resurs u svijetu (pickup ili mining). Dodaje se runtime pri spawnu resursa;
    // konsolidirano iz ItemInteractable.cs (srpanj 2026.).
    public class ItemInteractable : BaseInteractable
    {
        [SerializeField] private Item referenceItem;
        [SerializeField] private bool isPickup;
        [SerializeField] private bool destroyAfterPickup = true;

        private bool _regenerating;

        public override float HoldTime => !isPickup && referenceItem != null ? referenceItem.miningTime : 0f;
        public Item ReferenceItem => referenceItem;
        public bool IsPickup => isPickup;

        public override bool CanInteract
        {
            get
            {
                if (_regenerating) return false;
                if (isPickup) return true;
                if (referenceItem == null || referenceItem.requiredTool == null) return true;
                if (PlayerToolSystem.Instance == null) return false;
                var equipped = PlayerToolSystem.Instance.EquippedTool;
                if (equipped == null) return false;
                // Isti alat ili alat iste klase dovoljnog ranga
                return equipped == referenceItem.requiredTool ||
                       (equipped.toolClass == referenceItem.requiredTool.toolClass &&
                        equipped.miningTier >= referenceItem.requiredTool.miningTier);
            }
        }

        public void Init(Item item, bool pickup = false, bool destroy = true)
        {
            referenceItem = item;
            isPickup = pickup;
            destroyAfterPickup = destroy;
        }

        public override void Interact()
        {
            if (referenceItem == null)
            {
                Debug.LogWarning($"{name}: nema dodjeljenog Item asseta.");
                return;
            }

            int yieldCount = isPickup ? 1 : Random.Range(referenceItem.minMiningYield, referenceItem.maxMiningYield + 1);
            for (int i = 0; i < yieldCount; i++)
                InventorySystem.Instance.Add(referenceItem);

            if (!isPickup && referenceItem.bonusMiningItem != null && Random.value < referenceItem.bonusMiningChance)
                InventorySystem.Instance.Add(referenceItem.bonusMiningItem);

            PlayerToolSystem.Instance?.OnResourceMined();
            Debug.Log($"Picked up: {referenceItem.displayName} x{yieldCount}");

            if (referenceItem.regenerationTime > 0f)
                StartCoroutine(RegenerateAfter(referenceItem.regenerationTime));
            else if (destroyAfterPickup)
                // Raspadanje s dimom umjesto trenutnog nestanka (dorada vizuala,
                // srpanj 2026.) — Play() odmah gasi collider i ovu skriptu.
                DisintegrationEffect.Play(gameObject);
        }

        // Koristi stroj umjesto igrača — preskače tool provjeru, ne dodaje u player inventory
        public bool TryCollectByMachine(out Item collected)
        {
            collected = null;
            if (_regenerating || referenceItem == null) return false;

            collected = referenceItem;

            if (referenceItem.regenerationTime > 0f)
                StartCoroutine(RegenerateAfter(referenceItem.regenerationTime));
            else if (destroyAfterPickup)
                DisintegrationEffect.Play(gameObject);

            return true;
        }

        // Trajanje skupljanja/rasta na rubovima regeneracije. Scale se NE sprema
        // u ResourceSave, pa animacija ne može zagaditi save; _regenerating se
        // diže PRIJE animacije da CanInteract/TryCollectByMachine odmah odbiju.
        private const float RegenAnimTime = 0.35f;

        private IEnumerator RegenerateAfter(float seconds)
        {
            _regenerating = true;

            // Skupljanje + dim umjesto trenutnog nestanka; original scale se
            // vraća prije čekanja jer SetVisible(false) ionako sve skriva.
            Vector3 originalScale = transform.localScale;
            VfxManager.PlayDisintegrate(transform.position, transform.up, 0.8f);
            for (float t = 0f; t < RegenAnimTime; t += Time.deltaTime)
            {
                transform.localScale = originalScale * (1f - t / RegenAnimTime);
                yield return null;
            }
            SetVisible(false);
            transform.localScale = originalScale;

            yield return new WaitForSeconds(seconds);

            // Rast natrag umjesto trenutnog pojavljivanja.
            SetVisible(true);
            for (float t = 0f; t < RegenAnimTime; t += Time.deltaTime)
            {
                transform.localScale = originalScale * (t / RegenAnimTime);
                yield return null;
            }
            transform.localScale = originalScale;
            _regenerating = false;
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = visible;
        }
    }
}
