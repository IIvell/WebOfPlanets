---
tags: [fizika, moj-projekt]
---

# Kamera (PlayerCamera)

Dio [[Gravitacija i kretanje|Gravitacije i kretanja]]. Datoteka: `Player/PlayerCamera.cs` (~90 linija). Third-person kamera prilagođena kugli.

## Kako radi

- U `LateUpdate` (nakon što se igrač pomaknuo) cilja poziciju `igrač − forward * distance + planetUp * height`, gdje je `planetUp` **radijala od centra planeta** — kamera je uvijek "iznad" u lokalnom smislu kugle.
- Glađenje eksponencijalnim faktorom `1 − e^(−smoothSpeed·dt)` — framerate-neovisno (običan `Lerp(a, b, konst)` bi ovisio o FPS-u).
- Scroll kotačić mijenja `height` (zoom, clamp 4–20) — čita se direktno s `Mouse.current` ([[Input System (novi)]]).

## Kamera nema vlastiti yaw

Kamera uvijek stoji **iza `player.forward`**, a taj se smjer prirodno "zakreće" hodanjem po kugli (rotaciju tijela vodi [[Sferna gravitacija (Attractor)|Attractor]] slerp). Nema miš-orbite oko igrača.

Zato **reset kamere okreće igrača, a ne kameru**: početni world-space forward projicira se na trenutnu tangentnu ravninu planeta (`ProjectOnPlane`) i tijelo se rotira prema njemu. Tipka je u [[Konvencije u kodu|GameKeysu]] (`CameraReset`), ne hardkodirana.

## Moguća potpitanja

- *„Zašto `LateUpdate`?"* → kamera se mora pomaknuti tek nakon što su igrač i fizika gotovi za taj frame, inače kaska jedan korak i trese se.
- *„Što se dogodi pri teleportu?"* → `SetPlanet` postavi kameru trenutno (bez glađenja) na novu poziciju iza igrača, da ne doleti preko pola svijeta.
