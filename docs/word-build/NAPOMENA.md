# NAPOMENA — NE REBUILDATI docx iz template/!

`docs/Zavrsni-rad.docx` se od 1.8.2026. uređuje **izravno u Wordu** (naslovnica,
slika 1, ostale ručne izmjene). Mapa `template/` je zastarjeli izvor iz prvotne
izgradnje i **ne smije se više zipati preko docx-a** — to briše korisnikove izmjene
(dogodilo se 2.8.2026., vraćeno iz `Zavrsni-rad-backup-2026-08-02.docx`).

Nove slike/izmjene umetati kirurški: raspakirati docx (`current-docx/` je zadnja
takva radna kopija), izmijeniti `word/document.xml` + `_rels` + `media/`, ponovno
zipati. Word je prenumerirao relacije u `rId1..rIdN`; nove dodavati s jedinstvenim
imenima (npr. `rIdSlika2`).
