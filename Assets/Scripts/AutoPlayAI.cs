using UnityEngine;

/// <summary>
/// Timeout auto-play. Previously carried its own priority-list move picker, which was a second,
/// weaker set of Ludo rules living alongside the real one; it now delegates to <see cref="LudoAI"/>
/// so there is a single brain in the project.
///
/// Kept as a component (rather than deleted) because the scene references it, and because the
/// human player still needs something to move for them when their countdown expires.
/// </summary>
public class AutoPlayAI : MonoBehaviour
{
    public static AutoPlayAI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Selects a die and moves a token for the given player. Returns false when nothing is legal,
    /// which tells the caller to pass the turn.
    /// </summary>
    public bool AutoPlayBestMove(GameManager.PlayerColor color, int die1, int die2)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;

        // Never fire into a move that is already running.
        if (gm.IsAnyTokenMoving) return false;

        LudoAI.MoveOption? choice = LudoAI.PickBestMove(color, die1, die2);
        if (choice == null)
        {
            Debug.Log($"[AutoPlayAI] No legal move for {color}.");
            return false;
        }

        LudoAI.MoveOption move = choice.Value;

        if (move.useCombined) gm.SelectCombinedDice();
        else gm.SelectDice(move.dieNumber);

        Debug.Log($"[AutoPlayAI] Timeout — {color} plays {move.token.name} {move.steps} step(s).");
        move.token.MoveSteps(move.steps);
        return true;
    }
}
