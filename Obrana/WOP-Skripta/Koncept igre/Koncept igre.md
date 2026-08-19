---
tags: [dizajn, moj-projekt]
---

# Koncept igre

**Web of Planets** — single-player 3D igra preživljavanja i automatizacije u svemiru. Igrač hoda po malim **sfernim, proceduralno generiranim planetima**, ručno rudari resurse, izrađuje strojeve i povezuje planete u **mrežu veza koje s vremenom propadaju**.

## Središnja ideja: mreža kao igra

- Temelj je **teorija grafova** (GDD §1): planeti su **čvorovi (nodes)**, veze **bridovi (edges)**. Igrač ne upravlja samo jednim planetom, nego topologijom, zdravljem i protokom resursa cijele mreže.
- Mreža **nije statična** — veze degradiraju s vremenom, brže uz nestabilne (vulkanske/plinske) planete. Održavanje mreže je stalni trošak i izvor napetosti.
- Otud i naslov: *Web* of Planets — mreža je srž, planeti su njezini čvorovi.

## Ostali stupovi koncepta

- **Sferna gravitacija** — kretanje po površini kugle, inspirirano *Super Mario Galaxy* pristupom, u pojednostavljenom obliku.
- **Proceduralna generacija** — 5 tipova planeta u implementaciji (Mining, Organic, Ice, Volcanic, Gaseous), svaki s vlastitim resursima i opasnostima.
- **Progresija kroz Hub** — početni planet s Glavnim Računalom; ulaganjem resursa otključavaju se novi recepti i na kraju pobjeda.

## Povezano

- [[Žanr]] — hibrid: [[Survival žanr]] + [[Factory žanr]]
- [[Osnovna petlja igre]] — kako se koncept pretvara u minutu-po-minutu gameplay

## Moguća potpitanja

- *„Opišite igru u dvije rečenice."* → prva dva odlomka gore.
- *„Što igru razlikuje od sličnih igara?"* → veze koje propadaju: mreža se ne samo gradi, nego i održava; strategija topologije (kraće veze, redundantnost).
- *„Odakle inspiracija?"* → Super Mario Galaxy (gravitacija), Factorio/Satisfactory (automatizacija), teorija grafova (struktura).
