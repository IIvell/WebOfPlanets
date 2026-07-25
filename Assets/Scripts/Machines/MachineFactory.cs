using UnityEngine;

namespace WebOfPlanets
{
    // Statički factory svjetskih objekata (strojevi, totemi, markeri, mobovi).
    // Ranije je živio u MachinePlaceru — input komponenti — a zvali su ga
    // SaveSystem, PlanetConnection, ConnectionManager, HubBase, EnemyMobSpawner,
    // RespawnTotem, ComputerMachine i GameManager. Ovdje je i JEDINA tablica
    // fallback boja i default scale-ova po tipu stroja: MachinePlacer i
    // SaveSystem su ranije držali po vlastitu kopiju boja, koje su već
    // divergirale.
    public static class MachineFactory
    {
        // Fallback boje kocke — koriste se samo kad stroj nema prefab.
        public static readonly Color CollectorColor  = new(0.2f, 0.6f, 1f);
        public static readonly Color StorageColor    = new(0.8f, 0.4f, 0f);
        public static readonly Color SmelterColor    = new(0.9f, 0.2f, 0.1f);
        public static readonly Color ExtractorColor  = new(0.1f, 0.8f, 0.5f);
        public static readonly Color UplinkColor     = new(0.2f, 0.8f, 0.9f);
        public static readonly Color TeleporterColor = new(0.6f, 0.3f, 0.9f);
        public static readonly Color TwoWayGateColor = new(1f, 0.6f, 0.1f);

        // Dvosmjerni gate ima svoju boju — SaveSystem je ranije sve teleportere
        // loadao u boji običnog (dokumentirani defekt-fix, PLAN-KOD §1).
        public static Color TeleporterColorFor(TeleporterMachineData data) =>
            data is TwoWayTeleporterMachineData ? TwoWayGateColor : TeleporterColor;

        // Default world scale pri postavljanju po tipu (SaveSystem sprema stvarni
        // localScale pa kod loada koristi spremljeni). Collector jedini čita
        // MachineData.worldScale iz asseta.
        public const float StorageScale    = 150f;
        public const float SmelterScale    = 3f;
        public const float ExtractorScale  = 7f;
        public const float UplinkScale     = 7f;
        public const float TeleporterScale = 7f;
        public const float TotemScale      = 5f;
        // 0.52 = world skala hub Računala u sceni (ista Computer.fbx instanca).
        public const float ComputerScale   = 0.52f;

        // pos mora biti točka na površini planeta; uz zadan planet objekt se
        // prizemlji tako da mu dno stvarne geometrije sjedne na pos, bez obzira
        // gdje je pivot prefaba.
        public static GameObject SpawnObject(GameObject prefab, Vector3 pos, Quaternion rot,
            string fallbackName, Color fallbackColor, float scale = 300f, Quaternion? rotationOffset = null,
            bool fitColliderToRenderer = false, Transform planet = null)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, pos, rot);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(pos, rot);
                go.GetComponent<Renderer>().material.color = fallbackColor;
            }

            go.transform.localScale = Vector3.one * scale;
            go.transform.rotation   = rot * (rotationOffset ?? Quaternion.Euler(-90f, 0f, 0f));
            go.name = fallbackName;

            if (fitColliderToRenderer)
                SurfacePlacement.FitBoxColliderToGeometry(go);
            else if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Object.Destroy(rb);

            // rot je FromToRotation(Vector3.up, normala), pa je rot * up normala površine
            // i kad rotationOffset dodatno zakrene model.
            if (planet != null)
                SurfacePlacement.GroundToSurface(go, planet, pos, rot * Vector3.up);

            // Zajednički put svih postavljanja (strojevi, totemi) — prašina oko baze.
            VfxManager.PlayMachinePlaced(pos, rot * Vector3.up);

            return go;
        }

        // Standardni spawn STROJA: model uspravno na normalu (bez default -90°
        // offseta prefab modela), BoxCollider po stvarnoj geometriji, prizemljen
        // na planet.
        public static GameObject SpawnMachine(GameObject prefab, Vector3 pos, Quaternion rot,
            string name, Color fallbackColor, float scale, Transform planet)
            => SpawnObject(prefab, pos, rot, name, fallbackColor, scale,
                rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);

        // Čisto tlo: u radijusu objekta ne smije biti ničeg osim same planete.
        public static bool IsSpotClear(Vector3 pos, Transform planet)
        {
            foreach (var col in Physics.OverlapSphere(pos, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (col.transform != planet) return false;
            return true;
        }
    }
}
