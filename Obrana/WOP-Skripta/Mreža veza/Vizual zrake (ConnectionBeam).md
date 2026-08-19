---
tags: [mreza-veza, moj-projekt]
---

# Vizual zrake (ConnectionBeam)

Dio [[Mreža veza|Mreže veza]]. Vizual žive veze u `PlanetConnection` — dio vizualne dorade iz srpnja 2026. (tema završnog rada).

## Tri segmenta, ne jedan

Zraka se **ne vuče od šiljka do šiljka**: jedan kosi cilindar na totemu izgleda ukošeno. Umjesto toga: uspravni segment uz os svakog totema + kosi spoj tek u visini (`RiseFactor`), s malim preklopom na koljenu da se ne vidi procjep. Sidro zrake je **izmjereni stvarni šiljak geometrije totema** (`TryGetTopPoint`), ne točka na osi — nagnuti modeli imaju šiljak pomaknut od osi pa je osna varijanta kapu ostavljala da visi u zraku.

## Shader i materijal

Custom shader `WebOfPlanets/ConnectionBeam` — animirani "tok energije" umjesto punog Lit cilindra. Tempo po [[Tierovi veza|tieru]]: jača veza = brži i gušći tok; Ancient sporo pulsira. **Jedna instanca materijala za sva tri segmenta**: [[Degradacija veza|health boja]] se piše u jedan materijal (`_BaseColor`), a `OnDestroy` ga čisti točno jednom.

Shader živi u `Art/Shaders/Resources` da ga **player build ne strippa**: `Shader.Find` bez asset-materijala u sceni ne preživi build (isti razlog kao fallback lanac u VfxManageru). Ako ga nema, fallback je puni Lit cilindar u boji — degradacijski gradijent radi i tada. Srodno: [[URP u mom projektu]].

## Sitnice iz koda

- Collider primitivnog cilindra se **odmah `enabled = false` pa `Destroy`** — `Destroy` je odgođen do kraja framea, a `FindClearSurfacePoint` radi `OverlapSphere` isti frame, pa bi "živi-mrtvi" collider otjerao totem s idealne točke.
- Totem prave veze mora osvanuti **točno na pozi potencijalnog totema** (koji je mogao biti bočno pomaknut zbog resursa) — ponovni izračun pozicije bi ga odveo drugdje i igraču bi veza "nestala". Poza se čita i s ugašenog totema (transform ostaje).

## Moguća potpitanja

- *„Zašto ne LineRenderer?"* → treba mi 3D volumen s debljinom po tieru, custom shaderom i colliderom-free geometrijom; skalirani cilindri su jednostavniji i rade s istim materijalom.
