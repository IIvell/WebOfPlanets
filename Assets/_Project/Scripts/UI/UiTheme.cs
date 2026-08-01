using UnityEngine;
using UnityEngine.UI;

namespace WebOfPlanets
{
    // Vizualna tema UI-ja — SunGraphica "Game UI collection FREE version" (itch.io).
    //
    // Zašto ovako: sav UI se gradi iz koda (runtime bootstrap obrazac), pa se
    // sprite-ovi učitavaju po imenu iz Resources/UISprites — pripremljenih Editor
    // alatom "Tools/Web of Planets/Uvezi UI sprite-ove" (Assets/Editor/UiSpriteSetup.cs).
    // Sve metode su null-safe: ako sprite-ovi nisu uvezeni, ne diraju ništa i UI
    // ostaje na starim jednobojnim pločama. Time nijedna postojeća UI skripta ne
    // ovisi o asset packu (mentorova uputa 31.7.2026.: samo vizualna dorada, bez
    // izbacivanja/lomljenja postojećih skripti).
    //
    // Imena sprite-ova su stabilna i bez boje (ui_frame, ne ui_frame_blue) —
    // promjena sheme boja znači samo ponovni uvoz, bez diranja koda. Nikad ih ne
    // preimenovati (isti razlog kao Machines/Resources — lookup po imenu).
    public static class UiTheme
    {
        private static bool _tried;
        private static Sprite _panel, _panelTall, _frame, _frameWarn, _slot, _barFill, _accent;

        // Statics preživljavaju domain reload — isti reset obrazac kao drugdje.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _tried = false;
            _panel = _panelTall = _frame = _frameWarn = _slot = _barFill = _accent = null;
        }

        private static void EnsureLoaded()
        {
            if (_tried) return;
            _tried = true;
            _panel     = Resources.Load<Sprite>("UISprites/ui_panel");
            _panelTall = Resources.Load<Sprite>("UISprites/ui_panel_tall");
            _frame     = Resources.Load<Sprite>("UISprites/ui_frame");
            _frameWarn = Resources.Load<Sprite>("UISprites/ui_frame_warn");
            _slot      = Resources.Load<Sprite>("UISprites/ui_slot");
            _barFill   = Resources.Load<Sprite>("UISprites/ui_bar_fill");
            _accent    = Resources.Load<Sprite>("UISprites/ui_accent");
        }

        // Je li tema dostupna — za odabir tinta koji rade i sa spriteom i bez njega.
        public static bool HasTheme
        {
            get { EnsureLoaded(); return _frame != null; }
        }

        // Vrati themed boju ako je tema učitana, inače stari fallback — da izgled
        // bez uvezenih sprite-ova ostane identičan dosadašnjem.
        public static Color Tint(Color themed, Color fallback) => HasTheme ? themed : fallback;

        // ── Stilovi ───────────────────────────────────────────────────────────
        // Sve metode: bez sprite-a ne diraju Image (ni boju), s spriteom postave
        // sprite + type + bijeli tint (sprite nosi vlastite boje).

        // Sliced sprite s automatskim skaliranjem ruba: ako je zbroj bordera veći
        // od ~70% dimenzije elementa, slice bi se urušio (Unity ga degenerira),
        // pa se pixelsPerUnitMultiplier povećava dok rub ne stane. Dimenzije se
        // čitaju iz sizeDelta — pouzdano za centar-sidrene panele; stretch osi
        // (sizeDelta <= 0) se preskaču i vrijedi baseMult.
        private static void ApplySliced(Image img, Sprite sprite, float borderSumX, float borderSumY, float baseMult)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;

