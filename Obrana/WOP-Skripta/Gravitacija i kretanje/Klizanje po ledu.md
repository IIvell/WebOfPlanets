---
tags: [fizika, moj-projekt]
---

# Klizanje po ledu

Dio [[Gravitacija i kretanje|Gravitacije i kretanja]]. Poseban režim u `Move()` kad je `planet.Type == Ice` ([[Ledeni planet (Ice)]]): metoda `MoveOnIce` u [[Kretanje igrača (PlayerController)|PlayerControlleru]].

## Ideja

Na običnom tlu brzina se **prepisuje** svaki tick (trenutni start/stop). Na ledu brzina **teži cilju s ograničenim ubrzanjem** (`Vector3.MoveTowards`) — igrač se zalijeće, ne skreće trenutno i klizi do zaustavljanja.

- `rate = moveSpeed / iceAccelTime` dok ima inputa (default 1 s do pune brzine), `moveSpeed / iceStopTime` bez inputa (default 1,5 s do stanja).
- Rate **izveden iz `moveSpeed`**: promjena brzine hoda u inspectoru čuva isti osjećaj leda.
- Konstantna deceleracija = kinetičko trenje; `MoveTowards` (za razliku od lerpa) **garantira potpuni stop**, bez asimptotskog puzanja.
- Donji prag `rate ≥ 0.5`: i uz `moveSpeed = 0` naslijeđena brzina mora otkliziti u stop, ne ostati zamrznuta.
- "Up" za split vertikala/horizontala je **radijala od centra planeta, ne `transform.up`** — poravnanje tijela ([[Sferna gravitacija (Attractor)|Attractor]] slerp) kasni za stvarnom normalom pa bi split krivo rezao.

## Defekt iz kojeg je nastalo (dobra priča za obranu)

Scena je imala serijalizirane `iceAcceleration=25` / `iceDeceleration=0.3` koje su **pregazile code defaulte**: ubrzanje praktički trenutno, a zaustavljanje sa 7 m/s trajalo ~23 s — "beskonačno" klizanje. Popravak: **namjerno nova imena polja** (`iceAccelTime`/`iceStopTime`, bez `FormerlySerializedAs`) da stare scene-vrijednosti prestanu važiti. Komentar u kodu izričito zabranjuje vraćanje starih imena.

Pouka: **preimenovanje serijaliziranog polja tiho resetira vrijednost iz scene** — obično zamka ([[Konvencije u kodu]]), ovdje iskorišteno kao alat.
