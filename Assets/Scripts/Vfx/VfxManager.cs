using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Vizualni feedback za postojeće akcije: iskrice pri kopanju, burst pri
    // teleportu, prašina pri postavljanju stroja. Sve je generirano runtime —
    // ParticleSystemi se konfiguriraju u kodu, tekstura čestice (meki krug) se
    // crta proceduralno, bez asseta i bez izmjena scene (isti samopokretajući
    // Bootstrap obrazac kao MainMenuUI/AudioManager).
    public class VfxManager : MonoBehaviour
    {
        private const float SparkInterval = 0.15f; // MiningProgress dolazi svaki frame

        public static VfxManager Instance { get; private set; }

        private ParticleSystem _sparks;
        private ParticleSystem _teleportBurst;
        private ParticleSystem _placeDust;
        private ParticleSystem _breakSmoke;

        private PlayerController _player;
        private float _nextSparkTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("VfxManager").AddComponent<VfxManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Material mat = CreateParticleMaterial();

            _sparks = CreateSystem("Vfx_MiningSparks", mat,
                lifetime: 0.35f, sizeMin: 0.04f, sizeMax: 0.09f,
                new Color(1f, 0.9f, 0.3f), new Color(1f, 0.5f, 0.1f));

            _teleportBurst = CreateSystem("Vfx_Teleport", mat,
                lifetime: 0.7f, sizeMin: 0.08f, sizeMax: 0.16f,
                new Color(0.4f, 0.9f, 1f), Color.white);

            _placeDust = CreateSystem("Vfx_PlaceDust", mat,
                lifetime: 0.6f, sizeMin: 0.15f, sizeMax: 0.35f,
                new Color(0.6f, 0.55f, 0.5f, 0.8f), new Color(0.45f, 0.4f, 0.35f, 0.8f));

            _breakSmoke = CreateSystem("Vfx_BreakSmoke", mat,
                lifetime: 1.4f, sizeMin: 0.25f, sizeMax: 0.55f,
                new Color(0.25f, 0.22f, 0.2f, 0.85f), new Color(0.1f, 0.1f, 0.1f, 0.85f));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()  => GameEventBus.OnPlayerTeleported += HandleTeleported;
        void OnDisable() => GameEventBus.OnPlayerTeleported -= HandleTeleported;

        // ── Javni pozivi ──────────────────────────────────────────────────────

        // Iskrice na mjestu kopanja; normal = smjer od mete prema igraču.
        // Poziva se svaki frame dok se kopa — throttle drži ritam iskrica.
        public static void PlayMiningSparks(Vector3 pos, Vector3 normal)
        {
            if (Instance == null) return;
            if (Time.time < Instance._nextSparkTime) return;
            Instance._nextSparkTime = Time.time + SparkInterval;

            Vector3 dir = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
            EmitBurst(Instance._sparks, pos, dir, count: 10, speedMin: 2f, speedMax: 4f, spread: 0.5f);
        }

        // Prašina oko baze upravo postavljenog objekta (strojevi, totemi).
        public static void PlayMachinePlaced(Vector3 pos, Vector3 up)
        {
            if (Instance == null) return;

            // Prsten horizontalnih mlazova oko osi "up", blago podignutih.
            Vector3 tangent = Vector3.Cross(up, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(up, Vector3.right);
            tangent.Normalize();

            for (int i = 0; i < 18; i++)
            {
                Vector3 radial = Quaternion.AngleAxis(i * 20f, up) * tangent;
                Vector3 dir = (radial + up * 0.35f).normalized;
                EmitBurst(Instance._placeDust, pos + radial * 0.2f, dir,
                    count: 1, speedMin: 1f, speedMax: 2.5f, spread: 0.15f);
            }
        }

        // Tamni dim pri kvaru stroja — stup uz normalu površine.
        public static void PlayMachineBroken(Vector3 pos, Vector3 up)
        {
            if (Instance == null) return;
            EmitBurst(Instance._breakSmoke, pos, up, count: 25, speedMin: 0.8f, speedMax: 2.5f, spread: 0.5f);
        }

        private void HandleTeleported(PlayerTeleportEvent e)
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;

            Vector3 pos = _player.transform.position;
            Vector3 up = _player.transform.up;

            // Sferni shimmer oko igrača + stup uz njegovu up os.
            EmitBurst(_teleportBurst, pos, Vector3.zero, count: 40, speedMin: 1.5f, speedMax: 4f, spread: 1f);
            EmitBurst(_teleportBurst, pos, up, count: 15, speedMin: 3f, speedMax: 6f, spread: 0.25f);
        }

        // ── Emitiranje ────────────────────────────────────────────────────────

        // Ručne EmitParams brzine umjesto shape modula: jedan sustav pokriva
        // sve smjerove (na sfernim planetama "gore" je uvijek drugačiji).
        // spread 0 = točno uz dir, 1 = puna sfera; dir == zero = puna sfera.
        private static void EmitBurst(ParticleSystem ps, Vector3 pos, Vector3 dir,
            int count, float speedMin, float speedMax, float spread)
        {
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                Vector3 rnd = Random.onUnitSphere;
                Vector3 d = dir == Vector3.zero ? rnd : (dir.normalized + rnd * spread).normalized;

                ep.position = pos + rnd * 0.05f;
                ep.velocity = d * Random.Range(speedMin, speedMax);
                ps.Emit(ep, 1);
            }
        }

        // ── Konstrukcija sustava ──────────────────────────────────────────────

        private ParticleSystem CreateSystem(string name, Material mat, float lifetime,
            float sizeMin, float sizeMax, Color colorA, Color colorB)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;

            var emission = ps.emission;
            emission.enabled = false; // sve ide kroz Emit()

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.35f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return ps;
        }

        // Sprites/Default: alpha blend + vertex boja + tekstura, radi i pod URP-om.
        // Fallback lanac za slučaj da ga build strippa (tada dodati shader u
        // Always Included Shaders u Graphics settings).
        private static Material CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                Debug.LogWarning("[VfxManager] Nijedan particle shader nije nađen — čestice će biti magenta.");
                return null;
            }

            var mat = new Material(shader) { mainTexture = CreateSoftCircleTexture() };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            return mat;
        }

        // Meki bijeli krug (radijalni alpha falloff) — bez njega su čestice kvadrati.
        private static Texture2D CreateSoftCircleTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            float half = (size - 1) * 0.5f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a *= a; // mekši rub
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
