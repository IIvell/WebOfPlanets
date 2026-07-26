using UnityEngine;

namespace WebOfPlanets
{
    public class PlayerController : MonoBehaviour
    {
        public Rigidbody rig;
        public Transform currentPlanet;
        public bool freezeRotation = true;

        [SerializeField] private float moveSpeed = 3f;

        [Header("Visual")]
        [Tooltip("Model robota koji se vizualno rotira prema smjeru kretanja (ne rotira Player/collider).")]
        [SerializeField] private Transform visualModel;
        public Transform VisualModel => visualModel;
        [Tooltip("Koliko brzo se model okreće prema smjeru kretanja.")]
        [SerializeField] private float turnSpeed = 10f;

        // Namjerno NOVA imena polja (bez FormerlySerializedAs): scena je imala
        // serijalizirane iceAcceleration=25 / iceDeceleration=0.3 koje bi pregazile
        // code defaulte — s njima je ubrzanje bilo praktički trenutno, a zaustavljanje
        // sa 7 m/s trajalo ~23 s ("beskonačno" klizanje). Ne vraćati stara imena.
        [Tooltip("Sekunde do pune brzine na ledu (veće = tromije ubrzanje).")]
        [SerializeField] private float iceAccelTime = 1f;
        [Tooltip("Sekunde od pune brzine do zaustavljanja na ledu bez inputa (veće = duže klizanje).")]
        [SerializeField] private float iceStopTime = 1.5f;

        [Header("Surface lock")]
        [Tooltip("Dopuštena visina dna kapsule iznad površine planeta. Mora pokriti nagibe i rubove faceta mesh planeta (~0.1), ali ostati ispod visine resursa.")]
        [SerializeField] private float surfaceSkin = 0.15f;
        [Tooltip("Najveći povrat visine po fizičkom koraku — omeđuje trzaj ako se stanje ikad zatekne daleko iznad tla.")]
        [SerializeField] private float maxSnapPerStep = 0.2f;
        [Tooltip("Visina iznad koje se lock otpušta i igrač normalno pada (rub litice, krov nakon teleporta). Penjanjem nedostižno: lock višak reže već iznad skina.")]
        [SerializeField] private float ungroundHeight = 0.5f;

        private Planet _planet;
        private float _defaultLinearDamping;
        private Vector3 _faceDirection;
        private CapsuleCollider _capsule;
        private bool _grounded;
        private Vector3 _lastStepPos;

        PlayerInputActions _input;

        void Awake()
        {
            _input = new PlayerInputActions();
        }

        void OnEnable()  => _input?.Enable();
        void OnDisable() => _input?.Disable();

        public void SetInputEnabled(bool enabled)
        {
            if (enabled) _input.Enable();
            else _input.Disable();
        }

        void Start()
        {
            Ini();
            AlignColliderWithVisual();
            if (currentPlanet != null)
                _planet = currentPlanet.GetComponent<Planet>();

            // Nakon AlignColliderWithVisual — surface lock mjeri dno kapsule iz
            // stvarnog (runtime poravnatog) centra, ne iz pretpostavke o pivotu.
            TryGetComponent(out _capsule);
            _lastStepPos = rig.position;
        }

        // Vizualni robot rotira oko visualModel pivota (FaceDirection), a kapsula je
        // fiksna na Playeru. U sceni je mesh robota stajao pomaknut od pivota
        // (~1.3 po z), pa je pri okretanju KRUŽIO oko pivota umjesto da se vrti u
        // mjestu — vizual i fizika su se razilazili do ~2.5 jedinice ovisno o smjeru
        // gledanja (igrač "u totemu" s jedne strane, blokiran zrakom s druge).
        // Umjesto ručnog štimanja scene: izmjeri stvarnu geometriju robota, centriraj
        // je na pivot (sva djeca pivota zajedno, da alat u ruci zadrži odnos prema
        // mešu) i postavi kapsulu točno na pivot. Visina (y) se ne dira — visina
        // stajanja je namještena u sceni i nije bila problem.
        private void AlignColliderWithVisual()
        {
            if (visualModel == null) return;

            // Lokalni AABB stvarne geometrije (vrhovi/bakani skinned vrhovi) u
            // prostoru pivota; pri Startu alat još nije opremljen pa mjeri samo robota.
            if (SurfacePlacement.TryGetLocalBounds(visualModel.gameObject, out Bounds local))
            {
                Vector3 delta = new Vector3(local.center.x, 0f, local.center.z);
                if (delta.sqrMagnitude > 0.01f)
                {
                    foreach (Transform child in visualModel)
                        child.localPosition -= delta;
                    Debug.Log($"PlayerController: vizual robota centriran na pivot (pomak {delta}).");
                }
            }

            if (TryGetComponent(out CapsuleCollider capsule))
            {
                Vector3 pivotLocal = transform.InverseTransformPoint(visualModel.position);
                capsule.center = new Vector3(pivotLocal.x, capsule.center.y, pivotLocal.z);
            }
        }

