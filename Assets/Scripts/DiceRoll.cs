using UnityEngine;

/// <summary>
/// A single die: shows its rolled face, and lets the player pick it when it is their turn.
/// </summary>
public class DiceRoll : MonoBehaviour
{
    [Header("Selection feedback")]
    [Tooltip("Tint applied to the chosen die. Selection is shown by colour, never by size — " +
             "the two dice must always read as the same size as each other.")]
    public Color selectedTint = new Color(1f, 0.78f, 0.20f, 1f);

    private Renderer[] _renderers;
    private MaterialPropertyBlock _block;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _block = new MaterialPropertyBlock();
    }

    private void Start()
    {
        transform.localRotation = Quaternion.identity;
        Update3DDiceRotation();
        UpdateVisualState();
    }

    /// <summary>
    /// Taps are only accepted from the player whose turn it is, and only while the game is
    /// actually waiting for a choice. Previously any tap at any time toggled the selection, so
    /// the dice reacted during the computer's turn and mid-animation.
    /// </summary>
    private void OnMouseDown()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // A computer house is thinking. Say so rather than swallowing the tap — silence reads
        // as a broken button.
        if (!gm.IsHumanControlled(gm.currentTurnColor))
        {
            Notify("NOT YOUR TURN", gm.currentTurnColor);
            return;
        }

        // Between rolls, during a hop, or while the turn is resolving: nothing to choose yet.
        if (gm.currentPhase != GameManager.TurnPhase.AwaitingSelection)
        {
            if (gm.currentPhase == GameManager.TurnPhase.AwaitingRoll)
                Notify("ROLL FIRST", gm.currentTurnColor);
            return;
        }

        gm.ToggleDiceSelection(gameObject);

        // Say what is now selected. Without this the only cue was the tint, and a player who
        // tapped the second die could not tell a switch from a combine — which is what made
        // deselecting look like it had not worked.
        int v = gm.GetSelectedDiceValue();
        if (v > 0)
            Notify(gm.isDiceCombined ? "COMBINED " + v : "SELECTED " + v, gm.currentTurnColor);
        else
            Notify("NO DIE SELECTED", gm.currentTurnColor);
    }

    private static void Notify(string message, GameManager.PlayerColor turn)
    {
        if (UIManager.Instance == null) return;
        Color c = LudoNX.UI.UITheme.Current.ForPlayer(turn);
        UIManager.Instance.ShowNotice(message, Color.Lerp(c, Color.white, 0.55f));
    }

    public void Roll()
    {
        Update3DDiceRotation();
        UpdateVisualState();
    }

    public void Update3DDiceRotation()
    {
        if (GameManager.Instance == null) return;

        int value = 0;
        if (gameObject == GameManager.Instance.dice1Object) value = GameManager.Instance.dice1Value;
        else if (gameObject == GameManager.Instance.dice2Object) value = GameManager.Instance.dice2Value;

        switch (value)
        {
            case 1: transform.localRotation = Quaternion.Euler(0, 0, 0); break;
            case 2: transform.localRotation = Quaternion.Euler(180, 0, 0); break;
            case 3: transform.localRotation = Quaternion.Euler(-90, 0, 0); break;
            case 4: transform.localRotation = Quaternion.Euler(90, 0, 0); break;
            case 5: transform.localRotation = Quaternion.Euler(0, -90, 0); break;
            case 6: transform.localRotation = Quaternion.Euler(0, 90, 0); break;
        }
    }

    /// <summary>
    /// Marks the chosen die by tinting it.
    ///
    /// This used to write transform.localScale: a fixed 1.15 for the selected die and a
    /// "default" cached in Awake for the other. Both were wrong. BoardCameraFramer sizes the
    /// dice to the board after Awake has run, so the cached default was a stale pre-framing
    /// value, and 1.15 was an absolute scale rather than a multiplier — selecting a die
    /// resized it to something unrelated to its sibling. Touching scale here also fought the
    /// framer for control of the same property. Colour carries the state and leaves size alone.
    /// </summary>
    public void UpdateVisualState()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool isSelected = false;
        if (gameObject == gm.dice1Object) isSelected = gm.isDice1Selected;
        else if (gameObject == gm.dice2Object) isSelected = gm.isDice2Selected;

        Tint(isSelected ? selectedTint : Color.white);
    }

    private void Tint(Color c)
    {
        if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>(true);
        if (_block == null) _block = new MaterialPropertyBlock();

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_block);
            // URP shaders use _BaseColor; older/2D ones use _Color. Setting both is harmless and
            // saves branching on which shader a die happens to be using.
            _block.SetColor(BaseColorId, c);
            _block.SetColor(ColorId, c);
            r.SetPropertyBlock(_block);
        }
    }
}
