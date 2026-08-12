using UnityEngine;
using TMPro;

public class WinUIController : MonoBehaviour
{
    [Header("Player Win Probability UI Texts")]
    [SerializeField] private TMP_Text redWinUI;
    [SerializeField] private TMP_Text greenWinUI;
    [SerializeField] private TMP_Text yellowWinUI;
    [SerializeField] private TMP_Text blueWinUI;

    private void Update()
    {
        if (WinCalculator.Instance == null) return;

        // Assign formatted percentage strings to TextMeshPro elements
        if (redWinUI != null) redWinUI.text = WinCalculator.Instance.redWinText;
        if (greenWinUI != null) greenWinUI.text = WinCalculator.Instance.greenWinText;
        if (yellowWinUI != null) yellowWinUI.text = WinCalculator.Instance.yellowWinText;
        if (blueWinUI != null) blueWinUI.text = WinCalculator.Instance.blueWinText;

        // Tint each percentage with its player's brand color, matching PlayerCardUI.
        if (UIManager.Instance != null)
        {
            if (redWinUI != null) redWinUI.color = UIManager.Instance.redColor;
            if (greenWinUI != null) greenWinUI.color = UIManager.Instance.greenColor;
            if (yellowWinUI != null) yellowWinUI.color = UIManager.Instance.yellowColor;
            if (blueWinUI != null) blueWinUI.color = UIManager.Instance.blueColor;
        }
    }
}