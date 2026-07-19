using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Spawna EnemyMob-ove na svakom otkrivenom planetu (osim Huba — sigurna baza).
    // Isti obrazac kao VolcanicHazardSpawner: OnPlanetDiscovered + prolaz kroz već
    // postojeće planete nakon PlanetCreator.Start. Mob se gradi proceduralno iz
    // primitiva pa nije potreban prefab ni referenca u sceni.
    public class EnemyMobSpawner : MonoBehaviour
    {
        [SerializeField] private int minMobsPerPlanet = 3;
        [SerializeField] private int maxMobsPerPlanet = 5;
        [Tooltip("Skala alien modela — kit modeli su mali (totemi koriste 5, smelter 3).")]
        [SerializeField] private float modelScale = 3f;
        [Tooltip("Boja fallback kapsule kad alien model nije u Resources.")]
        [SerializeField] private Color bodyColor = new Color(0.65f, 0.12f, 0.12f);

        private readonly HashSet<Transform> _processed = new();

        // Runtime bootstrap umjesto dodavanja u scenu — editor drži scenu u
        // memoriji pa disk izmjene scene ne prežive (isti razlog kao Planet.Awake).
        // Guard dopušta i ručno postavljen spawner u sceni bez dupliranja.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<EnemyMobSpawner>() != null) return;
            new GameObject("EnemyMobSpawner").AddComponent<EnemyMobSpawner>();
        }

        void OnEnable()  => GameEventBus.OnPlanetDiscovered += OnPlanetDiscovered;
        void OnDisable() => GameEventBus.OnPlanetDiscovered -= OnPlanetDiscovered;

        private IEnumerator Start()
        {
            yield return null; // wait for PlanetCreator.Start() to finish spawning
            foreach (var planet in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                OnPlanetDiscovered(planet.transform);
        }

        private void OnPlanetDiscovered(Transform planetTransform)
        {
            if (_processed.Contains(planetTransform)) return;
            _processed.Add(planetTransform);

            Planet planet = planetTransform.GetComponent<Planet>();
            if (planet == null || planet.IsHub) return;

            int count = Random.Range(minMobsPerPlanet, maxMobsPerPlanet + 1);
            for (int i = 0; i < count; i++)
                SpawnMob(planetTransform);
        }

        private void SpawnMob(Transform planet)
        {
            // Čisto tlo kao kod strojeva/totema: mob spawnan u totemu/resursu bi
            // depenetracijom odletio, a mob na idealnoj točki veze bi smetao
            // markerima. Nakon 8 pokušaja prihvati zadnju točku (mali planeti).
            Vector3 dir = Random.onUnitSphere;
            SurfacePlacement.GetSurfacePoint(planet, dir, out Vector3 hitPoint, out Vector3 hitNormal);
            for (int attempt = 0; attempt < 8 && !MachinePlacer.IsSpotClear(hitPoint, planet); attempt++)
            {
                dir = Random.onUnitSphere;
                SurfacePlacement.GetSurfacePoint(planet, dir, out hitPoint, out hitNormal);
            }
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hitNormal);

            // Vizual: alien iz Space kita. Spawner se bootstrapa runtime pa nema
            // Inspector referencu — model se čita iz Resources kopije (isti fallback
            // obrazac kao GameManager za hub totem data). Bez modela: crvena kapsula.
            GameObject model = Resources.Load<GameObject>("EnemyMobAlien");

            GameObject mob;
            if (model != null)
            {
                mob = new GameObject("EnemyMob");
                mob.transform.SetPositionAndRotation(hitPoint, rot);

                GameObject visual = Instantiate(model, mob.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * modelScale;
            }
            else
            {
                mob = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                mob.transform.localScale = Vector3.one * 0.8f;
                mob.transform.SetPositionAndRotation(hitPoint, rot);
                mob.GetComponent<Renderer>().material.color = bodyColor;

                AddEye(mob.transform, new Vector3(0.18f, 0.55f, 0.38f));
                AddEye(mob.transform, new Vector3(-0.18f, 0.55f, 0.38f));
            }
            mob.name = "EnemyMob";

            // Isti put kao strojevi (MachinePlacer.SpawnObject): dno stvarne
            // geometrije na površinu, pa jedan BoxCollider po granicama geometrije.
            SurfacePlacement.GroundToSurface(mob, planet, hitPoint, hitNormal);
            SurfacePlacement.FitBoxColliderToGeometry(mob);

            Rigidbody rig = mob.AddComponent<Rigidbody>();
            rig.mass = 1f;

            mob.AddComponent<EnemyMob>().Init(planet);
        }

        private static void AddEye(Transform body, Vector3 localPos)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(body, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = Vector3.one * 0.18f;
            eye.GetComponent<Renderer>().material.color = Color.white;
        }
    }
}
