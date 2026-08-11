using UnityEngine;
using System.Collections;

public class Dice3DRoller : MonoBehaviour
{
    [Header("3D Dice Objects")]
    public Transform dice1Transform;
    public Transform dice2Transform;

    [Header("Spin Settings")]
    public float spinDuration = 0.8f;
    public float spinSpeed = 1200f;

    [Header("State")]
    public bool isRolling = false;

    // Standard face rotations for landing 1 through 6 (calibrated for 3D dice mesh)
    private static readonly Vector3[] faceRotations = new Vector3[]
    {
        new Vector3(0, 0, 0),       // Face 1 -> Face_1
        new Vector3(180, 0, 0),     // Face 2 -> Face_2
        new Vector3(-90, 0, 0),     // Face 3 -> Face_3
        new Vector3(90, 0, 0),      // Face 4 -> Face_4
        new Vector3(0, -90, 0),     // Face 5 -> Face_5
        new Vector3(0, 90, 0)       // Face 6 -> Face_6
    };

    public void TriggerInPlaceRoll(int targetResult1, int targetResult2)
    {
        if (isRolling) return;
        StartCoroutine(AnimateInPlaceSpin(targetResult1, targetResult2));
    }

    private IEnumerator AnimateInPlaceSpin(int res1, int res2)
    {
        isRolling = true;

        float elapsedTime = 0f;

        // Rapid 3D tumble on all axes right in position
        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;

            if (dice1Transform != null)
            {
                dice1Transform.Rotate(new Vector3(spinSpeed, spinSpeed * 0.7f, spinSpeed * 0.3f) * Time.deltaTime);
            }

            if (dice2Transform != null)
            {
                dice2Transform.Rotate(new Vector3(spinSpeed * 0.5f, spinSpeed, spinSpeed * 0.8f) * Time.deltaTime);
            }

            yield return null;
        }

        // Snap smoothly to calculated target face angles
        if (dice1Transform != null)
        {
            dice1Transform.localRotation = Quaternion.Euler(faceRotations[Mathf.Clamp(res1 - 1, 0, 5)]);
        }

        if (dice2Transform != null)
        {
            dice2Transform.localRotation = Quaternion.Euler(faceRotations[Mathf.Clamp(res2 - 1, 0, 5)]);
        }

        isRolling = false;
    }
}