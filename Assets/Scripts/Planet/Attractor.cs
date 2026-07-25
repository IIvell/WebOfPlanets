using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class Attractor : MonoBehaviour
    {
        public static List<Attractor> Attractors = new();

        [Tooltip("Treba li ovo tijelo samo sebe orijentirati prema gravitaciji drugih attractora. Planeti su čisti izvori gravitacije i ovo mora biti isključeno kod njih, inače ih igrač (kao attractor) rotira dok se kreće po njima.")]
        [SerializeField] private bool orientToGravity = true;
        public bool OrientToGravity { get => orientToGravity; set => orientToGravity = value; }

        private Rigidbody _rigidbody;

        public void Attract(Transform body) => Attract(body, body.GetComponent<Rigidbody>());

        public void Attract(Transform body, Rigidbody rb)
        {
            Vector3 gravityUp = (body.position - transform.position).normalized;
            Vector3 bodyUp = body.up;
            Quaternion targetRotation = Quaternion.FromToRotation(bodyUp, gravityUp) * body.rotation;
            rb.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, 50f * Time.fixedDeltaTime));
        }

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _rigidbody.useGravity = false;
        }

        // Invarijanta: attractori planeta su ugašeni osim onog na kojem igrač
        // trenutno jest (PlanetCreator/teleport gase stari i pale novi), pa
        // registar tipično sadrži samo igrača + aktivni planet.
        void FixedUpdate()
        {
            if (!orientToGravity) return;
            if (_rigidbody == null) return;

            foreach (Attractor attractor in Attractors)
            {
                if (attractor != this)
                    attractor.Attract(transform, _rigidbody);
            }
        }

        void OnEnable()
        {
            Attractors.Add(this);
        }

        void OnDisable()
        {
            Attractors.Remove(this);
        }
    }
}