            float mult = baseMult;
            Vector2 size = img.rectTransform.sizeDelta;
            if (size.x > 0f) mult = Mathf.Max(mult, borderSumX / (0.7f * size.x));
            if (size.y > 0f) mult = Mathf.Max(mult, borderSumY / (0.7f * size.y));
            img.pixelsPerUnitMultiplier = mult;
        }

        // Veliki prozor/panel (crafting, meniji, računalo...). Border 240/240 L-R,
        // 170/170 T-B (postavlja UiSpriteSetup).
        public static void StylePanel(Image img)
        {
            EnsureLoaded();
            if (img == null || _panel == null) return;
            ApplySliced(img, _panel, 480f, 340f, 1f);
            img.color = Color.white;
        }

        // Visoki uski panel (liste, bočni prozori). Border 170/170 L-R, 220/220 T-B.
        public static void StylePanelTall(Image img)
        {
            EnsureLoaded();
            if (img == null || _panelTall == null) return;
            ApplySliced(img, _panelTall, 340f, 440f, 1f);
            img.color = Color.white;
        }

        // Prozor liste (hub skladište, inventar, storage). Namjerno koristi isti
        // pravokutni ui_frame kao gumbi, a NE ukrasni ui_panel_tall: taj sprite ima
        // stepenaste ukrase i prozirni rub izvan tamnog tijela (~15% širine slijeva),
        // pa je sadržaj uvučen 16px od RectTransforma ispadao izvan okvira, a 9-slice
        // je rastezao same stepenice. Prijavljeno 1.8.2026. — ne vraćati StylePanelTall
        // ovdje bez uvođenja zasebnog content roota uvučenog na tijelo sprite-a.
        public static void StyleWindow(Image img)
        {
            EnsureLoaded();
            if (img == null || _frame == null) return;
            ApplySliced(img, _frame, 48f, 48f, 1f);
            img.color = Color.white;
        }

        // Unutarnja uvlaka sadržaja koja čisti rub + sjenu ui_frame sprite-a.
        // Bez teme ostaje stara vrijednost da se izgled fallbacka ne mijenja.
        public static float WindowPadding => HasTheme ? 22f : 16f;

        // Gumb ili manji okvir (tamna ploča s plavim rubom). Radi s uGUI Button
        // color-tint tranzicijama (normal/highlighted/pressed množe bijeli tint).
        // Border 24px sa svih strana.
        public static void StyleButton(Image img)
        {
            EnsureLoaded();
            if (img == null || _frame == null) return;
            ApplySliced(img, _frame, 48f, 48f, 1f);
            img.color = Color.white;
        }

        // Žuti okvir za upozorenja (AlertsUI).
        public static void StyleWarning(Image img)
        {
            EnsureLoaded();
            if (img == null || _frameWarn == null) return;
            ApplySliced(img, _frameWarn, 48f, 48f, 1f);
            img.color = Color.white;
        }

        // Kvadratni slot inventara/hotbara (oktagon s dekoracijom). Poziva se bez
        // postavljanja boje — pozivatelj zadržava svoju selekcijsku tint logiku
        // (kroz Tint() da fallback ostane stari).
        public static void StyleSlot(Image img)
        {
            EnsureLoaded();
            if (img == null || _slot == null) return;
            img.sprite = _slot;
            img.type = Image.Type.Simple;
            img.preserveAspect = false; // slotovi su ionako kvadratni
        }

        // Okvir HP/progress trake (mali sliced okvir, tanki rub).
        public static void StyleBarFrame(Image img)
        {
            EnsureLoaded();
            if (img == null || _frame == null) return;
            ApplySliced(img, _frame, 48f, 48f, 1f);
            img.color = Color.white;
        }

        // Punjenje trake — bijeli segmentirani strip, pozivatelj ga tinta svojom
        // bojom (zdravlje, trajnost...) i zadržava svoj Image.Type (Filled ili
        // anchor-širina). Ne dira boju.
        public static void StyleBarFill(Image img)
        {
            EnsureLoaded();
            if (img == null || _barFill == null) return;
            img.sprite = _barFill;
        }

        // Ukrasni kosi akcent (uz naslove panela).
        public static void StyleAccent(Image img)
        {
            EnsureLoaded();
            if (img == null || _accent == null) return;
            img.sprite = _accent;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }
    }
}
