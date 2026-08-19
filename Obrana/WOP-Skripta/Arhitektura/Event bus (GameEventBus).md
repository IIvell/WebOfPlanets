---
tags: [arhitektura, moj-projekt]
---

# Event bus (GameEventBus)

Dio [[Arhitektura projekta]]. Datoteka: `Assets/_Project/Scripts/Events/GameEventBus.cs`.

## Što je

Statična klasa s **31 C# eventom** (`public static event Action<T>`) plus `Raise*` helperi. Payloadi su `struct`-ovi u istoj datoteci (`ConnectionEvent`, `ResourceCollectedEvent`, `PlayerTeleportEvent`…).

```csharp
public static event Action<ConnectionEvent> OnConnectionCreated;

// izdavač
GameEventBus.RaiseConnectionCreated(new ConnectionEvent { ... });

// pretplatnik (u OnEnable, odjava u OnDisable!)
GameEventBus.OnConnectionCreated += HandleConnection;
```

Grupe eventa: Player, Resources, Network/Connections, Planets, Hub, Machines, Story.

## Zašto — problem koji rješava

Bez busa svaki sustav mora **držati referencu** na svaki drugi: UI na inventar, audio na strojeve, VFX na veze… To je graf koji eksponencijalno raste i onemogućuje mijenjanje jednog sustava bez diranja ostalih.

S busom je veza **jednosmjerna i anonimna**: izdavač ne zna tko ga sluša, pretplatnik ne zna tko je javio. Novi sustav (npr. statistika) se doda **bez ijedne izmjene u postojećem kodu**.

To je i preduvjet za [[Runtime bootstrap pattern]] — sustavi koji se stvaraju u runtimeu ionako ne mogu imati Inspector reference jedni na druge.

## Ključan primjer u igri

`Planet.Start()` diže **`OnPlanetDiscovered`**. Na taj se event pretplaćuju spawner vulkanskih zona i spawner mobova. Posljedica: pri **loadu igre** planeti se ponovno stvaraju istim kodom, event opet plane, i hazardi/mobovi se sami poslože — [[Save-load sustav]] ne mora znati ništa o njima.

## Rezervirani eventi (dokumentirana odluka)

Dio eventa nema publishera ili subscribera (ancient veze, sekundarni hub, transportne rute, story fragmenti). To je **namjerno dogovoreno sučelje za planirane featuree T5/T6**, zabilježeno u komentaru zaglavlja iz audita 14. 7. 2026.

> Ako me pitaju „ovo je mrtav kod?" → nije, to je deklarirano sučelje; brisanje bi značilo da svaki budući feature opet mijenja bus.

## Rizici

- **Curenje pretplata**: ako se ne odjaviš u `OnDisable`/`OnDestroy`, statični event drži referencu na uništeni objekt → `MissingReferenceException` i curenje memorije.
- **Teže praćenje toka**: ne vidi se iz koda tko reagira; treba tražiti po projektu.

## Moguća potpitanja

- *„Zašto `struct` a ne `class` za payload?"* → vrijednosni tip, nema alokacije na heapu → nema pritiska na GC pri čestim eventima.
- *„Zašto ne UnityEvent?"* → `UnityEvent` se spaja u Inspectoru, što je upravo ono što izbjegavam ([[Runtime bootstrap pattern]]); C# event je tipiziran i provjeren pri kompajliranju.
- *„Nije li statika loša?"* → jest kad drži *stanje*; ovdje drži samo *pretplate*, a resetira se preko `SubsystemRegistration`.
