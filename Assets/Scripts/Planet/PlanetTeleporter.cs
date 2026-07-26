using UnityEngine;

namespace WebOfPlanets
{
    // Izvedba teleporta igrača na planet — zove se za veze, teleporter strojeve,
    // respawn i load. PlanetCreator ostaje javna ulazna točka (scena drži tri
    // serijalizirane planetCreator reference pa bi puna selidba bila visok
    // rizik), ali sama odgovornost živi ovdje kao imenovani servis.
    internal static class PlanetTeleporter
    {
        // Vraća novu trenutnu planetu (uvijek targetPlanet); pozivatelj je dužan
        // zapamtiti povratnu vrijednost kao svoje novo stanje.
        public static Transform Teleport(PlayerController player, PlayerCamera playerCamera,
            Transform currentPlanet, Transform targetPlanet, Transform fromPlanet, Transform destinationMarker)
        {
            if (currentPlanet != null)
            {
                if (currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            if (targetPlanet.TryGetComponent(out Attractor newAttractor))
                newAttractor.enabled = true;

            Vector3 playerPos;
            Vector3 playerUp;

            if (destinationMarker != null)
            {
                // Sletimo blizu stvarne pozicije markera/totema (umjesto da ponovno
                // računamo površinu iz centra planeta, što na nesferičnim meshovima
                // zna promašiti stvarnu točku markera).
                Vector3 markerUp = destinationMarker.up;
                Vector3 tangent = Vector3.Cross(markerUp, Vector3.up);
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(markerUp, Vector3.right);
                tangent.Normalize();

                // Marker sa solid colliderom (teleporter gate, totemi veza): sleti duž
                // forward osi tik uz rub collidera — izvan njega (inače fizika izbaci
                // igrača), ali ne dalje nego što je nužno. Za eventualni trigger marker
                // bez solid collidera ostaje paušalnih 2 m.
                float lateral = 2f;
                if (destinationMarker.TryGetComponent(out Collider markerCollider) && !markerCollider.isTrigger)
                {
                    tangent = destinationMarker.forward;

                    float probe = markerCollider.bounds.extents.magnitude + 2f;
                    Ray edgeRay = new Ray(destinationMarker.position + markerUp * 1f + tangent * probe, -tangent);
                    lateral = markerCollider.Raycast(edgeRay, out RaycastHit edgeHit, probe)
                        ? (probe - edgeHit.distance) + 1.5f
                        : markerCollider.bounds.extents.magnitude + 1.5f;
                }

                Vector3 rayOrigin = destinationMarker.position + markerUp * 2f + tangent * lateral;
                if (Physics.Raycast(rayOrigin, -markerUp, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // +2, ne +1: dno kapsule je 1.69 ISPOD pivota, pa bi +1 kapsulu
                    // startno ukopao 0.69 u pogođenu površinu. Zraka pogađa SVE solid
                    // collidere — na niskom objektu (npr. pickup kamen, krov ~0.6) bi
                    // se surface-lock uhvatio u penetraciji i zaglavio u deadlocku sa
                    // PhysX depenetracijom. S +2 stopala kreću ~0.3 iznad pogođenog:
                    // meki pad na tlo, a niski objekt ostaje "krov" (negrounded) s
                    // kojeg igrač normalno siđe. Isto radi i sibling T-spawn (+2).
                    playerPos = hit.point + hit.normal * 2f;
                    playerUp = hit.normal;
                }
                else
                {
                    playerPos = destinationMarker.position + tangent * lateral;
                    playerUp = markerUp;
                }
            }
            else
            {
                // localScale laže za mesh planete (Hub: localScale 1000, stvarni radijus
                // ~19) — fallback bi igrača ostavio stotine jedinica iznad površine.
                float radius = SurfacePlacement.GetPlanetRadius(targetPlanet);

                Vector3 surfaceNormal = fromPlanet != null
                    ? (fromPlanet.position - targetPlanet.position).normalized
                    : Random.onUnitSphere;

                Vector3 tangent = Vector3.Cross(surfaceNormal, Vector3.up);
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(surfaceNormal, Vector3.right);
                tangent.Normalize();

                float lateralOffset = Mathf.Min(6f, radius * 0.5f);
                Vector3 aimDirection = (surfaceNormal * radius + tangent * lateralOffset).normalized;

                Vector3 rayOrigin = targetPlanet.position + aimDirection * (radius * 1.5f);
                if (Physics.Raycast(rayOrigin, -aimDirection, out RaycastHit hit, radius * 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // +2 iz istog razloga kao marker-grana gore (dno kapsule -1.69).
                    playerPos = hit.point + hit.normal * 2f;
                    playerUp = hit.normal;
                }
                else
                {
                    playerPos = targetPlanet.position + aimDirection * (radius + 2f);
                    playerUp = aimDirection;
                }
            }

            Quaternion playerRot = Quaternion.FromToRotation(Vector3.up, playerUp);

            Rigidbody playerRb = player.rig;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = playerPos;
            playerRb.rotation = playerRot;

            player.SetPlanet(targetPlanet);
            if (playerCamera != null) playerCamera.SetPlanet(targetPlanet);

            // Centralna točka svih teleporta (veze, strojevi, respawn); trenutni
            // subscriber (AudioManager) treba samo činjenicu teleporta.
            GameEventBus.Raise(new PlayerTeleportEvent { FromPlanet = fromPlanet, ToPlanet = targetPlanet });

            return targetPlanet;
        }
    }
}
