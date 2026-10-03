using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One player's corner card: who they are, how many tokens they have home, and whether the
    /// turn is theirs.
    ///
    /// The avatar ring is strictly the player's house colour (Red, Green, Blue, Yellow).
    /// The active state scales the card to 1.03, lights the card body to cream50, and adds a soft
    /// house-colour outer glow. Inactive cards dim to 82% brightness.
    /// Bot seats always display a BOT tag.
    ///
    /// Also the landing spot for the reaction bubble both match trays fire onto (match-emoji-tray
    /// and match-message-tray share this one slot per SCREEN_PLANS, so an emote and a message from
    /// the same player queue instead of overlapping), and the long-press-to-mute gesture both trays
    /// read from — one mute covers both, per the same spec.
    /// </summary>
    public class MatchPlayerCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform body;
        public Image frame;
        public Image ring;
        public Image portrait;
        public TextMeshProUGUI nameLabel;
        public Image[] tokenPips;
        public Image activeGlow;

        [Header("New V2 HUD Chrome")]
        public GameObject botTag;
        public TextMeshProUGUI levelLabel;
        public TextMeshProUGUI nxPointsLabel;
        public TextMeshProUGUI winPercentLabel;

        [Header("Reaction bubble (emotes + quick messages share this one slot)")]
        public CanvasGroup bubbleGroup;
        public Image bubbleBgImage;
        public Image bubbleEmoteImage;
        public EmoteFlipbook bubbleEmoteFlipbook;
        public TextMeshProUGUI bubbleTextLabel;

        [HideInInspector] public GameManager.PlayerColor house;

        /// <summary>Fired on a long press (0.5s) anywhere on the card — MatchHudView wires this to
        /// its shared mute list. Never invoked for the local human's own card.</summary>
        public Action<GameManager.PlayerColor> onLongPress;

        private float _pressStarted = -1f;
        private const float LongPressSeconds = 0.5f;
        private bool _muted;

        [Header("States")]
        public Color activeFrame = new Color32(232, 185, 38, 255);
        public Color idleFrame = new Color32(206, 190, 160, 255);
        public float activeLift = 3f;

        private bool _active;
        private float _blend;
        private Vector2 _restPosition;
        private bool _captured;
        private int _pips = -1;

        private struct BubbleRequest
        {
            public bool isEmote;
            public Texture2D texture;
            public int frameCount;
            public int contentSize;
            public float fps;
            public string text;
            public float holdSeconds;
        }

        private readonly Queue<BubbleRequest> _bubbleQueue = new Queue<BubbleRequest>();
        private bool _bubbleBusy;

        private void Awake()
        {
            if (body != null && !_captured)
            {
                _restPosition = body.anchoredPosition;
                _captured = true;
            }
        }

        private void Update()
        {
            // Fires as soon as the hold crosses the threshold rather than waiting for release, so
            // muting reads as an immediate response to the press instead of to letting go.
            if (_pressStarted >= 0f && Time.unscaledTime - _pressStarted >= LongPressSeconds)
            {
                _pressStarted = -1f;
                onLongPress?.Invoke(house);
            }
        }

        public void OnPointerDown(PointerEventData eventData) => _pressStarted = Time.unscaledTime;
        public void OnPointerUp(PointerEventData eventData) => _pressStarted = -1f;
        public void OnPointerExit(PointerEventData eventData) => _pressStarted = -1f;

        /// <summary>Dims the card to show muted state persists — cheaper to read at a glance mid-match
        /// than a toast that already faded.</summary>
        public void SetMuted(bool muted) => _muted = muted;
        public bool IsMuted => _muted;

        /// <summary>Queues an animated emote over this card. Renders nothing at all when muted —
        /// not faded, not silent, per match-emoji-tray.json: a muted player's reactions do not
        /// exist for anyone watching.</summary>
        public void ShowEmoteBubble(Texture2D texture, int frameCount, int contentSize, float fps, float holdSeconds)
        {
            if (_muted || texture == null) return;
            _bubbleQueue.Enqueue(new BubbleRequest
            {
                isEmote = true, texture = texture, frameCount = frameCount,
                contentSize = contentSize, fps = fps, holdSeconds = holdSeconds,
            });
            if (!_bubbleBusy) PlayNextBubble();
        }

        /// <summary>Queues a quick-message bubble over this card. Same mute rule as emotes — the
        /// two share one mute list and one bubble slot (match-message-tray.json).</summary>
        public void ShowTextBubble(string text, float holdSeconds)
        {
            if (_muted || string.IsNullOrEmpty(text)) return;
            _bubbleQueue.Enqueue(new BubbleRequest { isEmote = false, text = text, holdSeconds = holdSeconds });
            if (!_bubbleBusy) PlayNextBubble();
        }

        private void PlayNextBubble()
        {
            if (_bubbleQueue.Count == 0 || bubbleGroup == null) { _bubbleBusy = false; return; }
            _bubbleBusy = true;
            var req = _bubbleQueue.Dequeue();

            if (bubbleBgImage != null) bubbleBgImage.enabled = !req.isEmote;
            if (bubbleEmoteImage != null) bubbleEmoteImage.gameObject.SetActive(req.isEmote);
            if (bubbleTextLabel != null) bubbleTextLabel.gameObject.SetActive(!req.isEmote);

            if (req.isEmote && bubbleEmoteFlipbook != null)
            {
                bubbleEmoteFlipbook.texture = req.texture;
                bubbleEmoteFlipbook.frameCount = req.frameCount;
                bubbleEmoteFlipbook.contentSize = req.contentSize;
                bubbleEmoteFlipbook.framesPerSecond = req.fps;
                bubbleEmoteFlipbook.Play();
            }
            else if (!req.isEmote && bubbleTextLabel != null)
            {
                bubbleTextLabel.text = req.text;
            }

            bubbleGroup.gameObject.SetActive(true);
            UITween.Fade(bubbleGroup, 1f, 0.15f, () =>
            {
                UITween.Run(req.holdSeconds, UITween.Linear, _ => { }, () =>
                {
                    UITween.Fade(bubbleGroup, 0f, 0.2f, () =>
                    {
                        bubbleGroup.gameObject.SetActive(false);
                        PlayNextBubble();
                    });
                });
            });
        }

        [Header("Appearance")]
        public bool tintRingWithHouseColor = false;

        /// <summary>Configures the card for the assigned house and bot identity.</summary>
        public void ApplyHouse(GameManager.PlayerColor c, bool isBot)
        {
            house = c;
            if (ring != null) ring.color = tintRingWithHouseColor ? Colour() : Color.white;
            if (botTag != null) botTag.SetActive(isBot);
        }

        public void SetActive(bool active, float pulseSpeed)
        {
            _active = active;
            if (!Application.isPlaying)
            {
                _blend = active ? 1f : 0f;
            }
            else
            {
                _blend = Mathf.MoveTowards(_blend, active ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            }

            // Active card lit to cream50 #FBF4E6; inactive dimmed to 82% brightness (bible 5.14)
            Color baseColor = Color.Lerp(new Color(0.82f, 0.82f, 0.82f, 1f), new Color32(251, 244, 230, 255), _blend);
            // Scale rgb only — scaling alpha too would make the card translucent, not dim.
            if (_muted) baseColor = new Color(baseColor.r * 0.6f, baseColor.g * 0.6f, baseColor.b * 0.6f, baseColor.a);
            if (frame != null) frame.color = baseColor;

            if (activeGlow != null)
            {
                activeGlow.gameObject.SetActive(_blend > 0.01f);
                float pulse = _active ? (pulseSpeed > 0f ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * pulseSpeed) : 1f) : 0f;
                Color glowTint = Colour();
                glowTint.a = _blend * pulse * 0.85f;
                activeGlow.color = glowTint;
            }

            if (body != null)
            {
                if (_captured)
                    body.anchoredPosition = _restPosition + new Vector2(0f, _blend * activeLift);
                // Active scale 1.03 (tokens.json motion & MATCH_HUD_V2 spec)
                float s = Mathf.Lerp(1.00f, 1.03f, _blend);
                body.localScale = new Vector3(s, s, 1f);
            }
        }

        /// <summary>Four pips, filled as tokens reach home — the only score that matters in Ludo.</summary>
        public void SetTokensHome(int count)
        {
            if (tokenPips == null || count == _pips) return;
            _pips = count;
            for (int i = 0; i < tokenPips.Length; i++)
            {
                if (tokenPips[i] == null) continue;
                bool filled = i < count;
                tokenPips[i].color = filled ? Colour() : new Color(0.84f, 0.80f, 0.72f, 1f);
            }
        }

        public Color Colour() => house switch
        {
            GameManager.PlayerColor.RED => new Color32(231, 55, 53, 255),      // house.red #E73735
            GameManager.PlayerColor.GREEN => new Color32(77, 174, 81, 255),    // house.green #4DAE51
            GameManager.PlayerColor.YELLOW => new Color32(251, 192, 43, 255),  // house.yellow #FBC02B
            _ => new Color32(27, 137, 231, 255),                               // house.blue #1B89E7
        };
    }
}
