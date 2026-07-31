using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WebOfPlanets
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Transform interactorSource;
        [SerializeField] private float interactRange = 3f;

        public Transform InteractorSource => interactorSource;
        public float InteractRange => interactRange;

        private IInteractable _currentTarget;
        private float _holdTimer;

        void Start()
        {
            if (interactorSource == null)
                interactorSource = transform;
        }

        void Update()
        {
            if (Keyboard.current == null) return;
            if (!GameManager.IsPlaying) return;

            if (GameKeys.WasPressed(GameKeys.Interact))
            {
                _currentTarget = FindClosest();
                _holdTimer = 0f;

                if (_currentTarget != null && !_currentTarget.CanInteract)
                {
                    Debug.Log("Potreban je specifičan alat za minanje ovog resursa.");
                    _currentTarget = null;
                    return;
                }

                if (_currentTarget != null && _currentTarget.HoldTime <= 0f)
                {
                    _currentTarget.Interact();
                    _currentTarget = null;
                    return;
                }
            }

            if (_currentTarget == null) return;

            if (!GameKeys.IsPressed(GameKeys.Interact))
            {
                CancelMining();
                return;
            }

            if (_currentTarget is MonoBehaviour mb)
            {
                Vector3 closestPoint = mb.TryGetComponent<Collider>(out var col)
                    ? col.ClosestPoint(interactorSource.position)
                    : mb.transform.position;

                if (Vector3.Distance(interactorSource.position, closestPoint) > interactRange)
                {
                    CancelMining();
                    return;
                }

                // Iskrice na točki kopanja, prema igraču (VfxManager throttla ritam).
                VfxManager.PlayMiningSparks(closestPoint, interactorSource.position - closestPoint);
            }

            _holdTimer += Time.deltaTime * PlayerToolSystem.GetSpeedMultiplier();
            float progress = Mathf.Clamp01(_holdTimer / _currentTarget.HoldTime);
            GameEventBus.Raise(new MiningProgressEvent { Progress = progress, IsMining = true });

            if (_holdTimer >= _currentTarget.HoldTime)
            {
                _currentTarget.Interact();
                _currentTarget = null;
                _holdTimer = 0f;
                GameEventBus.Raise(new MiningProgressEvent { Progress = 0f, IsMining = false });
            }
        }

        // Za InteractableHighlight: isti sken kao pri pritisku tipke interakcije,
        // bez nuspojava — highlight i stvarna interakcija tako uvijek ciljaju
        // ISTI objekt (dvije kopije logike bi se s vremenom razišle, ista klasa
        // problema kao nekadašnje duplicirane tablice u MachineFactoryju).
        internal IInteractable PeekClosest() => FindClosest();

        private IInteractable FindClosest()
        {
            Collider[] nearby = Physics.OverlapSphere(
                interactorSource.position, 
                interactRange, 
                Physics.DefaultRaycastLayers, 
                QueryTriggerInteraction.Collide);
                
            IInteractable closest = null;
            float closestDist = Mathf.Infinity;

            foreach (var col in nearby)
            {
                if (col.TryGetComponent(out IInteractable interactable))
                {
                    Vector3 closestPoint = col.ClosestPoint(interactorSource.position);
                    float dist = Vector3.Distance(interactorSource.position, closestPoint);
                    if (dist >= closestDist) continue;
                    if (!HasLineOfSight(col, closestPoint)) continue;

                    closestDist = dist;
                    closest = interactable;
                }
            }
            return closest;
        }

        // Bez ovoga OverlapSphere dopušta minanje/interakciju kroz prepreke
        // (kamenje, strojeve, pa i "iza horizonta" planeta).
        private bool HasLineOfSight(Collider target, Vector3 targetPoint)
        {
            Vector3 origin = interactorSource.position;
            Vector3 toTarget = targetPoint - origin;
            float dist = toTarget.magnitude;
            if (dist < 0.05f) return true;

            Vector3 dir = toTarget / dist;
            foreach (var hit in Physics.RaycastAll(origin, dir, dist - 0.02f,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == target) continue;
                if (hit.collider.transform.IsChildOf(target.transform)) continue; // dijelovi istog objekta
                if (hit.collider.transform.IsChildOf(transform.root)) continue;   // vlastiti collider igrača
                return false;
            }
            return true;
        }

        private void CancelMining()
        {
            GameEventBus.Raise(new MiningProgressEvent { Progress = 0f, IsMining = false });
            _currentTarget = null;
            _holdTimer = 0f;
        }
    }

    // Premješteno iz InteractableHighlight.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Suptilni puls svjetline na najbližem interaktabilnom objektu u dosegu
    // (dorada vizuala, srpanj 2026.: igrač do sada nije imao signal "s ovim mogu
    // interaktirati" dok ne pritisne tipku). MaterialPropertyBlock umjesto
    // Renderer.material: ne instancira materijale (nema leaka ni razbijanja
    // batchinga), a čisti se jednim SetPropertyBlock(null). Samopokretajući
    // Bootstrap obrazac kao VfxManager/AudioManager — bez izmjena scene.
    public class InteractableHighlight : MonoBehaviour
    {
        private const float ScanInterval = 0.12f;  // sken ~8x/s je dovoljan; puls je per-frame
        private const float PulseSpeed = 5f;       // rad/s sinusa pulsa
        private const float PulseStrength = 0.35f; // maksimalno pojačanje svjetline (subtilno)

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // legacy/Sprites

        public static InteractableHighlight Instance { get; private set; }

        private Interactor _interactor;
        private MonoBehaviour _target;
        private float _nextScan;
        private MaterialPropertyBlock _mpb;

        // (renderer, indeks materijala, originalna boja) — original se čita iz
        // sharedMaterial JEDNOM pri odabiru mete; puls ga samo množi.
        private readonly List<(Renderer renderer, int index, Color baseColor)> _entries = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("InteractableHighlight").AddComponent<InteractableHighlight>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _mpb = new MaterialPropertyBlock();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnDisable() => SetTarget(null);

        void Update()
        {
            if (!GameManager.IsPlaying)
            {
                if (_target != null || _entries.Count > 0) SetTarget(null);
                return;
            }

            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + ScanInterval;
                Scan();
            }

            ApplyPulse();
        }

        private void Scan()
        {
            if (_interactor == null)
            {
                _interactor = FindFirstObjectByType<Interactor>();
                if (_interactor == null) return;
            }

            IInteractable closest = _interactor.PeekClosest();
            var mb = closest as MonoBehaviour;

            // Bez signala kad interakcija trenutno nije moguća (krivi alat,
            // resurs u regeneraciji) — highlight bi lagao.
            if (mb != null && !closest.CanInteract) mb = null;

            if (mb != _target) SetTarget(mb);
        }

        private void SetTarget(MonoBehaviour target)
        {
            // Očisti staru metu; renderer u međuvremenu može biti uništen
            // (izrudareni resurs), zato null provjera po unosu.
            foreach (var e in _entries)
                if (e.renderer != null)
                    e.renderer.SetPropertyBlock(null, e.index);
            _entries.Clear();

            _target = target;
            if (target == null) return;

            foreach (var r in target.GetComponentsInChildren<Renderer>())
            {
                // Samo mesh vizuali — čestice i sl. preskačemo.
                if (r is not MeshRenderer and not SkinnedMeshRenderer) continue;

                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    Color c = mats[i].HasProperty(BaseColorId) ? mats[i].GetColor(BaseColorId)
                        : mats[i].HasProperty(ColorId) ? mats[i].GetColor(ColorId)
                        : Color.white;
                    _entries.Add((r, i, c));
                }
            }
        }

        private void ApplyPulse()
        {
            if (_target == null)
            {
                if (_entries.Count > 0) SetTarget(null); // meta uništena između skenova
                return;
            }

            float pulse = 1f + PulseStrength * (0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed));
            foreach (var e in _entries)
            {
                if (e.renderer == null) continue;

                // Množenje originala pulsom čuva ton materijala; alpha ostaje
                // netaknuta da transparentni materijali ne probljeskuju.
                Color c = e.baseColor * pulse;
                c.a = e.baseColor.a;

                _mpb.Clear();
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(ColorId, c);
                e.renderer.SetPropertyBlock(_mpb, e.index);
            }
        }
    }
}
