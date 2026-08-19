using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single move evaluator every AI house shares.
///
/// One brain rather than three: a Ludo decision is stateless — given the board, the dice and a
/// colour, there is one best move. Three separate implementations would be three copies of the
/// same rules-aware scoring that drift apart the moment a rule changes.
///
/// Tuned to play tough: it takes free captures, protects advanced tokens, avoids parking inside
/// an opponent's strike range, and pushes for exact finishes.
///
/// <see cref="CurrentDifficulty"/> sits on top of that, and only decides how often the evaluator's
/// answer is ignored in favour of another legal move. The evaluation itself never weakens.
/// </summary>
public static class LudoAI
{
    public struct MoveOption
    {
        public TokenMovement token;
        public int steps;
        public bool useCombined;   // both dice spent on this one token
        public int dieNumber;      // 1 or 2 when not combined
        public float score;
    }

    // --- scoring weights ---
    private const float FinishToken = 1000f;
    private const float CaptureBase = 300f;
    private const float CapturePerVictimStep = 6f;   // punishing a well-advanced token is worth more
    private const float EnterHomeStretch = 140f;
    private const float EscapeDangerBase = 90f;
    private const float EnterDangerBase = -110f;
    private const float LandOnSafeTile = 55f;
    private const float FormBlockade = 75f;
    private const float LeaveYardScarce = 110f;      // fewer than two tokens active
    private const float LeaveYardPlenty = 45f;
    private const float ProgressPerStep = 1.6f;
    private const float AdvancementBonus = 0.45f;    // prefer moving tokens that are already far

    // With two dice an opponent can reach 1-6 with either die, or 2-12 combined.
    private const int StrikeRange = 12;

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// How the offline opponents play. Set by the menu before a match starts; Hard is the
    /// behaviour the AI had before difficulty existed.
    /// </summary>
    public static Difficulty CurrentDifficulty = Difficulty.Normal;

