using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Postavljivo Računalo: E otvara isti ComputerMenuUI kao hub Računalo
    // (NetworkComputerInteractable), pa udaljena baza s Respawn Totemom pokriva
    // ulogu sekundarnog huba bez posebnog sustava.
    public class ComputerMachine : BaseInteractable
    {
        private static readonly Color ComputerColor = new(0.25f, 0.55f, 0.95f);

        [SerializeField] private ComputerMachineData data;

        private Transform _planet;

        public Transform Planet => _planet;
        public ComputerMachineData Data => data;

        public override float HoldTime => 0f;

        public static ComputerMachine Spawn(ComputerMachineData data, Transform planet, Vector3 pos, Quaternion rot)
        {
            // 0.52 = world skala hub Računala u sceni (ista Computer.fbx instanca).
            GameObject go = MachinePlacer.SpawnObject(data != null ? data.prefab : null, pos, rot,
                data != null ? data.displayName : "Network Computer", ComputerColor, scale: 0.52f,
                rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);

            var computer = go.AddComponent<ComputerMachine>();
            computer.Init(data, planet);
            return computer;
        }

        public void Init(ComputerMachineData machineData, Transform planet)
        {
            data = machineData;
            _planet = planet;
        }

        public override void Interact()
        {
            var menu = ComputerMenuUI.Instance;
            if (menu == null)
            {
                Debug.LogWarning("ComputerMachine: ComputerMenuUI nije u sceni.");
                return;
            }

            if (menu.IsOpen)
                menu.Hide();
            else
                menu.Show();
        }
    }
}
