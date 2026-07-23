using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    internal static class SurfacePlacement
    {
        // mesh.vertices vraća kopiju cijelog polja pri svakom pozivu — keš sprječava
        // GC pritisak kad world-gen u jednom frameu prizemljuje stotine objekata.
        private static readonly Dictionary<Mesh, Vector3[]> VertexCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => VertexCache.Clear();

        // Jedinstveni izračun radijusa planeta: za primitivne sfere (PlanetCreator)
        // isto kao localScale.x * 0.5, a za mesh planete (Hub, Planet.fbx) localScale
        // laže pa se čita iz renderer boundsa.
        public static float GetPlanetRadius(Transform planet)
        {
            Renderer rend = planet.GetComponentInChildren<Renderer>();
            return rend != null ? rend.bounds.size.x * 0.5f : planet.localScale.x * 0.5f;
        }

        // Raycast can otherwise hit a previously spawned resource's collider instead of
        // the planet, so only accept hits against the planet's own collider.
        public static bool TryRaycastSurface(Transform planet, Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance);
            float closestDistance = float.MaxValue;
            RaycastHit closestHit = default;
            bool found = false;

            foreach (var h in hits)
            {
                if (h.collider.transform != planet) continue;
                if (h.distance >= closestDistance) continue;
                closestDistance = h.distance;
                closestHit = h;
                found = true;
            }

            hit = closestHit;
            return found;
        }

        // Kanonska točka površine u smjeru dir od centra planeta. Origin s marginom
        // iznad radijusa — ray koji kreće točno na radijusu zna početi na/unutar
        // collidera i promašiti, pa bi se koristio neprecizni analitički fallback.
        public static void GetSurfacePoint(Transform planet, Vector3 dir, out Vector3 point, out Vector3 normal)
        {
            float radius = GetPlanetRadius(planet);
            Vector3 origin = planet.position + dir * (radius + 20f);
            float maxDistance = radius + 40f;

            if (!TryRaycastSurface(planet, origin, -dir, maxDistance, out RaycastHit hit))
            {
                // World-gen (resursi, markeri, totemi) raycasta frame nakon kreiranja
                // planeta, a autoSyncTransforms je u projektu isključen — collider u
                // PhysX-u zna još biti na staroj pozi pa ray promaši i sve padne na
                // analitički fallback (koji na mesh planetima zna biti par jedinica
                // iznad stvarne površine). Sinkaj transforme pa pokušaj još jednom.
                Physics.SyncTransforms();
                if (!TryRaycastSurface(planet, origin, -dir, maxDistance, out hit))
                {
                    // Analitička točka leži na opisanoj kugli — na poligonalnim
                    // planetima do ~1.3% R IZNAD vidljivih trokuta, pa objekt na njoj
                    // lebdi. Upozori umjesto tihe degradacije da se stvarna
                    // pojavljivanja vide u konzoli uz Play-mode audit.
                    Debug.LogWarning($"SurfacePlacement: raycast na '{planet.name}' promašio i nakon SyncTransforms — analitički fallback (objekt može lebdjeti).");
                    point = planet.position + dir * radius;
                    normal = dir;
                    return;
                }
            }

            point = hit.point;
            normal = hit.normal;
        }

        // Pomiče objekt duž normale da mu najniža stvarna točka geometrije sjedne na
        // surfacePoint (+gap), bez obzira gdje je pivot prefaba. Uz to ga ukopa
        // koliko mu rub dna stvarno visi iznad tla (ComputeSink), da ravno dno na
        // zakrivljenoj/nagnutoj podlozi ne izgleda kao da lebdi na rubovima.
        public static void GroundToSurface(GameObject go, Transform planet, Vector3 surfacePoint, Vector3 surfaceNormal, float gap = 0f)
        {
            if (!TryGetExtents(go, surfaceNormal, out float lowest, out float halfWidth, out float height))
                return;

            float sink = ComputeSink(planet, surfacePoint, surfaceNormal, halfWidth, height);

            float current = Vector3.Dot(go.transform.position - surfacePoint, surfaceNormal) + lowest;
            go.transform.position += surfaceNormal * (gap - sink - current);
        }

        // Namjerni ukop = najveći IZMJERENI razmak ruba dna od tla: tlo se uzorkuje
        // raycastima na 4 rubne točke footprinta (polumjer halfWidth oko
        // surfacePointa). Prije se zakrivljenost modelirala analitički (sagitta
        // w²/2R), ali collider je sada na svim planetima jednak vidljivom meshu:
        // na ravnom trokutu primitivne sfere ispravan ukop je 0, a formula bi
        // objekt bezrazložno zakopala do ~1.3% R. Cap od 30% visine kao i prije;
        // analitička formula ostaje samo kao fallback kad svi rubni raycastovi
        // promaše. Internal i zbog SurfaceAudita — dijagnostika mora računati
        // očekivani ukop identično kao prizemljenje.
        internal static float ComputeSink(Transform planet, Vector3 surfacePoint, Vector3 surfaceNormal, float halfWidth, float height)
        {
            float maxSink = height * 0.3f;
            if (halfWidth < 0.01f || maxSink <= 0f) return 0f;

            Vector3 tangent = Vector3.Cross(surfaceNormal, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(surfaceNormal, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(surfaceNormal, tangent);

            // Origin dovoljno iznad da preskoči lokalne uzvisine. Ray seže osjetno
            // ispod capa ukopa: dublji pad ruba (litica) mora se IZMJERITI pa tek
            // onda odrezati clampom — prekratki ray bi uzorak odbacio i rub bi
            // ostao visjeti. Promašaj i dalje preskače uzorak (može značiti i da je
            // origin unutar susjednog brda), nikad ne implicira maksimalni ukop.
            float margin = height + 5f;
            float rimGap = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 rimDir = i == 0 ? tangent : i == 1 ? -tangent : i == 2 ? bitangent : -bitangent;
                Vector3 origin = surfacePoint + rimDir * halfWidth + surfaceNormal * margin;
                if (!TryRaycastSurface(planet, origin, -surfaceNormal, margin + maxSink + halfWidth + 2f, out RaycastHit hit))
                    continue;

                // Koliko je tlo pod rubom niže od tla pod centrom (pozitivno = rub
                // bi visio); uzvisine daju negativno i ne doprinose ukopu.
                float drop = Vector3.Dot(surfacePoint - hit.point, surfaceNormal);
                if (drop > rimGap) rimGap = drop;
            }

            if (rimGap > float.MinValue)
                return Mathf.Clamp(rimGap, 0f, maxSink);

            float radius = GetPlanetRadius(planet);
            return radius > 0.01f
                ? Mathf.Min(halfWidth * halfWidth / (2f * radius), maxSink)
                : 0f;
        }

        // lowest: pomak najniže točke geometrije od pivota po normali (obično negativan).
        // halfWidth: najveća radijalna udaljenost geometrije od osi kroz pivot duž normale.
        // height: proteg geometrije duž normale.
        // Internal i zbog SurfaceAudita: dijagnostika mora mjeriti identično kao
        // prizemljenje, inače prijavljuje lažne ukope za velike rotirane objekte.
        internal static bool TryGetExtents(GameObject go, Vector3 normal, out float lowest, out float halfWidth, out float height)
        {
            Vector3 pivot = go.transform.position;
            float minProj = float.MaxValue;
            float maxProj = float.MinValue;
            float maxRadialSqr = 0f;

            foreach (Vector3 worldPoint in GeometryPoints(go))
            {
                Vector3 fromPivot = worldPoint - pivot;
                float proj = Vector3.Dot(fromPivot, normal);
                if (proj < minProj) minProj = proj;
                if (proj > maxProj) maxProj = proj;
                float radialSqr = (fromPivot - normal * proj).sqrMagnitude;
                if (radialSqr > maxRadialSqr) maxRadialSqr = radialSqr;
            }

            if (minProj == float.MaxValue)
            {
                lowest = 0f;
                halfWidth = 0f;
                height = 0f;
                return false;
            }

            lowest = minProj;
            height = maxProj - minProj;
            halfWidth = Mathf.Sqrt(maxRadialSqr);
            return true;
        }

        // Stvarni najviši vrh geometrije duž normale, kao world TOČKA (ne samo
        // visina): kod nagnutih/asimetričnih modela šiljak nije na osi pivota,
        // pa "osna točka na visini vrha" promašuje vidljivi vrh bočno — zraka
        // veze mora ciljati baš vrh (PlanetConnection.BeamAnchor). SAMO stvarni
        // vrhovi (preciseOnly): najviši KUT bounds boxa nije vrh — bočno je
        // pomaknut za pola širine modela, pa je gore od bilo kojeg fallbacka.
        // Mesh bez Read/Write → false, pozivatelj neka koristi svoj fallback.
        public static bool TryGetTopPoint(GameObject go, Vector3 normal, out Vector3 top)
        {
            top = default;
            float maxProj = float.MinValue;

            foreach (Vector3 worldPoint in GeometryPoints(go, preciseOnly: true))
            {
                float proj = Vector3.Dot(worldPoint, normal);
                if (proj > maxProj)
                {
                    maxProj = proj;
                    top = worldPoint;
                }
            }

            return maxProj != float.MinValue;
        }

        // Sve world točke stvarne geometrije objekta. Mjeri po stvarnim vrhovima kad
        // je mesh Read/Write, inače po kutovima lokalnog mesh boundsa transformiranim
        // u world (tight OBB) — world AABB već rotiranog objekta bi napuhao mjere
        // (širinu do √2) ovisno o orijentaciji na planeti. preciseOnly preskače sve
        // bounds fallbackove i vraća isključivo stvarne vrhove (za TryGetTopPoint:
        // ekstremi projekcija po boundsima su OK, ali kut boxa kao TOČKA nije).
        private static IEnumerable<Vector3> GeometryPoints(GameObject go, bool preciseOnly = false)
        {
            bool any = false;

            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null) continue;
                any = true;

                Vector3[] vertices = GetReadableVertices(mesh);
                if (vertices != null)
                {
                    Transform meshTransform = mf.transform;
                    foreach (Vector3 v in vertices)
                        yield return meshTransform.TransformPoint(v);
                }
                else if (!preciseOnly)
                {
                    Bounds b = mesh.bounds;
                    for (int i = 0; i < 8; i++)
                        yield return mf.transform.TransformPoint(b.center + Vector3.Scale(b.extents, Corner(i)));
                }
            }

            // Skinned meshevi nemaju MeshFilter, a SkinnedMeshRenderer.bounds su
            // izvedene iz KOSTIJU (bind-pose bounds u prostoru root bonea) — kod
            // riganih FBX-ova s Blender armature scale nakaradom sežu desetke
            // jedinica od vidljive geometrije, pa bi objekt (npr. 'Ice' fridge)
            // nakon prizemljenja visio u zraku, a audit bi šutio jer mjeri isto.
            // Zato se mjeri BAKANA skinnana geometrija = ono što se stvarno crta;
            // world bounds ostaju samo fallback ako bake ne vrati vrhove.
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                any = true;

                var baked = new Mesh();
                try
                {
                    // useScale:true → vrhovi su stvarno u lokalnom prostoru i s
                    // TransformPoint se točno preslikavaju u world. Default overload
                    // ih peče s lossyScaleom, pa bi TransformPoint skalu primijenio
                    // DVAPUT — kriva mjera za svaki spawn s world scaleom != 1.
                    smr.BakeMesh(baked, true);
                    Vector3[] bakedVerts = baked.vertices;
                    if (bakedVerts.Length > 0)
                    {
                        Transform smrTransform = smr.transform;
                        foreach (Vector3 v in bakedVerts)
                            yield return smrTransform.TransformPoint(v);
                    }
                    else if (!preciseOnly)
                    {
                        Bounds wb = smr.bounds;
                        for (int i = 0; i < 8; i++)
                            yield return wb.center + Vector3.Scale(wb.extents, Corner(i));
                    }
                }
                finally
                {
                    if (Application.isPlaying) Object.Destroy(baked);
                    else Object.DestroyImmediate(baked);
                }
            }

            // Bez ijednog MeshFiltera/skinned mesha (npr. samo particle rendereri):
            // world AABB kutovi.
            if (!any && !preciseOnly)
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) yield break;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                for (int i = 0; i < 8; i++)
                    yield return bounds.center + Vector3.Scale(bounds.extents, Corner(i));
            }
        }

        // AABB stvarne geometrije u lokalnom prostoru roota — isti izvor točaka kao
        // TryGetExtents, pa BoxCollider prati ono što se stvarno crta i za skinned
        // modele (SMR.bounds su bone-frame AABB, znaju biti jedinice od vidljivog mesha).
        internal static bool TryGetLocalBounds(GameObject go, out Bounds localBounds)
        {
            Transform root = go.transform;
            bool has = false;
            Bounds b = default;

            foreach (Vector3 worldPoint in GeometryPoints(go))
            {
                Vector3 local = root.InverseTransformPoint(worldPoint);
                if (!has) { b = new Bounds(local, Vector3.zero); has = true; }
                else b.Encapsulate(local);
            }

            localBounds = b;
            return has;
        }

        // Bounds samo dijela geometrije iznad y praga (0-1, normalizirano po visini
        // ukupnih boundsa) — npr. glava robota bez ruku koje strše naprijed i koje
        // bi min.z cijelog modela odvukle daleko od lica (GasMaskVisual).
        internal static bool TryGetLocalBoundsAbove(GameObject go, float minYNormalized, out Bounds localBounds)
        {
            if (!TryGetLocalBounds(go, out Bounds full))
            {
                localBounds = default;
                return false;
            }

            float minY = full.min.y + full.size.y * Mathf.Clamp01(minYNormalized);
            Transform root = go.transform;
            bool has = false;
            Bounds b = default;

            foreach (Vector3 worldPoint in GeometryPoints(go))
            {
                Vector3 local = root.InverseTransformPoint(worldPoint);
                if (local.y < minY) continue;
                if (!has) { b = new Bounds(local, Vector3.zero); has = true; }
                else b.Encapsulate(local);
            }

            localBounds = has ? b : full;
            return true;
        }

        // Jedan BoxCollider na rootu po stvarnim granicama geometrije. Briše postojeće
        // collidere (i po djeci) — collider mora završiti na root objektu jer Interactor
        // traži IInteractable na istom GameObjectu kao pogođeni collider, a i
        // ResourceSpawnManager.IsNearConnectionMarker računa na taj raspored.
        public static void FitBoxColliderToGeometry(GameObject go)
        {
            foreach (var existing in go.GetComponentsInChildren<Collider>())
                Object.Destroy(existing);

            bool measured = TryGetLocalBounds(go, out Bounds local);
            var box = go.AddComponent<BoxCollider>();
            if (measured)
            {
                box.center = local.center;
                box.size = local.size;
            }

            // Collider se u pravilu dodaje NAKON što je transform već pomaknut
            // (Instantiate → GroundToSurface → fit), a autoSyncTransforms je u
            // projektu isključen. Transform BEZ collidera pomak ne vodi kao dirty
            // za fiziku, pa PhysX box zna trajno ostati na pozi prije pomaka —
            // vizual i Unity-side bounds izgledaju ispravno, ali igrač prolazi kroz
            // vizual i sudara se s nevidljivim boxom pored njega (totemi veza).
            // Dodir pozicije objekt eksplicitno označi dirty, sync ga odmah gurne u
            // PhysX — usput novi collider postane vidljiv i same-frame world-gen
            // upitima (IsGroundFree, IsNearConnectionMarker).
            go.transform.position = go.transform.position;
            Physics.SyncTransforms();
        }

        private static Vector3 Corner(int i) => new(
            (i & 1) == 0 ? -1f : 1f,
            (i & 2) == 0 ? -1f : 1f,
            (i & 4) == 0 ? -1f : 1f);

        private static Vector3[] GetReadableVertices(Mesh mesh)
        {
            if (!mesh.isReadable) return null;

            if (!VertexCache.TryGetValue(mesh, out Vector3[] vertices))
            {
                vertices = mesh.vertices;
                VertexCache[mesh] = vertices;
            }

            return vertices;
        }
    }
}
