using System.IO;
using UnityEngine;

namespace WebOfPlanets
{
    // Save/load u JSON (jedan slot, Application.persistentDataPath). Sprema se:
    // proceduralne planete, aktivne veze (tip + zdravlje), strojevi s vezama
    // (collector→storage, teleporter parovi), respawn totemi, sadržaj storage
    // strojeva, hub skladište, inventar, hotbar (s trajnošću), hub prag i igrač.
    //
    // Load NE reloada scenu (runtime-bootstrap sustavi poput MainMenuUI/VfxManagera
    // bi nestali): proceduralni svijet se sruši u mjestu i ponovno izgradi kroz
    // ISTE puteve kao world-gen — Planet.Start opet raise-a OnPlanetDiscovered pa
    // se vulkanske zone i mobovi sami spawnaju. Resursi su iznimka: spremaju se
    // pojedinačno (item, pozicija, pickup/mining) pa se svježi spawn za load-ane
    // planete preskače (ResourceSpawnManager.MarkProcessed). Sprema se i Broken
    // stanje te interni bufferi svih strojeva.
    //
    // Svjesna pojednostavljenja: mining progress u tijeku i regeneracijski timeri
    // resursa se ne spremaju (resurs u regeneraciji se vrati vidljiv); hub dekor
    // resursi se ne diraju (HubResourceSpawner ih drži).
    //
    // Partial-split po odgovornostima: SaveSystem.Dto.cs (shema datoteke),
    // SaveSystem.Capture.cs (snimanje), SaveSystem.Restore.cs (rušenje + rebuild);
    // ovdje su put datoteke i zajednički helperi.
    public static partial class SaveSystem
    {
        private const int KindCollector = 0, KindStorage = 1, KindSmelter = 2,
                          KindExtractor = 3, KindUplink = 4, KindTeleporter = 5, KindTotem = 6,
                          KindComputer = 7;

        public static string SavePath => Path.Combine(Application.persistentDataPath, "webofplanets_save.json");
        public static bool SaveExists => File.Exists(SavePath);

        // ── Zajednički helperi ────────────────────────────────────────────────

        // Asseti se traže po tipu + imenu među učitanima (Resources.LoadAll u
        // LoadRoutine povuče sve iz Resources foldera, a recepti transitivno i
        // svoje result assete). Tipizirano jer se imena ponavljaju (recept
        // "Teleporter" vs machine data "Teleporter"). Keš: load zove Resolve u
        // petljama (svaki resurs/stroj/stack), a FindObjectsOfTypeAll je linearni
        // sken; asseti se runtime ne mijenjaju pa keš ne stari.
        private static readonly System.Collections.Generic.Dictionary<(System.Type, string), ScriptableObject> ResolveCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ResolveCache.Clear();

        private static T Resolve<T>(string assetName) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            var key = (typeof(T), assetName);
            if (ResolveCache.TryGetValue(key, out var cached) && cached != null)
                return (T)cached;

            foreach (var o in Resources.FindObjectsOfTypeAll<T>())
                if (o.name == assetName)
                {
                    ResolveCache[key] = o;
                    return o;
                }

            Debug.LogWarning($"[SaveSystem] Asset '{assetName}' ({typeof(T).Name}) nije pronađen — stavka preskočena.");
            return null;
        }

        private static void DestroyAll<T>() where T : Component
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(c.gameObject);
        }

        private static Transform FindHub()
        {
            foreach (var p in UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p.transform;
            return null;
        }

        private static Transform ClosestPlanet(Vector3 pos)
        {
            Planet best = ClosestPlanetOf(pos, UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.None));
            return best != null ? best.transform : null;
        }

        private static Planet ClosestPlanetOf(Vector3 pos, Planet[] planets)
        {
            Planet best = null;
            float bestDist = float.MaxValue;
            foreach (var p in planets)
            {
                float d = (p.transform.position - pos).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        private static bool IsClosestPlanetHub(Vector3 pos, Planet[] planets)
        {
            Planet best = ClosestPlanetOf(pos, planets);
            return best != null && best.IsHub;
        }
    }
}
