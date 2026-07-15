using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Baza na Hubu: prostor oko računala i skladišta. Odavde se računa središte i
    // radijus baze — GameManager tu smješta glavni respawn totem i gradi dekoraciju,
    // a HubResourceSpawner unutar radijusa ne spawna resurse.
    internal static class HubBase
    {
        // Margina oko računala/skladišta da baza ne bude pretijesna.
        private const float AreaMargin = 8f;

        // Središte = sredina između računala i skladišta projicirana na površinu;
        // radijus prati njihov stvarni razmak u sceni.
        public static bool TryGetArea(Transform hub, out Vector3 center, out float radius)
        {
            center = default;
            radius = 0f;

            var computer = Object.FindFirstObjectByType<NetworkComputerInteractable>();
            var storage  = Object.FindFirstObjectByType<HubStorageInteractable>();
            if (computer == null && storage == null) return false;

            Vector3 a = computer != null ? computer.transform.position : storage.transform.position;
            Vector3 b = storage  != null ? storage.transform.position  : a;

            center = SnapToSurface(hub, (a + b) * 0.5f);

            // Cap na pola radijusa planeta — na malom hubu bi se plato i svjetiljke
            // inače raštrkali preko pola kugle.
            float hubRadius = SurfacePlacement.GetPlanetRadius(hub);
            radius = Mathf.Min(Mathf.Max(10f, Vector3.Distance(a, b) * 0.5f + AreaMargin), hubRadius * 0.5f);
            return true;
        }

        // Kandidati u krugu oko središta baze; prvi s čistim tlom (da totem ne završi
        // u računalu/skladištu), inače prvi kandidat.
        public static Vector3 FindTotemSpot(Transform hub, Vector3 center, float radius)
        {
            Vector3 normal = (center - hub.position).normalized;
            Vector3 fallback = center;

            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = TangentDir(normal, i * 45f);
                Vector3 candidate = SnapToSurface(hub, center + dir * radius * 0.5f);
                if (i == 0) fallback = candidate;
                if (MachinePlacer.IsSpotClear(candidate, hub)) return candidate;
            }

            return fallback;
        }

        // Dekoracija oko spawn pointa: popločani plato (središnja ploča + dva prstena —
        // male ploče prate zakrivljenost planeta bolje nego jedna velika), svjetiljke
        // po rubu i meko svjetlo uz totem. Sve primitivi bez collidera pod zajedničkim
        // root objektom "HubBase" da ne smetaju postavljanju strojeva i interakciji.
        public static void BuildDecor(Transform hub, Vector3 center, float radius, Vector3 totemPos)
        {
            var root = new GameObject("HubBase");
            Vector3 normal = (center - hub.position).normalized;

            Color tileA = new(0.34f, 0.37f, 0.43f);
            Color tileB = new(0.26f, 0.29f, 0.34f);
            PlaceTile(root, hub, center, 3.2f, tileA);
            PlaceTileRing(root, hub, center, normal, radius * 0.45f, 8, 2.6f, tileB);
            PlaceTileRing(root, hub, center, normal, radius * 0.8f, 14, 2.2f, tileA);

            for (int i = 0; i < 6; i++)
            {
                Vector3 dir = TangentDir(normal, i * 60f);
                PlaceLantern(root, hub, SnapToSurface(hub, center + dir * radius));
            }

            // Zelenkasto svjetlo uz totem — spawn point se vidi izdaleka.
            var glow = new GameObject("TotemGlow");
            glow.transform.SetParent(root.transform, false);
            glow.transform.position = totemPos + normal * 4f;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.55f, 1f, 0.65f);
            light.range = 18f;
            light.intensity = 2.5f;
        }

        private static Vector3 TangentDir(Vector3 normal, float angleDeg)
        {
            Vector3 t = Vector3.Cross(normal, Vector3.up);
            if (t.sqrMagnitude < 0.01f) t = Vector3.Cross(normal, Vector3.right);
            return Quaternion.AngleAxis(angleDeg, normal) * t.normalized;
        }

        private static Vector3 SnapToSurface(Transform hub, Vector3 nearPos)
        {
            float hubRadius = SurfacePlacement.GetPlanetRadius(hub);
            Vector3 dir = (nearPos - hub.position).normalized;
            if (SurfacePlacement.TryRaycastSurface(hub, hub.position + dir * (hubRadius + 20f), -dir,
                    hubRadius + 40f, out RaycastHit hit))
                return hit.point;
            return hub.position + dir * hubRadius;
        }

        private static void PlaceTileRing(GameObject root, Transform hub, Vector3 center, Vector3 normal,
            float ringRadius, int count, float tileSize, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = TangentDir(normal, i * (360f / count));
                PlaceTile(root, hub, SnapToSurface(hub, center + dir * ringRadius), tileSize, color);
            }
        }

        private static void PlaceTile(GameObject root, Transform hub, Vector3 pos, float size, Color color)
        {
            Vector3 up = (pos - hub.position).normalized;
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tile.name = "Tile";
            tile.transform.SetParent(root.transform, false);
            tile.transform.SetPositionAndRotation(pos + up * 0.05f, Quaternion.FromToRotation(Vector3.up, up));
            tile.transform.localScale = new Vector3(size, 0.08f, size);
            tile.GetComponent<Renderer>().material.color = color;
            Object.Destroy(tile.GetComponent<Collider>());
        }

        private static void PlaceLantern(GameObject root, Transform hub, Vector3 pos)
        {
            Vector3 up = (pos - hub.position).normalized;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, up);

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "LanternPost";
            post.transform.SetParent(root.transform, false);
            post.transform.SetPositionAndRotation(pos + up * 1.5f, rot);
            post.transform.localScale = new Vector3(0.45f, 3f, 0.45f);
            post.GetComponent<Renderer>().material.color = new Color(0.2f, 0.21f, 0.25f);
            Object.Destroy(post.GetComponent<Collider>());

            Color warm = new(1f, 0.82f, 0.5f);
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "LanternBulb";
            bulb.transform.SetParent(root.transform, false);
            bulb.transform.position = pos + up * 3.3f;
            bulb.transform.localScale = Vector3.one * 0.9f;
            var mat = bulb.GetComponent<Renderer>().material;
            mat.color = warm;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", warm * 2f);
            Object.Destroy(bulb.GetComponent<Collider>());

            var light = bulb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = warm;
            light.range = 12f;
            light.intensity = 1.6f;
        }
    }
}
