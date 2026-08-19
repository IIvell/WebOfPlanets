---
tags: [svijet, moj-projekt]
---

# Generiranje planeta

Dio [[Proceduralna generacija|Proceduralne generacije]]. Kako nastaje **jedan** planet (`PlanetCreator.CreatePlanetObject`).

## Recept

1. `GameObject.CreatePrimitive(PrimitiveType.Sphere)` — Unityjeva ugrađena sfera, layer `Planet`.
2. Kinematic `Rigidbody` (bez Unity gravitacije — gravitacija je moja, vidi `Attractor`).
3. **Zamjena collidera** (vidi dolje).
4. `Attractor` (isprva ugašen) + `Planet` komponenta s `Gravity` i `Type`.
5. Materijal po tipu iz Inspector referenci + nasumična rotacija za varijaciju.

## Gotcha #1: SphereCollider vs. vidljivi mesh

Primitivna sfera nosi **analitički SphereCollider** (matematički savršena kugla), ali vidljivi mesh je **poligonalna aproksimacija** koja između vrhova pada do **~1,3 % radijusa ispod** te kugle (R=50 → ~0,65 jedinica). Sve što se prizemljuje raycastom sjelo bi na nevidljivu kuglu i **lebdjelo iznad vidljivog tla**.

Rješenje: SphereCollider se **ugasi pa uništi**, doda se non-convex `MeshCollider` sa stvarnim mesheom (dopušteno jer je rigidbody kinematic). Fizička površina = vidljiva površina.

Detalj koji pokazuje razumijevanje enginea: **disable prije Destroy** — `Destroy` je odgođen do kraja framea, a resursi se spawnaju event-lančano **isti frame**; aktivni SphereCollider bi bio bliži pogodak raycasta i sve bi opet lebdjelo.

## Gotcha #2: isti problem na Hub planetu, suprotni smjer

Hub je FBX model čiji je scene collider bio **convex** MeshCollider — hull od ≤255 poligona koji **premošćuje udoline i siječe brda**, pa igrač lebdi/tone. `Planet.Awake` runtime prebaci `convex = false` — runtime umjesto scene edita, jer editor drži scenu u memoriji pa disk izmjene ne prežive ([[Runtime bootstrap pattern]]).

> Ova dva gotcha zajedno su odličan odgovor na *„s kojim ste se problemom najviše mučili?"* — ista klasa problema (fizička ≠ vidljiva površina) u dva suprotna smjera.

## Izgled: asseti umjesto procedure

Teksture planeta su **materijal asseti** (`ThirdParty/PlanetModels`, `PlanetTextures`), ne proceduralne — proceduralni `PlanetTextureUtil.cs` izbačen je u srpnju 2026. (mentorova vizualna dorada). Varijaciju daje **nasumična rotacija sfere** — ista tekstura, drugačiji dojam.

- Mining: jupiter fotka, equirectangular i horizontalno seamless (venus je imala vidljivi šav)
- Gaseous / Organic: teksture iz paketa + rotacija; Organic ima fallback tint ako materijal nije dodijeljen
- Mining na Hub FBX-u: autorski UV otoci bi teksturu rezali → UV-ovi se preračunaju sferno (`SphericalUV.Apply`)

## Moguća potpitanja

- *„Zašto primitivna sfera, a ne modeli planeta?"* → jednake su, savršeno okrugle (predvidljiva fizika i prizemljenje), a izgled rješava tekstura; FBX planeti imaju laž u `localScale` (vidi [[Prizemljenje na sferu (SurfacePlacement)]]).
- *„Zašto je Attractor isprva ugašen?"* → gravitacija planeta se pali kad je relevantna za igrača; 30 aktivnih atraktora odjednom nema smisla.
- *„Što je nestabilan planet?"* → `IsUnstable = Volcanic || Gaseous`; ubrzava degradaciju veza i kvarove strojeva (GDD 4.2).
