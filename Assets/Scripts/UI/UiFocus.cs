using UnityEngine;

namespace WebOfPlanets
{
    // Jedini vlasnik "UI otvoren" protokola: kursor, input igrača/kamere i
    // Interactor na jednom mjestu, s brojačem otvorenih panela. Prije je isti
    // ~6-linijski blok bio kopiran u Show/Hide 11 panela, a "je li UI otvoren"
    // se izvodilo iz Cursor.lockState kao implicitnog globalnog flaga — zadnji
    // panel koji se zatvorio je pobjeđivao, pa su nastajali workaroundovi
    // (VictoryUI je silom zatvarao tuđe panele da ne ostane krivi kursor).
    //
    // Brojač: input se vraća igri tek kad se zatvori i ZADNJI otvoreni panel.
    public static class UiFocus
    {
        private static int _openPanels;
        private static int _lastReleaseFrame = -1;

        // Zamjena za raniji idiom "Cursor.lockState != CursorLockMode.Locked"
        // u gameplay kodu (MachinePlacer, Interactor...).
        public static bool IsAnyPanelOpen => _openPanels > 0;

        // Race unutar istog framea: panel se zatvori na Esc (Release), a MainMenuUI
        // kasnije u ISTOM frameu vidi isti Esc pritisak i IsAnyPanelOpen == false,
        // pa otvori pause menu (prijavljeno za ComputerMenuUI). MainMenuUI zato uz
        // IsAnyPanelOpen provjerava i ovaj flag — Esc koji je zatvorio panel ne
        // smije istovremeno otvoriti pause menu.
        public static bool ReleasedThisFrame => _lastReleaseFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _openPanels = 0;
            _lastReleaseFrame = -1;
        }

        // Panel se otvorio: oslobodi kursor i ugasi gameplay input. Panel
        // prosljeđuje vlastite (serijalizirane) reference — u sceni je jedan
        // igrač/kamera/interactor pa je učinak globalan.
        public static void Acquire(PlayerController playerController, PlayerCamera playerCamera, Interactor interactor)
        {
            _openPanels++;
            Apply(playerController, playerCamera, interactor, uiOpen: true);
        }

        // Panel se zatvorio: input se vraća igri samo ako je ovo bio zadnji
        // otvoreni panel — inače kursor i dalje pripada preostalom panelu.
        public static void Release(PlayerController playerController, PlayerCamera playerCamera, Interactor interactor)
        {
            _lastReleaseFrame = Time.frameCount;
            _openPanels = Mathf.Max(0, _openPanels - 1);
            if (_openPanels > 0) return;

            Apply(playerController, playerCamera, interactor, uiOpen: false);
        }

        private static void Apply(PlayerController playerController, PlayerCamera playerCamera, Interactor interactor, bool uiOpen)
        {
            Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = uiOpen;
            playerController?.SetInputEnabled(!uiOpen);
            playerCamera?.SetInputEnabled(!uiOpen);
            if (interactor != null) interactor.enabled = !uiOpen;
        }
    }
}
