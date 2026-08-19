---
tags: [fizika, moc]
---

# Gravitacija i kretanje

Hub za sfernu fiziku igrača. Sve se vrti oko jedne ideje: **"dolje" nije globalni smjer, nego smjer prema centru planeta** — različit u svakoj točki kugle.

Podjela odgovornosti (tri odvojena problema, tri rješenja):

| Problem | Rješenje | Bilješka |
|---|---|---|
| Tijelo mora biti *uspravno* na kugli | rotacijski slerp prema radijali | [[Sferna gravitacija (Attractor)]] |
| Tijelo mora *padati* prema planetu | ručna sila u FixedUpdateu | [[Kretanje igrača (PlayerController)]] |
| Tijelo mora *ostati na površini* (ne penjati se po objektima) | raycast-lock na planetov collider | [[Surface lock]] |

Uz to: [[Klizanje po ledu]] (poseban režim kretanja) i [[Kamera (PlayerCamera)]] (praćenje po kugli).

Unityjeva ugrađena gravitacija (`Physics.gravity`) je **globalni vektor prema dolje** — na kuglastim planetima neupotrebljiva, pa je svugdje `useGravity = false` i sila se računa ručno. Srodno: [[Prizemljenje na sferu (SurfacePlacement)]] (ista matematika za statične objekte), [[Tipovi planeta]].
