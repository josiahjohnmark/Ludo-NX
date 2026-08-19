using System.Collections;
using UnityEngine;

/// <summary>
/// Drives every AI-controlled house off the existing turn phase machine.
///
/// Red is the human player; the other three are played by <see cref="LudoAI"/>. All three share
/// the same evaluator and the same strength, set once per match by the menu through
/// <see cref="LudoAI.CurrentDifficulty"/>. The thinking delay is randomised on top, so turns read
/// as considered rather than instant.
///
/// Acts strictly inside AwaitingRoll and AwaitingSelection. It never touches a turn mid-animation,
/// which is what let two tokens move on one die before the phase machine existed.
/// </summary>
public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    [Header("Thinking Delay")]
    public float minThinkTime = 0.6f;
    public float maxThinkTime = 1.4f;
    [Tooltip("Extra pause after the dice land, before a token is chosen.")]
    public float postRollPause = 0.45f;

    [Header("Debug")]
    public bool logDecisions = true;

    // Guards against acting twice on the same decision point. A decision point is uniquely
    // identified by the turn sequence plus the phase plus the dice still on the table.
    private string handledKey;
    private Coroutine pending;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || pending != null) return;
        if (gm.IsAnyTokenMoving) return;
        if (gm.IsHumanControlled(gm.currentTurnColor)) return;

        string key = $"{gm.TurnSequence}:{gm.currentPhase}:{gm.dice1Value},{gm.dice2Value}";
        if (key == handledKey) return;

        if (gm.currentPhase == GameManager.TurnPhase.AwaitingRoll)
        {
            handledKey = key;
            pending = StartCoroutine(RollRoutine(gm));
        }
        else if (gm.currentPhase == GameManager.TurnPhase.AwaitingSelection)
        {
            handledKey = key;
            pending = StartCoroutine(MoveRoutine(gm));
        }
    }

    private IEnumerator RollRoutine(GameManager gm)
    {
        int sequence = gm.TurnSequence;
        yield return new WaitForSeconds(Random.Range(minThinkTime, maxThinkTime));

        if (gm.TurnSequence == sequence && gm.currentPhase == GameManager.TurnPhase.AwaitingRoll)
        {
            if (logDecisions) Debug.Log($"[AI] {gm.currentTurnColor} rolls.");
            gm.OnRollButtonClicked();
        }

        pending = null;
    }

    private IEnumerator MoveRoutine(GameManager gm)
    {
        int sequence = gm.TurnSequence;
        yield return new WaitForSeconds(postRollPause + Random.Range(minThinkTime, maxThinkTime) * 0.5f);

        // The turn may have moved on while we were thinking.
        if (gm.TurnSequence != sequence || gm.currentPhase != GameManager.TurnPhase.AwaitingSelection)
        {
            pending = null;
            yield break;
        }

        GameManager.PlayerColor color = gm.currentTurnColor;
        LudoAI.MoveOption? choice = LudoAI.PickBestMove(color, gm.dice1Value, gm.dice2Value);

        if (choice == null)
        {
            // Nothing playable. The phase machine normally catches this at roll time, so reaching
            // here means the second die became unusable — hand the turn on rather than stall.
            if (logDecisions) Debug.Log($"[AI] {color} has no legal move — passing.");
            gm.ResetDiceState();
            gm.AdvanceToNextPlayer();
            pending = null;
            yield break;
        }

        LudoAI.MoveOption move = choice.Value;

        if (move.useCombined) gm.SelectCombinedDice();
        else gm.SelectDice(move.dieNumber);

        if (logDecisions)
        {
            Debug.Log($"[AI] {color} plays {move.token.name} {move.steps} step(s) " +
                      $"({(move.useCombined ? "combined" : "die " + move.dieNumber)}), score {move.score:F0}.");
        }

        move.token.MoveSteps(move.steps);
        pending = null;
    }
}
