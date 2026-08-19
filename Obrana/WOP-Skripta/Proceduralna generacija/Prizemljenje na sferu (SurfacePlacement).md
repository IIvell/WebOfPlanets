---
tags: [svijet, moj-projekt]
---

# Prizemljenje na sferu (SurfacePlacement)

Dio [[Proceduralna generacija|Proceduralne generacije]]. Datoteka: `Planet/SurfacePlacement.cs` (~490 linija, `internal static`). **Zajednička matematika** za sve što sjeda na kuglu: resurse, strojeve, totem markere, igračev surface-lock.

## Problem koji rješava

Na ravnom terenu je „stavi objekt na tlo" trivijalno: `y = 0`. Na kugli:

- „dolje" je **smjer prema centru planeta**, različit u svakoj točki,
- modeli dolaze iz raznih paketa s **različitim pivotima** (centar, dno, bilo gdje),
- vidljiva površina nije matematička kugla (vidi [[Generiranje planeta]]).

## Glavne operacije

- **`GetPlanetRadius`** — jedinstveni izračun: za primitivne sfere isto kao `localScale.x/2`, ali za FBX planete (Hub) `localScale` **laže**, pa se čita iz renderer boundsa.
- **`TryRaycastSurface`** — raycast prema površini koji **prihvaća samo pogodak u planetov vlastiti collider**; inače bi zraka pogodila već spawnani resurs i novi objekt bi sjeo na tuđi krov.
- **`GetSurfacePoint`** — smjer (normala) → točka na stvarnoj površini + normala tla.
- **`GroundToSurface`** — spusti objekt tako da mu **dno stvarne geometrije** sjedne na točku, neovisno o pivotu prefaba. (Prije se korigiralo samo uz poseban flag pa su modeli s drugačijim pivotom lebdjeli ili upadali u planet.)
- **`FitBoxColliderToGeometry`** — box collider po stvarnim vrhovima mesha umjesto default kocke na pivotu.

## Performanse — dva keša (dobar odgovor na pitanje o optimizaciji)

| Keš                                     | Zašto                                                                                                                                                  |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `VertexCache` (mesh → vrhovi)           | `mesh.vertices` **vraća kopiju cijelog polja pri svakom pozivu**; world-gen prizemljuje stotine objekata u jednom frameu → bez keša golemi GC pritisak |
| `RaycastBuffer` (`RaycastNonAlloc`, 64) | igračev surface-lock raycasta **svaki FixedUpdate**; alokacija po pozivu bi bila stalni GC pritisak                                                    |

Rub slučaj koji pokazuje pažnju: ako se buffer od 64 **napuni**, dio pogodaka je možda odbačen (među njima i planetov) → jednokratni fallback na `RaycastAll` čuva korektnost. Statika je sigurna jer Unity physics upiti idu samo s glavne niti; keševi se resetiraju preko `SubsystemRegistration` ([[Runtime bootstrap pattern]]).

## Moguća potpitanja

- *„Zašto raycast, a ne matematika kugle?"* → vidljivi mesh nije savršena kugla ([[Generiranje planeta]]); raycast pogađa stvarnu geometriju.
- *„Što je GC pritisak?"* → česte alokacije pune managed heap → garbage collector se češće budi → štucanje framerate-a. Rješenje: keš + `NonAlloc` API-ji.
- *„Zašto `internal static`, a ne singleton?"* → čista funkcijska knjižnica bez stanja (osim keševa); nema potrebe za objektom u sceni.
