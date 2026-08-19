---
tags: [arhitektura, moj-projekt, teorija]
---

# ScriptableObject podaci

Dio [[Arhitektura projekta]].

## Što je ScriptableObject

Unityjev serializabilni objekt koji **ne visi na GameObjectu** i živi kao `.asset` datoteka u projektu. Za razliku od `MonoBehaviour`a:

| MonoBehaviour | ScriptableObject |
|---|---|
| Mora biti na GameObjectu u sceni | Postoji sam za sebe u projektu |
| Ima `Update`, `Start`, fiziku | Nema game loop metoda |
| Instanca po objektu → 100 mobova = 100 kopija podataka | **Jedna kopija koju svi dijele** |

Klasična upotreba: **podaci odvojeni od ponašanja**.

## Kod mene

**51 `.asset` instance** u `Assets/_Project/Data/`: `Machines/`, `Planets/`, `Resources/` (+ `Recipes/`), `Tools/`, `Devices/`.

Hijerarhija tipova:

```
ScriptableObject
├── Item                    (resursi, materijali)
├── CraftingRecipe          (recept + rezultat + cijena)
├── PlanetResourceSettings  (što se spawna na kojem tipu planeta)
└── QuickSlotItem  (abstract — sve što ide u hotbar)
    ├── MachineData ──► SmelterMachineData, TeleporterMachineData, …
    ├── GasMaskData
    └── NetworkMapDeviceData
```

`QuickSlotItem` je lijep primjer za obranu: **apstraktni SO kao polimorfna baza**. Hotbar drži listu `QuickSlotItem`ova i ne zna je li u slotu stroj, maska ili uređaj — svaki podtip sam zna što radi kad ga se aktivira.

## Zašto — argumenti

1. **Balansiranje bez kompajliranja.** Promjena cijene recepta = izmjena `.asset` polja, ne koda.
2. **Ušteda memorije.** Podaci se dijele umjesto da se kopiraju po instanci.
3. **Novi sadržaj bez novog koda.** Novi resurs = novi `.asset`, nula linija C#-a.
4. **Preživljava play mode.** Izmjena SO-a u play modeu ostaje (za razliku od komponente u sceni) — brzo štimanje brojki.

## Veza sa save/loadom

Save **ne serijalizira** SO-ove — sprema samo **ime asseta** i pri loadu ga razriješi po tipu + imenu iz `Resources` (`SaveSystem.Resolve<T>`). Razrješavanje je tipizirano jer se imena ponavljaju (recept „Teleporter" vs. machine data „Teleporter") i keširano jer se poziva u petljama.

> Zato: **preimenovanje asseta u `Resources/` mapi razbija stare saveove.** Vidi [[Struktura mapa i asseta]].

## Moguća potpitanja

- *„Što ako u runtimeu promijenim polje SO-a?"* → mijenja se za sve i **u Editoru ostaje trajno**; runtime stanje se zato drži u MonoBehaviouru, ne u SO-u.
- *„Zašto ne JSON/CSV za podatke?"* → SO daje tipiziranost, Inspector UI, drag&drop reference na druge assete i validaciju pri kompajliranju.
- *„Kako biste ovo dali dizajneru?"* → već je dano: sve brojke su u `Data/`, ne u kodu.