    /// <summary>
    /// Chance of ignoring the best move and taking a random legal one instead.
    ///
    /// Difficulty is expressed as blunder rate rather than as a weaker scoring function on
    /// purpose: the AI still understands the board at every level, so an easy opponent misses
    /// chances rather than playing nonsense. A weakened evaluator produces moves that look broken,
    /// which reads as a bug to the player instead of as an easy opponent.
    /// </summary>
    private static float BlunderChance(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return 0.55f;
            case Difficulty.Normal: return 0.20f;
            default: return 0f;
        }
    }

    /// <summary>Best legal move for this colour, or null when nothing is playable.</summary>
    public static MoveOption? PickBestMove(GameManager.PlayerColor color, int die1, int die2)
    {
        List<MoveOption> options = EnumerateLegalMoves(color, die1, die2);
        if (options.Count == 0) return null;

        // An easy opponent that never captures feels rigged rather than easy, so the blunder is a
        // different legal move, never a refusal to move.
        if (options.Count > 1 && Random.value < BlunderChance(CurrentDifficulty))
        {
            return options[Random.Range(0, options.Count)];
        }

        MoveOption best = options[0];
        for (int i = 1; i < options.Count; i++)
        {
            if (options[i].score > best.score) best = options[i];
        }
        return best;
    }

    public static List<MoveOption> EnumerateLegalMoves(GameManager.PlayerColor color, int die1, int die2)
    {
        List<MoveOption> options = new List<MoveOption>();
        TokenMovement[] all = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (TokenMovement token in all)
        {
            if (token.tokenColor != color || token.isFinished) continue;

            if (die1 > 0 && TokenMovement.CanTokenMove(token, die1))
                options.Add(Build(token, die1, false, 1, all));

            if (die2 > 0 && TokenMovement.CanTokenMove(token, die2))
                options.Add(Build(token, die2, false, 2, all));

            // A combined sum can never spawn from the yard — that needs a literal 6.
            if (die1 > 0 && die2 > 0 && !token.isHome && TokenMovement.CanTokenMove(token, die1 + die2))
                options.Add(Build(token, die1 + die2, true, 0, all));
        }

        return options;
    }

    private static MoveOption Build(TokenMovement token, int steps, bool combined, int dieNumber, TokenMovement[] all)
    {
        return new MoveOption
        {
            token = token,
            steps = steps,
            useCombined = combined,
            dieNumber = dieNumber,
            score = Score(token, steps, all)
        };
    }

    private static float Score(TokenMovement token, int steps, TokenMovement[] all)
    {
        LudoPath path = token.pathScript != null ? token.pathScript : LudoPath.FindComplete();
        if (path == null) return 0f;

        float score = 0f;

        // --- leaving the yard ---
        if (token.isHome)
        {
            int active = 0;
            foreach (TokenMovement t in all)
            {
                if (t.tokenColor == token.tokenColor && !t.isHome && !t.isFinished) active++;
            }
            score += active < 2 ? LeaveYardScarce : LeaveYardPlenty;

            // Coming out onto a start tile that an enemy is sitting on is a free capture, but the
            // start tiles are safe zones in this ruleset, so there is nothing extra to claim.
            return score;
        }

        int pathLength = path.GetPathLength(token.tokenColor);
        int fromIndex = token.currentTileIndex;
        int toIndex = fromIndex + steps;

        // --- exact finish ---
        if (toIndex == pathLength - 1) return FinishToken;

        int mainTiles = pathLength - 6;
        int fromAbs = path.GetAbsoluteMainIndex(token.tokenColor, fromIndex);
        int toAbs = path.GetAbsoluteMainIndex(token.tokenColor, toIndex);

        // --- reaching the private home stretch ---
        if (toIndex >= mainTiles)
        {
            score += EnterHomeStretch + (toIndex - mainTiles) * 12f;
            return score + toIndex * ProgressPerStep;
        }

        // --- capture ---
        if (toAbs >= 0 && !path.IsAbsoluteTileSafe(toAbs))
        {
            foreach (TokenMovement other in all)
            {
                if (other.tokenColor == token.tokenColor || other.isHome || other.isFinished) continue;
                if (other.pathScript == null) continue;
                if (other.pathScript.GetAbsoluteMainIndex(other.tokenColor, other.currentTileIndex) != toAbs) continue;

                score += CaptureBase + other.stepsTaken * CapturePerVictimStep;
                break;
            }
        }

        // --- danger, before and after ---
        bool wasInDanger = fromAbs >= 0 && !path.IsAbsoluteTileSafe(fromAbs) && IsUnderThreat(fromAbs, token.tokenColor, path, all);
        bool willBeInDanger = toAbs >= 0 && !path.IsAbsoluteTileSafe(toAbs) && IsUnderThreat(toAbs, token.tokenColor, path, all);

        // Rescuing a token that has travelled a long way matters far more than rescuing a fresh one.
        float exposure = 1f + token.stepsTaken * 0.02f;
        if (wasInDanger && !willBeInDanger) score += EscapeDangerBase * exposure;
        if (!wasInDanger && willBeInDanger) score += EnterDangerBase * exposure;

        // --- positional ---
        if (toAbs >= 0 && path.IsAbsoluteTileSafe(toAbs)) score += LandOnSafeTile;

        if (toAbs >= 0 && !path.IsAbsoluteTileSafe(toAbs))
        {
            foreach (TokenMovement mate in all)
            {
                if (mate == token || mate.tokenColor != token.tokenColor) continue;
                if (mate.isHome || mate.isFinished || mate.pathScript == null) continue;
                if (mate.pathScript.GetAbsoluteMainIndex(mate.tokenColor, mate.currentTileIndex) == toAbs)
                {
                    score += FormBlockade;   // two of ours on one tile blocks the whole board
                    break;
                }
            }
        }

        score += steps * ProgressPerStep;
        score += token.stepsTaken * AdvancementBonus;

        return score;
    }

    /// <summary>
    /// True if any enemy token sits 1-12 tiles behind this absolute tile, i.e. could reach it on
    /// a single die or a combined roll next turn.
    /// </summary>
    private static bool IsUnderThreat(int absoluteTile, GameManager.PlayerColor myColor, LudoPath path, TokenMovement[] all)
    {
        int loop = path.mainLoop != null ? path.mainLoop.Count : 52;
        if (loop <= 0) return false;

        foreach (TokenMovement enemy in all)
        {
            if (enemy.tokenColor == myColor || enemy.isHome || enemy.isFinished) continue;
            if (enemy.pathScript == null) continue;

            int enemyAbs = enemy.pathScript.GetAbsoluteMainIndex(enemy.tokenColor, enemy.currentTileIndex);
            if (enemyAbs < 0) continue;   // already in their home stretch, harmless

            int gap = ((absoluteTile - enemyAbs) % loop + loop) % loop;
            if (gap >= 1 && gap <= StrikeRange) return true;
        }

        return false;
    }
}
