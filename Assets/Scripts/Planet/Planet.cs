using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [RequireComponent(typeof(Rigidbody))]
    public class Planet : MonoBehaviour
    {
        public PlanetType Type;
        public bool IsHub;
        public float Gravity = 20f;

        // Nestabilni planeti (GDD 4.2): ubrzavaju degradaciju veza, strojevi se češće kvare (tjedan 3).
        public bool IsUnstable => Type == PlanetType.Volcanic || Type == PlanetType.Gaseous;

        [SerializeField] private Material surfaceMaterial;

        void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            // Convex MeshCollider (scena ga tako drži na Hubu) je hull od ≤255 poligona:
            // premošćuje udoline i siječe kroz brda visokopoligonskog planeta, pa igrač
            // i sve što se postavlja raycastom lebdi/tone u odnosu na vidljivu površinu.
            // Kinematic rigidbody smije nositi non-convex MeshCollider, pa fizičku
            // površinu izjednačavamo sa stvarnim mesheom. Runtime umjesto scene edita —
            // editor drži scenu u memoriji pa disk izmjene scene ne prežive.
            if (TryGetComponent(out MeshCollider meshCollider) && meshCollider.convex)
                meshCollider.convex = false;

            if (surfaceMaterial != null)
            {
                Renderer renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    // Kameni planeti (uklj. Hub) dobivaju seamless proceduralni kamen
                    // umjesto venus fotke sa šavom — isto što PlanetCreator radi za
                    // spawnane Mining planete. Autorski UV otoci FBX mesha bi uzorak
                    // rezali na granicama, pa se UV-ovi prvo preračunaju sferno.
                    if (Type == PlanetType.Mining)
                    {
                        SphericalUV.Apply(renderer);
                        renderer.material = RockPlanetTexture.GetMaterial(surfaceMaterial);
                    }
                    else
                    {
                        renderer.material = surfaceMaterial;
                    }
                }
            }
        }

        void Start()
        {
            GameEventBus.RaisePlanetDiscovered(transform);
        }
    }
}
