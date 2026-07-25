using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    // Sve tipke IZVAN Input Actions asseta (on pokriva samo Movement/Jump/
    // MouseLook) na jednom mjestu, zajedno s prikaznim imenima za CONTROLS
    // ekran i hint stringove. Prije su tipke bile hardkodirane u 14+ skripti,
    // a imena tipki su TREĆI put živjela u MainMenuUI CONTROLS tekstu — tri
    // ručno sinkronizirana izvora istine. Puna migracija na Input Actions je
    // svjesno odgođena (dira .inputactions asset; vidi PLAN-KOD §3).
    public static class GameKeys
    {
        public const Key Interact         = Key.E;
        public const Key Inventory        = Key.I;
        public const Key ItemInfo         = Key.Q;
        public const Key CameraReset      = Key.C;
        public const Key PickupMachine    = Key.X;   // i odustajanje od dvosmjernog teleportera
        public const Key PlaceMachine     = Key.P;
        public const Key Respawn          = Key.R;
        public const Key TestKill         = Key.K;   // samo TestingMode
        public const Key DebugSpawnPlanet = Key.T;   // debug/stress alat, nije core loop
        public const Key Cancel           = Key.Escape;
        // Hotbar 1-9 ostaje u QuickSlotInventory.DigitKeyPressed (sistematičan raspon).

        public const string InteractName      = "E";
        public const string InventoryName     = "I";
        public const string ItemInfoName      = "Q";
        public const string CameraResetName   = "C";
        public const string PickupMachineName = "X";
        public const string PlaceMachineName  = "P";
        public const string RespawnName       = "R";
        public const string CancelName        = "ESC";

        // Null-safe provjere — Keyboard.current je null bez tipkovnice.
        public static bool WasPressed(Key key)
            => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

        public static bool IsPressed(Key key)
            => Keyboard.current != null && Keyboard.current[key].isPressed;
    }
}
