using System.Collections;
using UnityEngine;

namespace LudoNX.VFX
{
    /// <summary>
    /// Tasteful, prominent procedural VFX for core game moments:
    /// 1. Token home arrival (golden sparkle star burst).
    /// 2. Token capture (dynamic comic impact shockwave & sparks).
    /// 3. Match win (celebratory confetti rain in house colors across the full camera).
    /// 4. Token hop landing puff / spark.
    /// 5. Dice landing impact.
    ///
    /// Built entirely with procedural textures and high-visibility particle rendering.
    /// </summary>
    public class LudoVFXManager : MonoBehaviour
    {
        public static LudoVFXManager Instance { get; private set; }

        private static Shader _particleShader;
        private static Texture2D _circleTex;
        private static Texture2D _starTex;
        private static Texture2D _sparkTex;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitResources();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static LudoVFXManager Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("LudoVFXManager");
            Instance = go.AddComponent<LudoVFXManager>();
            DontDestroyOnLoad(go);
            Instance.InitResources();
            return Instance;
        }

        private void InitResources()
        {
            if (_particleShader == null)
            {
                _particleShader = Shader.Find("Particles/Standard Unlit");
                if (_particleShader == null) _particleShader = Shader.Find("Mobile/Particles/Additive");
                if (_particleShader == null) _particleShader = Shader.Find("Mobile/Particles/Alpha Blended");
                if (_particleShader == null) _particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                if (_particleShader == null) _particleShader = Shader.Find("Sprites/Default");
            }

            if (_circleTex == null)
            {
                // 1. Soft circle
                _circleTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = (x - 31.5f) / 31.5f;
                        float dy = (y - 31.5f) / 31.5f;
                        float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                        float a = Mathf.SmoothStep(1f, 0f, d);
                        _circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                _circleTex.Apply();
            }

            if (_starTex == null)
            {
                // 2. Crisp 4-point star/sparkle
                _starTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = Mathf.Abs(x - 31.5f) / 31.5f;
                        float dy = Mathf.Abs(y - 31.5f) / 31.5f;
                        float beam = Mathf.Max(1f - dx * 3.5f, 0f) * Mathf.Max(1f - dy, 0f) +
                                     Mathf.Max(1f - dy * 3.5f, 0f) * Mathf.Max(1f - dx, 0f);
                        float a = Mathf.Clamp01(beam);
                        _starTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                _starTex.Apply();
            }

            if (_sparkTex == null)
            {
                // 3. Crisp diamond spark
                _sparkTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float d = Mathf.Abs((x - 15.5f) / 15.5f) + Mathf.Abs((y - 15.5f) / 15.5f);
                        float a = d <= 1f ? (1f - d * 0.5f) : 0f;
                        _sparkTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                _sparkTex.Apply();
            }
        }

