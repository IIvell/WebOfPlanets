---
tags: [mreza-veza, moc]
---

# Mreža veza

Hub za sustav po kojem se igra zove — *Web of Planets*. Igrač povezuje planete u mrežu veza koje **propadaju s vremenom**, pa mreža nije statična infrastruktura nego nešto što se stalno održava. To je srž [[Osnovna petlja igre|osnovne petlje]]: rudari → gradi vezu → veza trune → rudari za novu.

Dvije klase, jasna podjela:

| Klasa | Uloga | Bilješka |
|---|---|---|
| `ConnectionManager` (scena) | servis: gdje veza smije nastati, koliko košta, koliko traje | [[Potencijalne veze (Kruskal)]], [[Tierovi veza]] |
| `PlanetConnection` (runtime, po vezi) | jedna živa veza: zdravlje, vizual, rušenje | [[Degradacija veza]], [[Vizual zrake (ConnectionBeam)]] |

Tok: pri startu se spawnaju **potencijalni totemi** (parovi markera koji pokazuju gdje veza *može* nastati) → igrač na totemu bira tier i plaća resurse → nastaje `PlanetConnection` sa zrakom → veza degradira i na 0 zdravlja se ruši → potencijalni totemi se ponovno upale.

Veza je ujedno i prijevoz: [[Teleportacija i putovanje]]. Sve prolazi kroz [[Event bus (GameEventBus)|GameEventBus]] (`ConnectionCreated/Destroyed/HealthChanged/Critical`), a [[Save-load sustav]] sprema par planeta + tip + zdravlje po vezi.

Datoteke: `Planet/ConnectionManager.cs` (~460 linija) i `Planet/PlanetConnection.cs` (~400 linija).
