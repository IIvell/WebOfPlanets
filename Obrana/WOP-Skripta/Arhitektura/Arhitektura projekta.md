---
tags: [moc, arhitektura, moj-projekt]
---

# Arhitektura projekta

Hub bilješka za sve što se tiče **kako je kod organiziran** u Web of Planets. Za sadržaj igre vidi [[Koncept igre]], za tehnologije [[Unity 6]] i [[URP]].

## Brojke (kolovoz 2026.)

| Stavka                                | Vrijednost                  |
| ------------------------------------- | --------------------------- |
| Runtime skripti (`.cs`)               | 67                          |
| Linija koda                           | ~13 600                     |
| Tipova (class/struct/interface/enum)  | ~138                        |
| ScriptableObject `.asset` instanci    | 51                          |
| Assembly definicija (`.asmdef`)       | **0**                       |
| Scena u buildu                        | **1** (`SampleScene.unity`) |
| Eventa u [[Event bus (GameEventBus)]] | 31                          |

## Teme

- [[Struktura mapa i asseta]] — gdje što živi i zašto
- [[Assembly-CSharp (bez asmdefova)]] — zašto nema `.asmdef` i što to znači
- [[Runtime bootstrap pattern]] — **najvažnija tema**, dominantni obrazac projekta
- [[Event bus (GameEventBus)]] — labavo povezivanje sustava
- [[ScriptableObject podaci]] — data-driven sadržaj
- [[Save-load sustav]] — JSON slot i rebuild u mjestu
- [[Strojevi i MachineFactory]] — kako se dodaje novi stroj
- [[Konvencije u kodu]] — namespace, imenovanje, `[SerializeField]`, `GameKeys`

## Rečenica za obranu

> „Projekt je jedna scena i jedan assembly. Sadržaj je u ScriptableObjectima, sustavi se sami stvaraju u runtimeu preko `[RuntimeInitializeOnLoadMethod]` umjesto da se ručno slažu u sceni, a međusobno komuniciraju preko statične sabirnice događaja. Zbog toga je scena minimalna, a git dijelovi projekta su čitljivi tekstualni fajlovi umjesto YAML-a scene."

## Moguća potpitanja

- *„Zašto samo jedna scena?"* → [[Runtime bootstrap pattern]]
- *„Kako biste ovo skalirali na tim od 5 ljudi?"* → [[Assembly-CSharp (bez asmdefova)]] (asmdefovi + odvojene scene po feature-u)
- *„Gdje je granica između podataka i koda?"* → [[ScriptableObject podaci]]
