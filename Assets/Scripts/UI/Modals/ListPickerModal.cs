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

        private Action<string> _onChosen;

        public void Configure(string title, string[] options, string current, Action<string> onChosen)
        {
            if (titleLabel != null) titleLabel.text = title;
            _onChosen = onChosen;

            if (rowTemplate == null || rowContainer == null) return;
            rowTemplate.gameObject.SetActive(false);

            var theme = UITheme.Current;
            const float rowH = 108f, gap = 14f;
            float top = (options.Length - 1) * (rowH + gap) * 0.5f;

            for (int i = 0; i < options.Length; i++)
            {
                string value = options[i];
                ClayButton row = Instantiate(rowTemplate, rowContainer);
                row.gameObject.SetActive(true);
                row.name = "Row_" + value;

                var rt = (RectTransform)row.transform;
                rt.anchoredPosition = new Vector2(0f, top - i * (rowH + gap));

                bool selected = value == current;
                row.Clay.color = selected ? theme.secondary : theme.panelHigh;
                row.Clay.SetVerticesDirty();

                var label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = value;
                    label.color = UITheme.Lin(selected ? theme.ink : theme.inkMuted);
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
