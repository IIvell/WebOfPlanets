---
tags: [fizika, moj-projekt]
---

# Sferna gravitacija (Attractor)

Dio [[Gravitacija i kretanje|Gravitacije i kretanja]]. Datoteka: `Planet/Attractor.cs` (~60 linija).

Unatoč imenu, `Attractor` **ne primjenjuje silu** — samo **orijentira tijelo** tako da mu "up" pokazuje od centra planeta (igrač uvijek stoji uspravno na kugli). Silu padanja primjenjuje [[Kretanje igrača (PlayerController)|PlayerController]] sam.

## Kako radi

- Statični registar `List<Attractor> Attractors` — komponente se dodaju u `OnEnable`, mažu u `OnDisable`.
- U `FixedUpdate` tijelo s `orientToGravity = true` traži od svakog drugog attractora da ga orijentira: radijala `(pozicija tijela − centar planeta)` postaje ciljni "up", pa se rotacija slerpa prema njemu (`Quaternion.Slerp`, faktor `50 * fixedDeltaTime`) kroz `rb.MoveRotation`.
- U `Start` postavlja `useGravity = false` (Unityjeva globalna gravitacija ne valja na kugli) i `FreezeRotation` — fizika ne smije vrtjeti tijelo, rotaciju kontrolira isključivo slerp.

## Dvije uloge, jedan flag

| Uloga | `orientToGravity` | Primjer |
|---|---|---|
| Tijelo koje se orijentira | `true` | igrač |
| Čisti izvor gravitacije | `false` | planeti |

Tooltip u kodu objašnjava zašto planeti moraju imati `false`: igrač je i sam attractor, pa bi inače **rotirao planet** dok hoda po njemu.

## Invarijanta registra

Attractori planeta su ugašeni **osim onog na kojem igrač trenutno jest** ([[Generiranje svijeta (PlanetCreator)|PlanetCreator]]/teleport gase stari i pale novi) — registar tipično sadrži samo igrača + aktivni planet. Bez toga bi igrača istovremeno vuklo/orijentiralo 30 planeta.

## Moguća potpitanja

- *„Zašto slerp, a ne trenutno poravnanje?"* → glatka tranzicija; kod teleporta/prelaska na drugi planet up se naglo mijenja, slerp to omekša.
- *„Zašto `MoveRotation` uz `FreezeRotation`?"* → `FreezeRotation` blokira samo rotaciju iz fizičke simulacije (momente, sudare); `MoveRotation` je eksplicitna kinematska naredba i dalje radi.
