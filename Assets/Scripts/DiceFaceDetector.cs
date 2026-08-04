using UnityEngine;

public class DiceFaceDetector : MonoBehaviour
{
    private Rigidbody rb;
    public bool isStopped { get; private set; }
    public int diceValue { get; private set; } = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Call this after throwing the dice to wait until physics stops moving
    /// </summary>
    public System.Collections.IEnumerator CheckWhenStopped(System.Action<int> onResultReady)
    {
        isStopped = false;

        // Give it a tiny fraction of a second to start moving first
        yield return new WaitForSeconds(0.2f);

        // Loop until physics velocity is virtually zero
        while (rb.linearVelocity.sqrMagnitude > 0.001f || rb.angularVelocity.sqrMagnitude > 0.001f)
        {
            yield return new WaitForSeconds(0.1f);
        }

        isStopped = true;
        diceValue = GetUpwardFace();
        
        Debug.Log($"Dice landed on: {diceValue}");
        onResultReady?.Invoke(diceValue);
    }

    private int GetUpwardFace()
    {
        // Compare the local directions against global World Up (Vector3.up)
        Vector3 worldUp = Vector3.up;

        float maxDot = -1f;
        int bestFace = 1;

        // Define local direction vectors for the faces
        // (Note: Adjust numbers 1-6 below if your dice asset mapping differs!)
        Vector3[] faceDirections = new Vector3[]
        {
            transform.up,       // Face 1 (Top)
            transform.forward,  // Face 2 (Front)
            transform.right,    // Face 3 (Right)
            -transform.right,   // Face 4 (Left)
            -transform.forward, // Face 5 (Back)
            -transform.up       // Face 6 (Bottom)
        };

        for (int i = 0; i < faceDirections.Length; i++)
        {
            float dot = Vector3.Dot(faceDirections[i], worldUp);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestFace = i + 1; // Faces 1 to 6
            }
        }

        return bestFace;
    }
}