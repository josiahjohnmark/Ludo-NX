using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Dice3DRoller : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Button rollButton;

    [Header("3D Dice References")]
    [SerializeField] private Rigidbody dice1Rigidbody;
    [SerializeField] private Rigidbody dice2Rigidbody;

    [Header("Spawn / Roll Positions")]
    [SerializeField] private Transform rollSpawnPoint; // Position in air where dice spawn/throw from

    [Header("Physics Settings")]
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float torqueForce = 20f;

    private void Start()
    {
        if (rollButton != null)
        {
            rollButton.onClick.AddListener(Roll3DDice);
        }
    }

    public void Roll3DDice()
    {
        // 1. Reset Dice Positions above the board
        ResetDice(dice1Rigidbody);
        ResetDice(dice2Rigidbody);

        // 2. Add random directional impulse forces
        Vector3 forceDirection1 = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), Random.Range(-2f, 2f)) * throwForce;
        Vector3 forceDirection2 = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), Random.Range(-2f, 2f)) * throwForce;

        dice1Rigidbody.AddForce(forceDirection1, ForceMode.Impulse);
        dice2Rigidbody.AddForce(forceDirection2, ForceMode.Impulse);

        // 3. Add heavy rotational spin (torque)
        Vector3 randomTorque1 = new Vector3(Random.Range(-500, 500), Random.Range(-500, 500), Random.Range(-500, 500)) * torqueForce;
        Vector3 randomTorque2 = new Vector3(Random.Range(-500, 500), Random.Range(-500, 500), Random.Range(-500, 500)) * torqueForce;

        dice1Rigidbody.AddTorque(randomTorque1, ForceMode.Impulse);
        dice2Rigidbody.AddTorque(randomTorque2, ForceMode.Impulse);
    }

    private void ResetDice(Rigidbody rb)
    {
        if (rb == null) return;
        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Position slightly above board
        if (rollSpawnPoint != null)
        {
            rb.transform.position = rollSpawnPoint.position + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
        }

        // Random starting orientation
        rb.transform.rotation = Random.rotation;
    }
}