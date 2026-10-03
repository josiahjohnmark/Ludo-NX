using System;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Pick one value from a short list — currently only the language row uses it.
    /// Rows are built on demand from a single template so the modal prefab stays small.
    /// </summary>
    public class ListPickerModal : UIModal
    {
        [Header("Wiring")]
        public TextMeshProUGUI titleLabel;
        public RectTransform rowContainer;
        public ClayButton rowTemplate;

        [Header("PNG-chassis rows (used instead of the Clay template when set)")]
        public UnityEngine.UI.Button plainRowTemplate;
        public Color rowIdleTint = Color.white;
        public Color rowSelectedTint = Color.white;
        public Color labelIdle = Color.black;
        public Color labelSelected = Color.white;

        [Header("Layout, in the container's units (defaults: the Clay popup's 1080 frame)")]
        public float rowHeight = 108f;
        public float rowGap = 14f;
        public float singleColumnWidth = 760f;
        public float twoColumnWidth = 370f;
        public float columnOffset = 195f;

        private Action<string> _onChosen;

        public void Configure(string title, string[] options, string current, Action<string> onChosen)
        {
            if (titleLabel != null) titleLabel.text = title;
            _onChosen = onChosen;

            bool plain = plainRowTemplate != null;
            if ((!plain && rowTemplate == null) || rowContainer == null) return;
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (plain) plainRowTemplate.gameObject.SetActive(false);

            var theme = UITheme.Current;
            float rowH = rowHeight, gap = rowGap;
            // Past six options one column overflows the card, so split into two columns.
            int cols = options.Length > 6 ? 2 : 1;
            int perCol = Mathf.CeilToInt(options.Length / (float)cols);
            float colW = cols == 1 ? singleColumnWidth : twoColumnWidth;
            float top = (perCol - 1) * (rowH + gap) * 0.5f;

            for (int i = 0; i < options.Length; i++)
            {
                string value = options[i];
                bool selected = value == current;
                UnityEngine.UI.Button row;
                if (plain)
                {
                    row = Instantiate(plainRowTemplate, rowContainer);
                    var bg = row.GetComponent<UnityEngine.UI.Image>();
                    if (bg != null) bg.color = selected ? rowSelectedTint : rowIdleTint;
                }
                else
                {
                    var clay = Instantiate(rowTemplate, rowContainer);
                    clay.Clay.color = selected ? theme.secondary : theme.panelHigh;
                    clay.Clay.SetVerticesDirty();
                    row = clay;
                }
                row.gameObject.SetActive(true);
                row.name = "Row_" + value;

                var rt = (RectTransform)row.transform;
                int col = i / perCol, r = i % perCol;
                float x = cols == 1 ? 0f : (col == 0 ? -columnOffset : columnOffset);
                rt.sizeDelta = new Vector2(colW, rowH);
                rt.anchoredPosition = new Vector2(x, top - r * (rowH + gap));

                var label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = value;
                    label.color = plain
                        ? (selected ? labelSelected : labelIdle)
                        : UITheme.Lin(selected ? theme.ink : theme.inkMuted);
                }

                row.onClick.AddListener(() =>
                {
                    Action<string> cb = _onChosen;
                    _onChosen = null;
                    Dismiss();
                    if (cb != null) cb(value);
                });
            }
        }
    }
}