        public void SetPlanet(Transform planet)
        {
            currentPlanet = planet;
            _planet = planet != null ? planet.GetComponent<Planet>() : null;
            // Novi planet = nova površina; tlo se ponovno hvata iz zraka (meki pad).
            _grounded = false;
        }

        void FixedUpdate()
        {
            ApplyGravity();
            Move();
            EnforceSurfaceLock();
        }

        void Update()
        {
            // Vizual se okreće u Updateu (render framerate), ne u FixedUpdateu (50 Hz),
            // inače rotacija modela štuca na monitorima s višim refreshom.
            FaceDirection(_faceDirection);
        }

        private void ApplyGravity()
        {
            if (_planet == null) return;
            Vector3 down = (currentPlanet.position - transform.position).normalized;
            rig.AddForce(down * _planet.Gravity, ForceMode.Acceleration);

            // Fizički linearDamping je isključen (gušio bi i horizontalno kretanje koje
            // sad ide kroz linearVelocity), pa se stari meki pad reproducira ručno samo
            // na vertikalnoj komponenti — ista formula kojom PhysX primjenjuje damping.
            float d = _defaultLinearDamping * Time.fixedDeltaTime;
            Vector3 vertical = Vector3.Project(rig.linearVelocity, down);
            rig.linearVelocity -= vertical * (d / (1f + d));
        }

        private void Ini()
        {
            rig.useGravity = false;
            rig.interpolation = RigidbodyInterpolation.Interpolate;
            _defaultLinearDamping = rig.linearDamping;
            if (freezeRotation)
                rig.constraints = RigidbodyConstraints.FreezeRotation;
            else
                rig.constraints = RigidbodyConstraints.None;

            // Kretanje smiju oblikovati samo naši accel/decel parametri. PhysX trenje
            // na kontaktu kapsula-planet inače troši naslijeđenu brzinu na ledu,
            // najjače na rubovima faceta mesh collidera (planet je poligonalna kugla),
            // pa je klizanje bilo mjestimično sporije. Običan hod ne ovisi o ovome
            // (brzina se ionako prepisuje svaki tick), a stajanje čuva kod, ne trenje.
            if (TryGetComponent(out CapsuleCollider capsule))
                capsule.material = new PhysicsMaterial("PlayerFrictionless")
                {
                    staticFriction = 0f,
                    dynamicFriction = 0f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
        }

        private void Move()
        {
            Vector2 input = _input.PlayerActionmap.Movement.ReadValue<Vector2>();
            bool onIce = _planet != null && _planet.Type == PlanetType.Ice;

            // Kretanje uvijek ide kroz linearVelocity — MovePosition je na dinamičkom
            // rigidbodyju teleport koji svaki fizički korak resetira interpolaciju i
            // radi trzaje. Damping je zato uvijek 0: horizontalu postavljamo sami svaki
            // korak, a meki pad na vertikali simulira ApplyGravity.
            rig.linearDamping = 0f;

            if (onIce)
            {
                MoveOnIce(input);
                return;
            }

            Vector3 verticalVelocity = Vector3.Project(rig.linearVelocity, transform.up);
            Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
            rig.linearVelocity = moveDir * moveSpeed + verticalVelocity;

            _faceDirection = moveDir;
        }

        private void MoveOnIce(Vector2 input)
        {
            // Umjesto direktnog namještanja pozicije, brzina teži cilju s ograničenim ubrzanjem
            // pa igrač klizi po ledu - ne staje odmah i ne skreće trenutno.
            // Up je radijala od centra planeta, ne transform.up — poravnanje tijela
            // (Attractor slerp) kasni za stvarnom normalom pa bi split krivo režao.
            Vector3 up = currentPlanet != null
                ? (transform.position - currentPlanet.position).normalized
                : transform.up;

            Vector3 verticalVelocity = Vector3.Project(rig.linearVelocity, up);
            Vector3 horizontalVelocity = rig.linearVelocity - verticalVelocity;

            Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;

            // Rate izveden iz moveSpeed: promjena brzine hoda u inspectoru čuva isti
            // osjećaj leda (isto vrijeme zaleta i zaustavljanja). Konstantna
            // deceleracija = kinetičko trenje, pa MoveTowards garantira potpuni stop.
            bool hasInput = input.sqrMagnitude > 0.0001f;
            // Donji prag rate-a: i uz moveSpeed 0 (lock kretanja u inspectoru)
            // naslijeđena brzina mora otkliziti u stop, ne ostati zamrznuta.
            float rate = Mathf.Max(0.5f, moveSpeed / Mathf.Max(0.05f, hasInput ? iceAccelTime : iceStopTime));

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.fixedDeltaTime);
            rig.linearVelocity = horizontalVelocity + verticalVelocity;

            _faceDirection = horizontalVelocity;
        }

