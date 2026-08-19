---
tags: [tehnologije]
---

# Linear color space

Način na koji engine računa miješanje boja i svjetla.

- **Gamma** — stariji pristup; boje se miješaju u prostoru iskrivljenom za prikaz na monitoru → osvjetljenje ispada fizikalno netočno.
- **Linear** — svjetlo se računa u linearnom prostoru pa se tek na kraju konvertira za prikaz → realističnije i konzistentnije osvjetljenje.

Projekt koristi **Linear** — standard uz [[URP]] na PC-u i Unityjeva preporuka za 3D.

## Odgovor u jednoj rečenici

> „Linear color space znači da se osvjetljenje računa fizikalno ispravno u linearnom prostoru, a konverzija za monitor radi se tek na kraju — uz [[URP]] na PC-u to je standard."
