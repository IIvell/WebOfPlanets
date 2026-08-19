---
tags: [fizika, moj-projekt]
---

# Kretanje igrača (PlayerController)

Dio [[Gravitacija i kretanje|Gravitacije i kretanja]]. Datoteka: `Player/PlayerController.cs` (~300 linija). Input dolazi iz generiranih `PlayerInputActions` ([[Input System u mom projektu]]).

## FixedUpdate — tri koraka, fiksni redoslijed

1. **`ApplyGravity`** — sila prema centru planeta: `AddForce(down * planet.Gravity, ForceMode.Acceleration)` (default `Gravity = 20`). `down` je radijala prema centru, ne globalno "dolje".
2. **`Move`** — čitanje `Movement` inputa i postavljanje brzine.
3. **`EnforceSurfaceLock`** — vidi [[Surface lock]].

## Ključne odluke

- **Kretanje ide kroz `linearVelocity`, ne `MovePosition`** — `MovePosition` je na dinamičkom rigidbodyju teleport koji svaki fizički korak resetira interpolaciju → trzaji. Umjesto toga: horizontalna brzina se prepisuje svaki tick (`moveDir * moveSpeed`, default 3), a vertikalna komponenta se sačuva (pad se ne prekida).
- **`linearDamping` je uvijek 0** — fizički damping bi gušio i horizontalno kretanje. "Meki pad" (za teleport/respawn koji spuštaju igrača ~1 m iznad tla) reproducira se **ručno samo na vertikalnoj komponenti**, istom formulom kojom PhysX primjenjuje damping.
- **Kapsula ima frictionless physics material** (trenje 0, `Minimum` combine) — PhysX trenje na kontaktu s mesh planetom inače troši brzinu, najjače na rubovima faceta poligonalne kugle, pa je klizanje po ledu bilo mjestimično sporije. Običan hod ne ovisi o trenju (brzina se ionako prepisuje), a stajanje čuva kod.
- **Vizual je odvojen od fizike**: kapsula/Player se ne rotira prema smjeru kretanja, samo se model robota (`visualModel`) slerpa prema njemu — i to u `Update` (render framerate), ne u `FixedUpdate` (50 Hz), inače rotacija štuca na monitorima s višim refreshom.
- **`AlignColliderWithVisual`** (u `Start`) — mesh robota je u sceni stajao ~1,3 pomaknut od pivota pa je pri okretanju *kružio* oko pivota; vizual i fizika razilazili su se do ~2,5 jedinice. Rješenje: izmjeri stvarnu geometriju ([[Prizemljenje na sferu (SurfacePlacement)|SurfacePlacement]] boundsi), centriraj je na pivot i postavi kapsulu na pivot — bez ručnog štimanja scene.

## Skoka nema

`Jump` akcija postoji u `.inputactions` assetu, ali je **nijedna skripta ne koristi** — igrač samo hoda. To je i pretpostavka [[Surface lock|surface locka]] (višak visine uvijek je prekršaj, ne skok).

## Moguća potpitanja

- *„Zašto ne CharacterController?"* → radi s globalnim up smjerom; na kugli treba rigidbody + vlastita gravitacija i orijentacija ([[Sferna gravitacija (Attractor)]]).
- *„Zašto su serijalizirana polja preimenovana bez `FormerlySerializedAs`?"* → namjerno: stare vrijednosti iz scene (`iceAcceleration=25`/`iceDeceleration=0.3`) pregazile bi code defaulte — vidi [[Klizanje po ledu]]. Scene-serijalizirane vrijednosti **uvijek pobjeđuju code defaulte**.
