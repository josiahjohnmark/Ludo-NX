using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LudoTurnManager : MonoBehaviour
{
    [Header("3D Dice References")]
    [SerializeField] private Transform dice1Transform;
    [SerializeField] private Transform dice2Transform;

    [Header("UI Controls")]
    [SerializeField] private Button rollButton;

    [Header("Player Turn System")]
    public int currentTurnIndex = 0; // 0=Red, 1=Green, 2=Yellow, 3=Blue
    [SerializeField] private GameObject[] playerProfileCards; // Drag 4 player cards here

    private bool isRolling = false;

    private void Start()
    {
        // Wire up roll button listener automatically
        if (rollButton != null)
        {
            rollButton.onClick.AddListener(RollDice);
        }

        HighlightCurrentPlayer();
    }

    /// <summary>
    /// Triggered when the ROLL button is tapped
    /// </summary>
    public void RollDice()
    {
        if (isRolling) return;
        StartCoroutine(RollDiceRoutine());
    }

    private IEnumerator RollDiceRoutine()
    {
        isRolling = true;
        if (rollButton != null) rollButton.interactable = false;

        // 🎲 1. Generate 2 random Ludo dice outcomes (1 to 6)
        int roll1 = Random.Range(1, 7);
        int roll2 = Random.Range(1, 7);

        // 🎲 2. Visual tumble/spin animation for your Blender dice models
        float duration = 0.7f;
        float elapsed = 0f;

        Vector3 startPos1 = dice1Transform.position;
        Vector3 startPos2 = dice2Transform.position;

        while (elapsed < duration)
        {
            // Rapidly rotate 3D models in mid-air
            if (dice1Transform != null)
            {
                dice1Transform.Rotate(new Vector3(25, 35, 15) * Time.deltaTime * 30f);
            }
            if (dice2Transform != null)
            {
                dice2Transform.Rotate(new Vector3(15, 45, 20) * Time.deltaTime * 30f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 🎲 3. Snap dice to face angles representing the rolled values
        ApplyDiceRotation(dice1Transform, roll1);
        ApplyDiceRotation(dice2Transform, roll2);

        Debug.Log($"---> PLAYER {currentTurnIndex + 1} ROLLED: [{roll1}] and [{roll2}] | TOTAL: {roll1 + roll2} <---");

        // 🎯 4. Trigger token predictions/highlights on the board
        ShowMovePredictions(currentTurnIndex, roll1, roll2);

        isRolling = false;
        if (rollButton != null) rollButton.interactable = true;
    }

    private void ApplyDiceRotation(Transform diceTransform, int value)
    {
        if (diceTransform == null) return;

        // Standard rotation angles for 6 faces (adjust offset if blender model face order differs)
        switch (value)
        {
            case 1: diceTransform.localEulerAngles = new Vector3(0, 0, 0); break;
            case 2: diceTransform.localEulerAngles = new Vector3(90, 0, 0); break;
            case 3: diceTransform.localEulerAngles = new Vector3(0, 0, -90); break;
            case 4: diceTransform.localEulerAngles = new Vector3(0, 0, 90); break;
            case 5: diceTransform.localEulerAngles = new Vector3(-90, 0, 0); break;
            case 6: diceTransform.localEulerAngles = new Vector3(180, 0, 0); break;
        }
    }

    public void HighlightCurrentPlayer()
    {
        for (int i = 0; i < playerProfileCards.Length; i++)
        {
            if (playerProfileCards[i] == null) continue;

            // Slightly scale up active player card
            if (i == currentTurnIndex)
                playerProfileCards[i].transform.localScale = new Vector3(1.08f, 1.08f, 1f);
            else
                playerProfileCards[i].transform.localScale = Vector3.one;
        }
    }

    private void ShowMovePredictions(int playerIndex, int val1, int val2)
    {
        // Logic for highlighting board step paths/tokens goes here!
        Debug.Log($"Showing valid board moves for Player {playerIndex + 1}");
    }
}