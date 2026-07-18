using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Dijagnostika za prijave "objekt lebdi/tone": za svaki interaktivni objekt u
    // sceni izmjeri razmak najniže točke geometrije od površine najbliže planete i
    // ispiše prekršitelje s brojkama. Poziva se iz editor menija
    // (Tools/Web of Planets/Audit Surface Placement) u Play modu.
    public static class SurfaceAudit
    {
        // Iznad ovog razmaka objekt se vodi kao "lebdi"; ukop se uspoređuje s
        // namjernim ukopom izračunatim istim ComputeSink pozivom kao u GroundToSurface.
        private const float FloatTolerance = 0.15f;

        public static void LogReport()
        {
            Planet[] planets = Object.FindObjectsByType<Planet>(FindObjectsSortMode.None);
            if (planets.Length == 0)
            {
                Debug.Log("SurfaceAudit: nema planeta u sceni.");
                return;
            }

            var offenders = new List<(float severity, string line)>();
            int total = 0;

            // Broj provjerenih objekata po planetu — dokaz da audit pokriva SVE
            // planete, ne samo Hub (objekt se pripisuje najbližem planetu).
            var perPlanet = new Dictionary<Planet, int>();
            foreach (var p in planets) perPlanet[p] = 0;

            foreach (var interactable in Object.FindObjectsByType<BaseInteractable>(FindObjectsSortMode.None))
            {
                Transform t = interactable.transform;

                Planet planet = NearestPlanet(planets, t.position, out float distanceToCenter);
                if (planet == null) continue;

                Vector3 normal = (t.position - planet.transform.position).normalized;
                SurfacePlacement.GetSurfacePoint(planet.transform, normal, out Vector3 surfacePoint, out Vector3 surfaceNormal);

                // Ista mjera kao u GroundToSurface (vrhovi/OBB) — world AABB rotiranog
                // objekta bi za velike objekte prijavio lažni ukop od pola dijagonale.
                if (!SurfacePlacement.TryGetExtents(t.gameObject, surfaceNormal, out float lowest, out float halfWidth, out float height))
                    continue;

                float gap = Vector3.Dot(t.position - surfacePoint, surfaceNormal) + lowest;

                float planetRadius = SurfacePlacement.GetPlanetRadius(planet.transform);
                float expectedSink = SurfacePlacement.ComputeSink(planet.transform, surfacePoint, surfaceNormal, halfWidth, height);

                total++;
                perPlanet[planet]++;

                // Strojevi/totemi (i totemi veza) se prizemljuju duž VLASTITE
                // (radijalne) osi, a audit mjeri po normali trokuta pod objektom. Na
                // nagnutom trokutu (do ~8° na generiranim planetima) objekt je namjerno
                // ukopan da mu niži rub ne visi: primijenjeni ukop ~w·tanθ (cap 30%
                // visine) + dubina uzbrdnog kuta ~w·sinθ. Bez te tolerancije svaki širi
                // stroj na nagibu ispadne lažni "UTONUO". Za resurse (prizemljene po
                // normali trokuta, θ≈0) slack je ≈0 pa se pragovi ne mijenjaju.
                float cosTilt = Mathf.Abs(Vector3.Dot(surfaceNormal, t.up));
                float sinTilt = Mathf.Sqrt(Mathf.Max(0f, 1f - cosTilt * cosTilt));
                float appliedTiltSink = cosTilt > 0.5f
                    ? Mathf.Min(height * 0.3f, halfWidth * sinTilt / cosTilt)
                    : height * 0.3f;
                float tiltSlack = appliedTiltSink + halfWidth * sinTilt;

                bool floats = gap > FloatTolerance;
                bool buried = gap < -(expectedSink + tiltSlack + 0.5f);
                if (!floats && !buried) continue;

                string verdict = floats ? "LEBDI" : "UTONUO";
                offenders.Add((Mathf.Abs(gap), $"{verdict} {gap,7:F2} (namjerni ukop {-expectedSink:F2}) | {interactable.GetType().Name,-28} '{t.name}' na '{planet.name}' (R={planetRadius:F1}, udaljenost od centra {distanceToCenter:F1})"));
            }

            string coverage = $"pokrivenost ({planets.Length} planeta): " + string.Join(", ",
                perPlanet.OrderBy(kv => kv.Key.name).Select(kv => $"{kv.Key.name}={kv.Value}"));

            if (offenders.Count == 0)
            {
                Debug.Log($"SurfaceAudit: svih {total} provjerenih objekata sjedi na površini (tolerancija {FloatTolerance}); {coverage}");
                return;
            }

            offenders.Sort((a, b) => b.severity.CompareTo(a.severity));
            var sb = new StringBuilder();
            sb.AppendLine($"SurfaceAudit: {offenders.Count}/{total} objekata odstupa od površine (pozitivno = lebdi, negativno = utonulo):");
            foreach (var (_, line) in offenders)
                sb.AppendLine("  " + line);
            sb.AppendLine("SurfaceAudit " + coverage);
            Debug.LogWarning(sb.ToString());
        }

        private static Planet NearestPlanet(Planet[] planets, Vector3 pos, out float distanceToCenter)
        {
            Planet nearest = null;
            float best = float.MaxValue;
            foreach (var p in planets)
            {
                float d = Vector3.Distance(pos, p.transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = p;
                }
            }
            distanceToCenter = best;
            return nearest;
        }

    }
}
