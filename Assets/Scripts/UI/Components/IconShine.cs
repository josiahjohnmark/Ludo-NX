using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// A soft light sweep that passes over a row of icons every few seconds, one icon a moment
    /// after the other, so it reads as a single glint travelling along the bar rather than
    /// separate sparkles. Kept brief and narrow: it should be noticed, not watched.
    ///
    /// Each target gets its own instance of <c>Resources/UIShine</c> (LudoNX/UIShine). When
    /// no targets are assigned it picks up the icons of a <see cref="HomeNavBar"/> on the same
    /// object.
    /// </summary>
    public class IconShine : MonoBehaviour
    {
        [Tooltip("Icons to sweep, in the order the light reaches them. Empty = the HomeNavBar icons.")]
        public List<Image> targets = new List<Image>();

        [Header("Timing (seconds)")]
        [Tooltip("Wait after the screen appears before the first sweep.")]
        public float firstDelay = 2f;
        [Tooltip("Time from the start of one pass to the start of the next.")]
        public float interval = 5f;
        [Tooltip("Time the band takes to cross a single icon.")]
        public float sweepDuration = 0.85f;
        [Tooltip("Delay between one icon and the next, which makes the light travel along the bar.")]
        public float stagger = 0.1f;

        [Header("Look")]
        // The icons are already cream, so this sits higher than it sounds: ~0.6 reads as a
        // glint, 0.3 is invisible.
        [Tooltip("How far the band pulls toward the shine colour.")]
        [Range(0f, 1f)] public float intensity = 0.6f;
        [Tooltip("Half-width of the band, as a fraction of the icon.")]
        [Range(0.02f, 0.5f)] public float width = 0.1f;
        [Tooltip("Degrees below horizontal: 0 sweeps straight across, 25 runs top-left to bottom-right.")]
        [Range(-90f, 90f)] public float angle = 25f;
        public Color shineColor = Color.white;

        private static readonly int ShinePosId = Shader.PropertyToID("_ShinePos");
        private static readonly int ShineIntensityId = Shader.PropertyToID("_ShineIntensity");
        private static readonly int ShineWidthId = Shader.PropertyToID("_ShineWidth");
        private static readonly int ShineAngleId = Shader.PropertyToID("_ShineAngle");
        private static readonly int ShineColorId = Shader.PropertyToID("_ShineColor");
        private static readonly int UVRectId = Shader.PropertyToID("_UVRect");

        private readonly List<Material> _materials = new List<Material>();
        private readonly List<Material> _originals = new List<Material>();
        private readonly List<float> _lastPos = new List<float>();
        private float _clock;

        private void Awake()
        {
            if (targets.Count == 0)
            {
                var bar = GetComponent<HomeNavBar>();
                if (bar != null)
                    foreach (var item in bar.items)
                        if (item != null && item.iconImage != null) targets.Add(item.iconImage);
            }
        }

        private void OnEnable()
        {
            var template = Resources.Load<Material>("UIShine");
            if (template == null)
            {
                Debug.LogWarning("[Ludo NX] Resources/UIShine.mat is missing; icon shine disabled.");
                enabled = false;
                return;
            }

            foreach (var img in targets)
            {
                if (img == null) continue;
                var mat = new Material(template) { name = "UIShine (" + img.name + ")" };
                if (img.sprite != null) mat.SetVector(UVRectId, DataUtility.GetOuterUV(img.sprite));
                _originals.Add(img.material);
                img.material = mat;
                _materials.Add(mat);
                _lastPos.Add(float.NaN);
            }

            ApplyLook();
            _clock = -firstDelay; // first pass starts after firstDelay
        }

        private void OnDisable()
        {
            int n = 0;
            foreach (var img in targets)
            {
                if (img == null || n >= _materials.Count) continue;
                if (img.material == _materials[n]) img.material = _originals[n];
                Destroy(_materials[n]);
                n++;
            }
            _materials.Clear();
            _originals.Clear();
            _lastPos.Clear();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _materials.Count > 0) ApplyLook();
        }

        private void ApplyLook()
        {
            foreach (var mat in _materials)
            {
                mat.SetFloat(ShineIntensityId, intensity);
                mat.SetFloat(ShineWidthId, width);
                mat.SetFloat(ShineAngleId, angle);
                mat.SetColor(ShineColorId, shineColor);
            }
        }

        private void Update()
        {
            float period = Mathf.Max(interval, 0.1f);
            _clock = (_clock + Time.unscaledDeltaTime) % period;
            float t = _clock; // time into the current pass (negative while waiting for firstDelay)

            for (int i = 0; i < _materials.Count; i++)
            {
                float p = (t - i * stagger) / Mathf.Max(sweepDuration, 0.01f);
                float pos = (p >= 0f && p <= 1f) ? EaseInOut(p) : -1f;
                if (pos == _lastPos[i]) continue; // idle frames touch nothing
                _lastPos[i] = pos;
                _materials[i].SetFloat(ShinePosId, pos);
            }
        }

        // A gentle start and finish keeps the glint from feeling mechanical.
        private static float EaseInOut(float x) => x * x * (3f - 2f * x);
    }
}
