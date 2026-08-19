---
tags: [mreza-veza, moj-projekt]
---

# Tierovi veza

Dio [[Mreža veza|Mreže veza]]. Enum `ConnectionType { Ancient, Weak, Mid, Strong }` (u `GameEventBus.cs`).

| Tier | Trajanje* | Debljina zrake | Cijena |
|---|---|---|---|
| Weak (slaba) | 60 s | 0.4 | najjeftinija |
| Mid (srednja) | 180 s | 0.6 | srednja |
| Strong (jaka) | 600 s | 0.9 | najskuplja |

\* defaulti iz koda; stvarne vrijednosti i cijene su `[SerializeField]` na `ConnectionManageru` u sceni (scena overrida kod). Trade-off za igrača: jeftino pa često obnavljati, ili skupo pa mir.

## Cijena — `ConnectionRequirement`

Par `Item` + količina, serijaliziran **inline u sceni** (obična `[System.Serializable]` klasa, ne ScriptableObject). Više zahtjeva se agregira po itemu pa provjerava/troši kroz `InventorySystem`. Sve cijene idu kroz `GameManager.TestingMode` — u testing modu su besplatne (projektno pravilo, vidi [[Konvencije u kodu]]).

## Ancient

Rezervirani tier za planirane "prastare" veze — [[Event bus (GameEventBus)|bus]] ima rezervirane evente `OnAncientConnectionDiscovered/Activated` (namjerno bez publishera, dokumentirano sučelje za budućnost). U kodu se Ancient već posebno tretira: `ConnectionManager` mu **ne pali potencijalne markere** nakon rušenja, a [[Vizual zrake (ConnectionBeam)|zraka]] mu ima sporo, "prastaro" pulsiranje.

## Moguća potpitanja

- *„Zašto klasa, a ne ScriptableObject za cijenu?"* → cijena je konfiguracija jednog polja na manageru, ne dijeljeni podatak; inline serijalizacija je jednostavnija (konsolidirano iz zasebne datoteke, srpanj 2026.).
- *„Odakle stvarno trajanje veze?"* → osnovni lifespan tiera, skraćen za nestabilne krajeve — vidi [[Degradacija veza]].
