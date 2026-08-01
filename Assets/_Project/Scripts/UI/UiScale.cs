using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WebOfPlanets
{
    // Globalno povećanje cijelog UI-ja (traženo 1.8.2026.: "sve malo veće za 20%").
    //
    // Zašto ovdje, a ne po skriptama: sav UI se gradi iz koda s fiksnim pikselima
    // (~20 skripti), pa bi ručno skaliranje svake vrijednosti bilo i besmisleno i
    // neodrživo. CanvasScaler skalira cijelo stablo odjednom, pa je jedina izmjena
    // ovdje — promjena faktora mijenja veličinu svega, bez diranja layouta.
    //
    // Scenske canvase hvata bootstrap (isti obrazac kao ostatak projekta, bez
    // editiranja SampleScene.unity); runtime-kreirani canvasi (MainMenuUI,
    // VictoryUI) zovu Apply sami jer je redoslijed dvaju AfterSceneLoad metoda
    // nedefiniran pa ih bootstrap ne bi pouzdano vidio.
    public static class UiScale
    {
        // 1.2 = +20%. Jedino mjesto za podešavanje.
        public const float Factor = 1.2f;

        // ScaleWithScreenSize se skalira dijeljenjem referentne rezolucije, što
        // NIJE idempotentno — drugi poziv bi udvostručio efekt. Zato evidencija
        // već obrađenih scalera.
        private static readonly HashSet<int> _applied = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _applied.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyToSceneCanvases()
        {
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (var scaler in scalers) Apply(scaler);
        }

        public static void Apply(CanvasScaler scaler)
        {
            if (scaler == null) return;
            if (!_applied.Add(scaler.GetInstanceID())) return;

            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                scaler.referenceResolution = scaler.referenceResolution / Factor;
            else
                scaler.scaleFactor *= Factor;
        }
    }
}
