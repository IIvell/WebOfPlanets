using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlanetConnection : MonoBehaviour
    {
        public Transform PlanetA { get; private set; }
        public Transform PlanetB { get; private set; }
        public float Health { get; private set; } = 100f;
        public ConnectionType Type { get; private set; }

        private GameObject _cylinder;
        private Material _material;
        private float _lifespan;
        private GameObject _markerPrefab;
        private float _markerScale;
        private float _markerHeight;
        private float _thickness;
        private GameObject _markerA;
        private GameObject _markerB;

        private static readonly Color HealthColorHigh = Color.green;
        private static readonly Color HealthColorMid = Color.yellow;
        private static readonly Color HealthColorLow = new Color(1f, 0.5f, 0f);
        private static readonly Color HealthColorCritical = Color.red;
        private const float FlickerThreshold = 20f;

        public void Init(Transform a, Transform b, ConnectionType type, PlanetCreator planetCreator, float lifespan = 0f, GameObject markerPrefab = null, float markerScale = 3f, float markerHeight = 3f, float thickness = 0.6f, Pose? markerPoseA = null, Pose? markerPoseB = null)
        {
            PlanetA = a;
            PlanetB = b;
            Type = type;
            _lifespan = lifespan;
            _markerPrefab = markerPrefab;
            _markerScale = markerScale;
            _markerHeight = markerHeight;
            _thickness = thickness;

            _cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _cylinder.transform.SetParent(transform);
            // Destroy je odgođen do kraja framea, a SpawnMarker dolje odmah radi
            // OverlapSphere (FindClearSurfacePoint) — collider zrake se zato gasi
            // odmah da živi-mrtvi capsule ne otjera marker s idealne točke.
            Collider beamCollider = _cylinder.GetComponent<Collider>();
            beamCollider.enabled = false;
            Destroy(beamCollider);
            _material = _cylinder.GetComponent<Renderer>().material;

            _markerA = SpawnMarker(from: a, toward: b, planetCreator, markerPoseA);
            _markerB = SpawnMarker(from: b, toward: a, planetCreator, markerPoseB);

            if (_markerA != null && _markerB != null)
            {
                _markerA.GetComponent<ConnectionInteractable>().SetDestinationMarker(_markerB.transform);
                _markerB.GetComponent<ConnectionInteractable>().SetDestinationMarker(_markerA.transform);
            }

            // Nakon spawna totema: zraka se sidri na njihove stvarne pozicije.
            UpdateHealthColor();
            UpdateVisual();
        }

        void Update()
        {
            UpdateHealthColor();

            if (_lifespan <= 0f) return;
            float damagePerSecond = 100f / _lifespan;
            ApplyDamage(damagePerSecond * Time.deltaTime);
        }

        private void UpdateHealthColor()
        {
            Color color = HealthToColor(Health);

            if (Health < FlickerThreshold)
            {
                float flickerSpeed = Mathf.Lerp(4f, 12f, 1f - Health / FlickerThreshold);
                float flicker = (Mathf.Sin(Time.time * flickerSpeed) + 1f) * 0.5f;
                color *= Mathf.Lerp(0.35f, 1f, flicker);
            }

            _material.color = color;
            _material.SetColor("_BaseColor", color);
        }

        private static Color HealthToColor(float health)
        {
            float t = Mathf.Clamp01(health / 100f);

            if (t > 2f / 3f)
                return Color.Lerp(HealthColorMid, HealthColorHigh, (t - 2f / 3f) * 3f);
            if (t > 1f / 3f)
                return Color.Lerp(HealthColorLow, HealthColorMid, (t - 1f / 3f) * 3f);

            return Color.Lerp(HealthColorCritical, HealthColorLow, t * 3f);
        }

        private GameObject SpawnMarker(Transform from, Transform toward, PlanetCreator planetCreator, Pose? pose)
        {
            if (_markerPrefab == null)
            {
                Debug.LogError("PlanetConnection: markerPrefab nije postavljen.");
                return null;
            }

            Vector3 pos;
            Quaternion rot;
            if (pose.HasValue)
            {
                // Točna poza potencijalnog totema (isti prefab i scale): igrač je
                // vezu aktivirao baš na njemu, pa totem prave veze mora osvanuti na
                // istom mjestu. Ponovni izračun bi ga sa zauzete idealne točke
                // (resurs) otjerao NOVIM nasumičnim bočnim pomakom na drugo mjesto —
                // igraču bi veza "nestala".
                pos = pose.Value.position;
                rot = pose.Value.rotation;
            }
            else
            {
                // Bez potencijalnog totema (npr. strana preskočena zbog exclusion
                // zone): idealna točka prema drugoj planeti, uz bijeg od zauzetog
                // tla. Radijalno uspravno kao strojevi — normala pogođenog trokuta
                // na low-poly planetima zna vidljivo odstupati od radijalnog "gore"
                // pa bi totem izgledao iskrivljeno; nagnute trokute rješava
                // izmjereni ukop u GroundToSurface, a radijalni up služi i
                // teleport dolasku (destinationMarker.up).
                Vector3 dir = (toward.position - from.position).normalized;
                pos = FindClearSurfacePoint(from, dir);
                Vector3 radial = (pos - from.position).normalized;
                rot = Quaternion.FromToRotation(Vector3.up, radial);
            }

            GameObject marker = Instantiate(_markerPrefab, pos, rot, transform);
            marker.name = "ConnectionMarker";
            marker.transform.localScale = Vector3.one * _markerScale;

            // Prizemlji po stvarnoj geometriji da dno sjedne na površinu bez obzira
            // na pivot prefaba. Preuzeta poza je VEĆ prizemljena (identičan prefab,
            // scale i rotacija) — ponovni GroundToSurface bi s pivotom umjesto
            // surface pointa kao referencom totem podigao/ukopao krivo.
            if (!pose.HasValue)
                SurfacePlacement.GroundToSurface(marker, from, pos, rot * Vector3.up);

            // Solid collider po stvarnim granicama vizuala (isto kao strojevi):
            // totem mora fizički blokirati igrača. Interakciji trigger ne treba —
            // Interactor cilja OverlapSphere-om po colliderima, a dolazak teleporta
            // već slijeće uz rub solid collidera (PlanetCreator.TeleportToPlanet).
            MachinePlacer.FitColliderToRenderer(marker);

            ConnectionInteractable interactable = marker.AddComponent<ConnectionInteractable>();
            interactable.Init(planetCreator, sourcePlanet: from, targetPlanet: toward);

            return marker;
        }

        // Zajednički helper umjesto vlastite matematike: localScale kao radijus laže
        // za mesh planete (Hub ima localScale 1000 uz stvarni radijus ~19), pa je
        // fallback markere znao ostaviti stotine jedinica u svemiru pored zrake.
        internal static Vector3 SurfacePoint(Transform planet, Vector3 directionFromPlanet)
            => SurfacePoint(planet, directionFromPlanet, out _);

        internal static Vector3 SurfacePoint(Transform planet, Vector3 directionFromPlanet, out Vector3 surfaceNormal)
        {
            SurfacePlacement.GetSurfacePoint(planet, directionFromPlanet, out Vector3 point, out surfaceNormal);
            return point;
        }

        // Kao SurfacePoint, ali izbjegava zauzeto tlo: resursi se spawnaju isti
        // frame kao markeri (redoslijed Start korutina nije definiran), pa bi totem
        // znao završiti unutar kamena/drveta. Ako je idealna točka zauzeta, proba
        // se nekoliko bočnih pomaka oko nje (isti recept kao izlaz teleportera u
        // MachinePlacer.TryPlaceTeleporter); ako ni jedan nije čist, vraća zadnji
        // pokušaj — malo pomaknut marker je bolji od markera zabijenog u resurs.
        internal static Vector3 FindClearSurfacePoint(Transform planet, Vector3 idealDir)
        {
            Vector3 pos = SurfacePoint(planet, idealDir);
            if (IsGroundFree(pos, planet)) return pos;

            float radius = SurfacePlacement.GetPlanetRadius(planet);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 tangent = Vector3.Cross(idealDir, Random.onUnitSphere);
                if (tangent.sqrMagnitude < 0.01f) continue;
                tangent.Normalize();

                Vector3 dir = (idealDir * radius + tangent * 15f).normalized;
                pos = SurfacePoint(planet, dir);
                if (IsGroundFree(pos, planet)) break;
            }

            return pos;
        }

        // MachinePlacer.IsSpotClear + iznimka za igrača: marker aktivne veze se
        // spawna dok igrač stoji uz potencijalni totem, pa bi ga vlastiti capsule
        // inače svaki put otjerao s idealnog mjesta. Igrač se prepoznaje preko
        // PlayerHealth na rigu (isti uzorak kao VolcanicHazardZone) — čisto
        // "ima Rigidbody" ne valja jer resursi svoj RB gube kroz ODGOĐENI Destroy,
        // pa bi ih marker spawnan isti frame promašio.
        private static bool IsGroundFree(Vector3 pos, Transform planet)
        {
            foreach (var col in Physics.OverlapSphere(pos, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (col.transform == planet) continue;
                if (col.attachedRigidbody != null && col.attachedRigidbody.TryGetComponent(out PlayerHealth _)) continue;
                // Mobovi su POKRETNI collideri — mob koji se zatekne na idealnoj
                // točki bi trajno otjerao totem u stranu (kosa zraka), a sam ode
                // za par sekundi. Totem se smije spawnati "u mobu": PhysX moba
                // samo izgura van jer je dinamičan.
                if (col.attachedRigidbody != null && col.attachedRigidbody.TryGetComponent(out EnemyMob _)) continue;
                return false;
            }
            return true;
        }

        private void UpdateVisual()
        {
            Vector3 tipA = BeamAnchor(_markerA, PlanetA, PlanetB);
            Vector3 tipB = BeamAnchor(_markerB, PlanetB, PlanetA);

            Vector3 midpoint = (tipA + tipB) * 0.5f;
            float distance = Vector3.Distance(tipA, tipB);

            _cylinder.transform.position = midpoint;
            _cylinder.transform.up = (tipB - tipA).normalized;
            _cylinder.transform.localScale = new Vector3(_thickness, distance * 0.5f, _thickness);
        }

        // Kraj zrake: VISINA je markerHeight iznad pivota totema (izvorna, ne
        // izvodi se iz geometrije), a PRAVAC iz smjera druge planete prolazi
        // točno kroz šiljak totema. Bez ciljanja šiljka produžetak kose zrake
        // promašuje totem pa kapa visi u zraku pored/iznad njega — a to se
        // vidi tek na planetima gdje GroundToSurface totem UKOPA u neravan
        // teren (šiljak niže od markerHeight). Kraj nikad nije ispod šiljka,
        // pa zraka ne može ni progutati vrh. Bez totema: markerHeight iznad
        // točke površine.
        private Vector3 BeamAnchor(GameObject marker, Transform planet, Transform otherPlanet)
        {
            Vector3 dirToOther = (otherPlanet.position - planet.position).normalized;

            if (marker == null)
            {
                Vector3 basePos = SurfacePoint(planet, dirToOther);
                return basePos + (basePos - planet.position).normalized * _markerHeight;
            }

            Vector3 up = marker.transform.up;
            Vector3 pivot = marker.transform.position;

            Vector3 peak = SurfacePlacement.TryGetExtents(marker, up, out float lowest, out _, out float height) && height > 0.01f
                ? pivot + up * (lowest + height)
                : pivot + up * _markerHeight;

            // Točka na pravcu (šiljak -> druga planeta) na visini markerHeight
            // iznad pivota. climb = koliko se pravac penje po jedinici puta.
            Vector3 toOther = (otherPlanet.position - peak).normalized;
            float climb = Vector3.Dot(toOther, up);
            float need = _markerHeight - Vector3.Dot(peak - pivot, up);

            if (climb > 0.05f && need > 0f)
            {
                Vector3 candidate = peak + toOther * (need / climb);
                // Preplitki smjer (jako pomaknut marker na maloj planeti) bi
                // kraj otjerao daleko od totema — tada radije ostani na šiljku.
                if ((candidate - peak).sqrMagnitude <= 4f * _markerHeight * _markerHeight)
                    return candidate;
            }

            // Šiljak je već na/iznad markerHeight ili je smjer preplitak:
            // sidri na sam šiljak.
            return peak;
        }

        public void ApplyDamage(float amount)
        {
            Health = Mathf.Clamp(Health - amount, 0f, 100f);
            GameEventBus.Raise(new ConnectionHealthChangedEvent
            {
                Health = Health,
                PlanetA = PlanetA,
                PlanetB = PlanetB,
                ConnectionType = Type
            });

            if (Health <= 0f)
            {
                GameEventBus.RaiseConnectionDestroyed(new ConnectionEvent
                {
                    PlanetA = PlanetA,
                    PlanetB = PlanetB,
                    ConnectionType = Type
                });
                Destroy(gameObject);
            }
        }

        public bool Connects(Transform a, Transform b) =>
            (PlanetA == a && PlanetB == b) || (PlanetA == b && PlanetB == a);
    }
}