        private Material CreateMaterial(Texture2D tex)
        {
            InitResources();
            var mat = new Material(_particleShader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            return mat;
        }

        // ==================================================================================
        // 1. Token Reaches Home (Golden Star & Sparkle Burst)
        // ==================================================================================

        public void PlayHomeArrival(Vector3 worldPos, Color houseColor)
        {
            Debug.Log($"[LudoVFXManager] PlayHomeArrival at {worldPos}");
            StartCoroutine(HomeArrivalRoutine(worldPos, houseColor));
        }

        private IEnumerator HomeArrivalRoutine(Vector3 worldPos, Color houseColor)
        {
            var go = new GameObject("VFX_HomeArrival");
            go.transform.position = new Vector3(worldPos.x, worldPos.y, -3f);

            var ps = go.AddComponent<ParticleSystem>();
            var ren = go.GetComponent<ParticleSystemRenderer>();
            ren.material = CreateMaterial(_starTex);
            ren.sortingLayerName = "Default";
            ren.sortingOrder = 300;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.90f, 0.35f, 1f), // warm gold
                Color.Lerp(houseColor, Color.white, 0.6f)
            );
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.2f); // soft upward float

            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            ps.Play();
            yield return new WaitForSeconds(1.4f);
            if (go != null) Destroy(go);
        }

        // ==================================================================================
        // 2. Token Capture (Dynamic Impact Ring & Sparks)
        // ==================================================================================

        public void PlayCapture(Vector3 worldPos)
        {
            Debug.Log($"[LudoVFXManager] PlayCapture at {worldPos}");
            StartCoroutine(CaptureRoutine(worldPos));
        }

        private IEnumerator CaptureRoutine(Vector3 worldPos)
        {
            var go = new GameObject("VFX_Capture");
            go.transform.position = new Vector3(worldPos.x, worldPos.y, -3f);

            // Expanding impact shockwave sprite
            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(go.transform, false);
            var sr = ringGo.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_circleTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            sr.color = new Color(1f, 0.95f, 0.60f, 0.95f);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 290;

            // Spark & star burst
            var ps = go.AddComponent<ParticleSystem>();
            var ren = go.GetComponent<ParticleSystemRenderer>();
            ren.material = CreateMaterial(_sparkTex);
            ren.sortingLayerName = "Default";
            ren.sortingOrder = 300;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.65f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.30f, 1f),
                new Color(1f, 0.40f, 0.20f, 1f)
            );

            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            ps.Play();

            // Animate impact ring expansion & fade
            float elapsed = 0f;
            const float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(0.3f, 2.8f, Mathf.Sqrt(t));
                ringGo.transform.localScale = new Vector3(scale, scale, 1f);
                sr.color = new Color(1f, 0.95f, 0.6f, Mathf.Lerp(0.95f, 0f, t * t));
                yield return null;
            }

            yield return new WaitForSeconds(0.6f);
            if (go != null) Destroy(go);
        }

        // ==================================================================================
        // 3. Victory Confetti (Celebratory Shower in House Colors)
        // ==================================================================================

        public void PlayVictoryConfetti()
        {
            Debug.Log("[LudoVFXManager] PlayVictoryConfetti triggered!");
            StartCoroutine(VictoryConfettiRoutine());
        }

        private IEnumerator VictoryConfettiRoutine()
        {
            var go = new GameObject("VFX_VictoryConfetti");
            var cam = Camera.main;
            float topY = 12f;
            float spanW = 16f;
            if (cam != null && cam.orthographic)
            {
                topY = cam.transform.position.y + cam.orthographicSize + 0.5f;
                spanW = cam.orthographicSize * cam.aspect * 2.2f;
                go.transform.position = new Vector3(cam.transform.position.x, topY, -4f);
            }
            else
            {
                go.transform.position = new Vector3(0f, topY, -4f);
            }

            var ps = go.AddComponent<ParticleSystem>();
            var ren = go.GetComponent<ParticleSystemRenderer>();
            ren.material = CreateMaterial(_sparkTex);
            ren.sortingLayerName = "Default";
            ren.sortingOrder = 350;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 4.0f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.65f); // falls gracefully
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

            // Multicolored confetti: Red, Green, Yellow, Blue, Gold
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.22f, 0.20f, 1f), // Red
                new Color(1f, 0.85f, 0.20f, 1f)  // Gold
            );

            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(45f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(spanW, 0.5f, 0.1f);

            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-240f * Mathf.Deg2Rad, 240f * Mathf.Deg2Rad);

            ps.Play();
            yield return new WaitForSeconds(5.5f);
            if (go != null) Destroy(go);
        }

        // ==================================================================================
        // 4. Token Hop Landing (Crisp Dust / Sparkle)
        // ==================================================================================

        public void PlayHopLanding(Vector3 worldPos, Color houseColor)
        {
            StartCoroutine(HopLandingRoutine(worldPos, houseColor));
        }

        private IEnumerator HopLandingRoutine(Vector3 worldPos, Color houseColor)
        {
            var go = new GameObject("VFX_HopLanding");
            go.transform.position = new Vector3(worldPos.x, worldPos.y, -2.5f);

            var ps = go.AddComponent<ParticleSystem>();
            var ren = go.GetComponent<ParticleSystemRenderer>();
            ren.material = CreateMaterial(_circleTex);
            ren.sortingLayerName = "Default";
            ren.sortingOrder = 250;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.85f),
                Color.Lerp(houseColor, Color.white, 0.7f)
            );

            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            ps.Play();
            yield return new WaitForSeconds(0.45f);
            if (go != null) Destroy(go);
        }
    }
}
