using System.Collections;
using UnityEngine;

public class DiceRoll : MonoBehaviour
{
    public static DiceRoll Instance { get; private set; }

    public static int lastRolledValue = 0;
    public bool isRolling = false;

    [Header("3D Roll Settings")]
    public float rollDuration = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnMouseDown()
    {
        // Prevent rolling if already rolling or if a move is pending
        if (isRolling || lastRolledValue > 0) return;

        StartCoroutine(Roll3DDiceRoutine());
    }

    public IEnumerator Roll3DDiceRoutine()
    {
        isRolling = true;

        float elapsed = 0f;

        // Tumble 3D cube randomly during roll duration
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            transform.Rotate(new Vector3(
                Random.Range(180f, 360f) * Time.deltaTime * 5f,
                Random.Range(180f, 360f) * Time.deltaTime * 5f,
                Random.Range(180f, 360f) * Time.deltaTime * 5f
            ));
            yield return null;
        }

        // Reset rotation alignment
        transform.eulerAngles = Vector3.zero;

        // Generate final roll outcome (1 to 6)
        lastRolledValue = Random.Range(1, 7);

        Debug.Log("🎲 " + GameManager.Instance.currentTurn + " rolled a " + lastRolledValue + "!");

        isRolling = false;

        // Send outcome to GameManager to validate and process
        GameManager.Instance.ProcessRollResult(lastRolledValue);
    }
}