        // Igrač mora uvijek ostati pri površini planeta: guranjem u box collider
        // resursa/stroja/totema PhysX depenetracija kapsulu vuče preko ruba prema
        // gore, a Move čuva vertikalnu brzinu, pa se bez ovoga dalo popeti na
        // objekte. Visina se mjeri raycastom koji prihvaća SAMO planetov collider
        // (objekti na površini nisu tlo); dok je igrač prizemljen, dno kapsule
        // iznad surfaceSkin gubi outward brzinu i višak visine.
        //
        // Gating po _grounded čuva legitimne letove: teleport/respawn/load spuštaju
        // igrača ~1 m iznad tla uz namjerni meki pad (ApplyGravity), a load zna
        // vratiti i pozu usred pada — svaki skok pozicije veći nego što ijedno
        // legitimno kretanje napravi u jednom koraku ruši _grounded pa se tlo
        // ponovno hvata iz zraka. Slijetanje na krov stroja (teleport raycasta sve
        // collidere) ostaje negrounded — igrač normalno siđe pa se lock uhvati.
        private void EnforceSurfaceLock()
        {
            if (_planet == null || _capsule == null) return;

            Vector3 pos = rig.position;
            if ((pos - _lastStepPos).sqrMagnitude > 1f) _grounded = false;
            _lastStepPos = pos;

            // Zraka uzorkuje tlo POD KAPSULOM, ne pod pivotom: capsule.center je
            // nakon AlignColliderWithVisual ~0.8 bočno od rig.position, pa bi stup
            // ispod pivota na nagibu ili rubu facete mjerio tuđu visinu (greška
            // ±offset·tanθ — lažni prekršaji ili propušteno penjanje, ovisno o
            // yawu tijela koji se s kretanjem ne rotira).
            Vector3 rayOrigin = pos + transform.rotation * new Vector3(_capsule.center.x, 0f, _capsule.center.z);
            Vector3 up = (rayOrigin - currentPlanet.position).normalized;

            // Promašaj (stale PhysX poza planeta i sl.): bez mete nema ni clampa —
            // analitički fallback je na mesh planetima do ~1.3% R kriv i lagao bi.
            if (!SurfacePlacement.TryRaycastSurface(currentPlanet, rayOrigin, -up, 30f, out RaycastHit hit))
                return;

            // Dno kapsule iz rig poze + rotacije: transform.position uz interpolaciju
            // i isključeni autoSyncTransforms ne mora odgovarati fizičkoj pozi.
            Vector3 feet = pos + transform.rotation * (_capsule.center + Vector3.down * (_capsule.height * 0.5f));
            float altitude = Vector3.Dot(feet - hit.point, up);

            if (!_grounded)
            {
                _grounded = altitude <= surfaceSkin;
                return;
            }

            if (altitude > ungroundHeight)
            {
                _grounded = false;
                return;
            }

            if (altitude <= surfaceSkin) return;

            // Aktivno penjanje: odrezati outward komponentu brzine (po stvarnoj
            // radijali, ne transform.up koji kasni za Attractor slerpom) i vratiti
            // višak visine. Direktno pisanje rig.position resetira interpolaciju,
            // ali samo u koracima u kojima je prekršaj stvarno nastao — običan hod
            // ostaje ispod skina i ne dira poziciju. Blagi pritisak u rub collidera
            // koji time nastane PhysX razrješava bočno, pa igrač sklizne s objekta.
            float outward = Vector3.Dot(rig.linearVelocity, up);
            if (outward > 0f) rig.linearVelocity -= up * outward;

            rig.position = pos - up * Mathf.Min(altitude - surfaceSkin, maxSnapPerStep);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (visualModel == null || direction.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(-direction.normalized, transform.up);
            visualModel.rotation = Quaternion.Slerp(visualModel.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
