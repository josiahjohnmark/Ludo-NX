using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fans out every token that shares a tile so none of them is ever hidden underneath another.
///
/// The problem this solves: two or more tokens on one tile — routine on a safe zone, and
/// guaranteed in the home triangle where all four of a colour end up — used to land on the exact
/// same point. The one drawn last covered the rest, so a player could not see how many tokens were
/// there, whose they were, or whether a stack was worth attacking.
///
/// The solution is circle packing, not a fixed grid. Each token shrinks to the largest radius at
/// which N equal circles still fit inside one token's original footprint, and sits at a packing
/// position for that N. Two consequences fall out of that choice, and both matter:
///
///   • The cluster never grows past the space a single token already occupied, so a busy tile
///     never bleeds into its neighbours.
///   • Tokens shrink monotonically as more arrive — 1.0, 0.50, 0.46, 0.41, 0.37 … — so the stack
///     getting visibly tighter is itself the signal that another token joined.
///
/// The ratios below are the known optimal packings of N unit circles in a unit circle. Because
/// they are optimal, non-overlap is guaranteed by construction rather than by tuning, and
/// <see cref="Gap"/> then shaves a little off each radius to leave clear air between neighbours.
///
/// The home triangle is the one place the footprint is allowed to grow (<see cref="HomeAreaScale"/>),
/// because that area is drawn much larger than a path tile and four tokens packed into a single
/// tile's worth of space would look needlessly cramped there.
/// </summary>
public static class TokenStackLayout
{
    /// <summary>Shaves each token's radius so neighbours have visible air between them rather than touching.</summary>
    public static float Gap = 0.90f;

    /// <summary>
    /// How much wider the cluster may spread once tokens are home. 1 = a single tile's footprint.
    /// The home triangle is far bigger than a tile, so the four finishers get room to breathe.
    /// </summary>
    public static float HomeAreaScale = 1.75f;

    private static readonly Dictionary<Transform, List<TokenMovement>> Groups =
        new Dictionary<Transform, List<TokenMovement>>();

    /// <summary>
    /// Recomputes every stack on the board and hands each token its slot.
    ///
    /// Called whenever occupancy can have changed — a token lands, is captured, is sent home, or a
    /// match starts. Cheap enough to run on any of those: sixteen tokens at the very most.
    /// </summary>
    public static void Refresh(bool animate = true)
    {
        // Play mode only. This is a presentation pass over live state, and the sizes it writes are
        // relative to each token's authored scale — which only exists once Awake has captured it.
        // Running in the editor would scale tokens against a default of 1, and any save after that
        // would bake the wrong sizes and positions into the scene permanently.
        if (!Application.isPlaying) return;

        Groups.Clear();

        TokenMovement[] tokens = Object.FindObjectsByType<TokenMovement>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (TokenMovement token in tokens)
        {
            if (token == null) continue;

            // A token mid-hop has already vacated its old tile and has not arrived at the next
            // one. Counting it in either would make the tokens it is leaving stay squeezed up for
            // the whole flight, and would seat it before it lands.
            if (token.IsMoving) continue;

            // A token in its yard already has a slot of its own and never shares one.
            if (token.isHome)
            {
                token.ApplyStackSlot(Vector3.zero, 1f, animate);
                continue;
            }

            Transform tile = token.GetStackTile();
            if (tile == null)
            {
                token.ApplyStackSlot(Vector3.zero, 1f, animate);
                continue;
            }

            if (!Groups.TryGetValue(tile, out List<TokenMovement> group))
            {
                group = new List<TokenMovement>();
                Groups[tile] = group;
            }
            group.Add(token);
        }

        foreach (KeyValuePair<Transform, List<TokenMovement>> entry in Groups)
        {
            LayOutGroup(entry.Value, animate);
        }
    }

    private static void LayOutGroup(List<TokenMovement> group, bool animate)
    {
        int n = group.Count;

        if (n == 1)
        {
            group[0].ApplyStackSlot(Vector3.zero, 1f, animate);
            return;
        }

        // Stable ordering, so the same token keeps the same seat between refreshes instead of
        // swapping places with a sibling every time the stack is recomputed. By name rather than
        // instance id: it is deterministic across runs and matches the hierarchy order, so a seat
        // can be reasoned about while debugging.
        group.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        // Finished tokens sit in the home triangle, which is drawn much larger than a path tile.
        float spread = group[0].isFinished ? HomeAreaScale : 1f;

        float ratio = RatioFor(n);
        float radius = group[0].StackFootprintRadius * spread;
        float tokenRadius = radius * ratio;
        float ringRadius = radius - tokenRadius;

        for (int i = 0; i < n; i++)
        {
            Vector3 offset = SlotOffset(i, n, ringRadius);
            group[i].ApplyStackSlot(offset, ratio * spread * Gap, animate);
        }
    }

    /// <summary>
    /// Largest radius, as a fraction of the stack's footprint, at which N tokens still clear each
    /// other in the arrangement <see cref="SlotOffset"/> actually uses.
    ///
    /// Derived rather than tabulated. For a ring of m tokens at radius (R − r), neighbours are a
    /// chord 2(R − r)·sin(π/m) apart and must not be closer than 2r, which rearranges to
    /// r ≤ R·sin(π/m) / (1 + sin(π/m)). Once a token sits in the middle as well, the centre-to-ring
    /// distance R − r must also clear 2r, capping the ratio at 1/3.
    ///
    /// The formula happens to reproduce the known optimal packings exactly for N ≤ 8 (0.5, 0.4641,
    /// 0.4142, 0.3702, 0.3333, 0.3333, 0.3026), and unlike a lookup table it stays correct for any
    /// N — including the sixteen-token worst case of every token in the game on one safe tile.
    /// </summary>
    private static float RatioFor(int n)
    {
        if (n <= 1) return 1f;
        if (n == 2) return 0.5f;

        if (n < 6)
        {
            float ring = Mathf.Sin(Mathf.PI / n);
            return ring / (1f + ring);
        }

        float s = Mathf.Sin(Mathf.PI / (n - 1));   // one token sits in the centre
        return Mathf.Min(s / (1f + s), 1f / 3f);
    }

    /// <summary>
    /// Where the i-th of n tokens sits. Two tokens sit side by side; three to five form a ring;
    /// six and up put one in the middle of the ring, which is what the optimal packings do.
    /// </summary>
    private static Vector3 SlotOffset(int i, int n, float ringRadius)
    {
        if (n == 2)
        {
            return new Vector3(i == 0 ? -ringRadius : ringRadius, 0f, 0f);
        }

        if (n >= 6)
        {
            if (i == 0) return Vector3.zero;   // centre token

            int ringCount = n - 1;
            float ringAngle = 90f + 360f * (i - 1) / ringCount;
            return Polar(ringAngle, ringRadius);
        }

        float angle = 90f + 360f * i / n;   // start at the top so small stacks read as upright
        return Polar(angle, ringRadius);
    }

    private static Vector3 Polar(float degrees, float radius)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
    }
}
