using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Vizualno sunce: svijetleća kugla postavljena u smjeru IZ kojeg dolazi svjetlo
    // Directional Lighta. Kamera ima far clip 1000, a planeti se protežu tisućama
    // jedinica — zato sunce prati kameru na fiksnoj udaljenosti u smjeru svjetla
    // (izgleda beskonačno daleko, bez paralakse), umjesto fiksne točke u svijetu.
    // Stvara se pri startu bez izmjena scene (RuntimeInitializeOnLoadMethod) —
    // vidi feedback o scene YAML editima.
    public static class SpaceSun
    {
        const float Distance = 900f;   // unutar far clipa (1000)
        const float Diameter = 70f;    // ~4.5° prividne veličine na 900 jedinica

        static readonly Color CoreColor = new Color(1f, 0.95f, 0.8f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            Light sunLight = FindDirectionalLight();
            if (sunLight == null)
            {
                Debug.LogWarning("SpaceSun: nema Directional Lighta u sceni, preskačem.");
                return;
            }

            GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sun.name = "Sunce";
            Object.Destroy(sun.GetComponent<Collider>());
            sun.transform.localScale = Vector3.one * Diameter;

            var renderer = sun.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", CoreColor);
                renderer.material = mat;
            }

            var follow = sun.AddComponent<SunFollow>();
            follow.Init(sunLight.transform);
        }

        static Light FindDirectionalLight()
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) return light;
            }
            return null;
        }
    }

    // Drži sunce na fiksnoj udaljenosti od kamere, suprotno od smjera svjetla.
    // Smjer se čita svaki frame pa sunce prati i eventualnu rotaciju svjetla.
    public class SunFollow : MonoBehaviour
    {
        const float Distance = 900f;

        Transform lightTransform;

        public void Init(Transform light) => lightTransform = light;

        void LateUpdate()
        {
            if (lightTransform == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            transform.position = cam.transform.position - lightTransform.forward * Distance;
        }
    }
}
