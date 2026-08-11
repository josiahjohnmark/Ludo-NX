using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple AI for auto-play when the turn timer expires.
/// Picks the best legal move based on priority:
/// 1. Capture an enemy token
/// 2. Spawn from yard (if rolled a 6)
/// 3. Advance the furthest token along the path
/// 4. Move any legal token
/// </summary>
public class AutoPlayAI : MonoBehaviour
{
    public static AutoPlayAI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Automatically selects a die and moves a token for the given player.
    /// Returns true if a move was executed, false if no legal move exists.
    /// </summary>
    public bool AutoPlayBestMove(GameManager.PlayerColor color, int die1, int die2)
    {
        Debug.Log($"[AutoPlayAI] Auto-playing for {color} with dice [{die1}] and [{die2}]");

        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);
        List<TokenMovement> myTokens = new List<TokenMovement>();

        foreach (var t in allTokens)
        {
            if (t.tokenColor == color && !t.isFinished)
            {
                myTokens.Add(t);
            }
        }

        if (myTokens.Count == 0) return false;

        // Try each available die value
        int[] diceValues = GetAvailableDice(die1, die2);

        foreach (int dieValue in diceValues)
        {
            if (dieValue <= 0) continue;

            // Priority 1: Can we capture an enemy?
            TokenMovement captureToken = FindCaptureMove(myTokens, dieValue);
            if (captureToken != null)
            {
                ExecuteMove(captureToken, dieValue, die1, die2);
                return true;
            }

            // Priority 2: Can we spawn from yard? (requires 6)
            if (dieValue == 6)
            {
                TokenMovement yardToken = FindYardToken(myTokens);
                if (yardToken != null)
                {
                    ExecuteMove(yardToken, dieValue, die1, die2);
                    return true;
                }
            }

            // Priority 3: Advance the furthest token (closest to home)
            TokenMovement furthestToken = FindFurthestToken(myTokens, dieValue);
            if (furthestToken != null)
            {
                ExecuteMove(furthestToken, dieValue, die1, die2);
                return true;
            }

            // Priority 4: Move any legal token
            TokenMovement anyToken = FindAnyLegalToken(myTokens, dieValue);
            if (anyToken != null)
            {
                ExecuteMove(anyToken, dieValue, die1, die2);
                return true;
            }
        }

        Debug.Log($"[AutoPlayAI] No legal moves found for {color}.");
        return false;
    }

    private int[] GetAvailableDice(int die1, int die2)
    {
        // Prefer larger die first for better strategic moves
        if (die1 >= die2)
            return new int[] { die1, die2 };
        else
            return new int[] { die2, die1 };
    }

    /// <summary>
    /// Find a token that can land on an enemy token (capture).
    /// </summary>
    private TokenMovement FindCaptureMove(List<TokenMovement> myTokens, int steps)
    {
        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);

        foreach (var token in myTokens)
        {
            if (!TokenMovement.CanTokenMove(token, steps)) continue;
            if (token.isHome) continue; // Can't capture from yard spawn

            int newIndex = token.currentTileIndex + steps;
            if (token.pathScript == null) continue;

            int newAbsolute = token.pathScript.GetAbsoluteMainIndex(token.tokenColor, newIndex);
            if (newAbsolute < 0) continue; // Home stretch — no capture possible

            // Check if any enemy is on this tile
            foreach (var other in allTokens)
            {
                if (other.tokenColor == token.tokenColor || other.isHome || other.isFinished) continue;
                if (other.pathScript == null) continue;

                int otherAbsolute = other.pathScript.GetAbsoluteMainIndex(other.tokenColor, other.currentTileIndex);
                if (otherAbsolute == newAbsolute && !token.pathScript.IsAbsoluteTileSafe(newAbsolute))
                {
                    Debug.Log($"[AutoPlayAI] Found capture opportunity: {token.tokenColor} can capture {other.tokenColor} at tile {newAbsolute}");
                    return token;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find a token still in the yard/home base.
    /// </summary>
    private TokenMovement FindYardToken(List<TokenMovement> myTokens)
    {
        foreach (var token in myTokens)
        {
            if (token.isHome && TokenMovement.CanTokenMove(token, 6))
            {
                return token;
            }
        }
        return null;
    }

    /// <summary>
    /// Find the token that is furthest along the path (closest to winning).
    /// </summary>
    private TokenMovement FindFurthestToken(List<TokenMovement> myTokens, int steps)
    {
        TokenMovement best = null;
        int highestIndex = -1;

        foreach (var token in myTokens)
        {
            if (token.isHome || !TokenMovement.CanTokenMove(token, steps)) continue;

            if (token.currentTileIndex > highestIndex)
            {
                highestIndex = token.currentTileIndex;
                best = token;
            }
        }

        return best;
    }

    /// <summary>
    /// Find any token that can legally move.
    /// </summary>
    private TokenMovement FindAnyLegalToken(List<TokenMovement> myTokens, int steps)
    {
        foreach (var token in myTokens)
        {
            if (TokenMovement.CanTokenMove(token, steps))
            {
                return token;
            }
        }
        return null;
    }

    /// <summary>
    /// Execute the auto-play move by selecting the correct die and triggering token movement.
    /// </summary>
    private void ExecuteMove(TokenMovement token, int dieValue, int die1, int die2)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // Select the correct die
        if (gm.dice1Value == dieValue && !gm.isDice1Selected)
        {
            gm.SelectDice(1);
        }
        else if (gm.dice2Value == dieValue && !gm.isDice2Selected)
        {
            gm.SelectDice(2);
        }
        else if (gm.dice1Value == dieValue)
        {
            gm.SelectDice(1);
        }
        else if (gm.dice2Value == dieValue)
        {
            gm.SelectDice(2);
        }

        // Trigger the move
        token.MoveSteps(dieValue);

        Debug.Log($"[AutoPlayAI] Auto-moved {token.tokenColor} token using die value {dieValue}");
    }
}
