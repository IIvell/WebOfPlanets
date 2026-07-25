using System.Text;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Dijagnostika za prijave "prolazim kroz totem": za svaki interaktivni objekt
    // ispiše stanje collidera (postoji li, enabled, trigger, layer) i usporedi ga
    // s vizualom (renderer bounds) te provjeri vidi li ga PhysX na tom mjestu
    // (OverlapBox). Totemi veza se ispisuju uvijek, ostali samo kad nešto štrči.
    // Poziva se iz editor menija (Tools/Web of Planets/Audit Colliders) u Play modu.
    public static class ColliderAudit
    {
        private const float CenterOffsetTolerance = 0.5f;

        public static void LogReport()
        {
            var sb = new StringBuilder();
            int total = 0, flagged = 0;

            LogPlayer(sb);

            foreach (var interactable in Object.FindObjectsByType<BaseInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject go = interactable.gameObject;
                total++;

                bool isConnectionTotem = go.name.Contains("ConnectionMarker");
                string report = DescribeObject(go, out bool anomaly);
                if (anomaly) flagged++;

                if (isConnectionTotem || anomaly)
                    sb.AppendLine(report);
            }

            sb.Insert(0, $"ColliderAudit: {total} interaktivnih objekata, {flagged} s anomalijom.\n");
            Debug.Log(sb.ToString());
        }

        private static void LogPlayer(StringBuilder sb)
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                sb.AppendLine("IGRAČ: nije nađen (nema PlayerController).");
                return;
            }

            sb.Append($"IGRAČ '{player.name}' layer={LayerMask.LayerToName(player.gameObject.layer)}");
            var rb = player.GetComponentInChildren<Rigidbody>();
            if (rb != null)
                sb.Append($" | Rigidbody kinematic={rb.isKinematic} detection={rb.collisionDetectionMode}");

            Collider playerCollider = null;
            foreach (var col in player.GetComponentsInChildren<Collider>())
            {
                if (playerCollider == null) playerCollider = col;
                sb.Append($" | {col.GetType().Name} na '{col.name}' enabled={col.enabled} trigger={col.isTrigger} boundsSize={Fmt(col.bounds.size)} boundsCenter={Fmt(col.bounds.center)}");
            }

            // Vizualni robot rotira oko visualModel pivota, a kapsula je fiksna na
            // Playeru — ako mesh nije centriran na pivot rotacije, vizual kruži oko
            // kapsule i pomak ovisi o smjeru gledanja. Zato se ispisuje trenutni
            // pomak centra vizuala od centra kapsule: uz ispravan setup mora ostati
            // malen (<~0.3) u SVIM smjerovima gledanja.
            if (playerCollider != null)
            {
                Bounds visual = default;
                bool hasVisual = false;
                foreach (var r in player.GetComponentsInChildren<Renderer>())
                {
                    if (!hasVisual) { visual = r.bounds; hasVisual = true; }
                    else visual.Encapsulate(r.bounds);
                }
                if (hasVisual)
                {
                    float offset = Vector3.Distance(visual.center, playerCollider.bounds.center);
                    sb.Append($" | vizual: center={Fmt(visual.center)} size={Fmt(visual.size)} POMAK VIZUALA OD KAPSULE {offset:F2}");
                }
            }
            sb.AppendLine();
        }

        private static string DescribeObject(GameObject go, out bool anomaly)
        {
            anomaly = false;
            var sb = new StringBuilder();
            string activePart = go.activeInHierarchy ? "" : " NEAKTIVAN";
            sb.Append($"'{go.name}'{activePart} layer={LayerMask.LayerToName(go.layer)} pos={Fmt(go.transform.position)} scale={Fmt(go.transform.lossyScale)}");

            Bounds visual = default;
            bool hasVisual = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!hasVisual) { visual = r.bounds; hasVisual = true; }
                else visual.Encapsulate(r.bounds);
            }
            sb.Append(hasVisual
                ? $"\n    vizual: center={Fmt(visual.center)} size={Fmt(visual.size)}"
                : "\n    vizual: NEMA RENDERERA");

            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                sb.Append("\n    collider: NEMA NIJEDNOG");
                anomaly = true;
                return sb.ToString();
            }

            bool anySolid = false;
            foreach (var col in colliders)
            {
                sb.Append($"\n    {col.GetType().Name} na '{col.name}' enabled={col.enabled} trigger={col.isTrigger} layer={LayerMask.LayerToName(col.gameObject.layer)}");
                if (col.attachedRigidbody != null)
                    sb.Append($" rb={col.attachedRigidbody.name}(kinematic={col.attachedRigidbody.isKinematic})");

                if (col is BoxCollider box)
                    sb.Append($" boxCenter={Fmt(box.center)} boxSize={Fmt(box.size)}");

                if (col.enabled && go.activeInHierarchy)
                {
                    sb.Append($" worldBounds: center={Fmt(col.bounds.center)} size={Fmt(col.bounds.size)}");
                    if (hasVisual)
                    {
                        float offset = Vector3.Distance(col.bounds.center, visual.center);
                        if (offset > CenterOffsetTolerance)
                        {
                            sb.Append($" POMAKNUT OD VIZUALA {offset:F2}");
                            anomaly = true;
                        }
                    }
                }

                if (col.enabled && !col.isTrigger) anySolid = true;
            }

            if (!anySolid && go.activeInHierarchy)
            {
                sb.Append("\n    NEMA AKTIVNOG SOLID COLLIDERA (samo trigger/disabled) — igrač prolazi kroz objekt");
                anomaly = true;
            }

            // Vidi li PhysX išta od ovog objekta na mjestu vizuala — hvata stale
            // collidere (transform pomaknut, PhysX nije syncan) i krive layere.
            if (go.activeInHierarchy && hasVisual)
            {
                bool physxSees = false;
                foreach (var hit in Physics.OverlapBox(visual.center, visual.extents + Vector3.one * 0.05f,
                             Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    if (hit.transform == go.transform || hit.transform.IsChildOf(go.transform)) { physxSees = true; break; }
                }
                if (!physxSees)
                {
                    sb.Append("\n    PHYSX NE VIDI COLLIDER NA MJESTU VIZUALA (stale pozicija ili collider drugdje)");
                    anomaly = true;
                }

                // Ground-truth proba: raycastovi kroz PhysX scenu izvana prema
                // vizualu. Unity-side col.bounds se računa iz transforma pa NE
                // otkriva stale PhysX pozu (collider dodan nakon pomaka transforma
                // uz isključen autoSyncTransforms) — jedino stvarni PhysX upit
                // pokazuje gdje box fizički stoji.
                if (TryProbePhysXCenter(go, visual, out Vector3 physxCenter))
                {
                    float physxOffset = Vector3.Distance(physxCenter, visual.center);
                    if (physxOffset > CenterOffsetTolerance)
                    {
                        sb.Append($"\n    PHYSX BOX POMAKNUT OD VIZUALA {physxOffset:F2} (physx centar {Fmt(physxCenter)})");
                        anomaly = true;
                    }
                }
            }

            return sb.ToString();
        }

        // Procjena centra stvarnog PhysX oblika: sa 6 strana (world osi) raycast
        // prema centru vizuala i prosjek ulaznih pogodaka po vlastitim colliderima.
        // Za box na pozi vizuala prosjek pada u centar vizuala; pomaknuti box vuče
        // prosjek prema stvarnoj PhysX pozi. Bez ijednog pogotka (pomak veći od
        // dosega ili collider mrtav u PhysX-u) vraća false — to već hvata OverlapBox.
        private static bool TryProbePhysXCenter(GameObject go, Bounds visual, out Vector3 physxCenter)
        {
            Vector3 sum = Vector3.zero;
            int hits = 0;
            float reach = visual.extents.magnitude + 6f;

            foreach (Vector3 dir in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                Vector3 origin = visual.center + dir * reach;
                foreach (var hit in Physics.RaycastAll(origin, -dir, reach * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    if (hit.transform != go.transform && !hit.transform.IsChildOf(go.transform)) continue;
                    sum += hit.point;
                    hits++;
                    break; // jedan vlastiti pogodak po smjeru (box = jedna ulazna ploha)
                }
            }

            physxCenter = hits > 0 ? sum / hits : default;
            return hits > 0;
        }

        private static string Fmt(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    }
}
