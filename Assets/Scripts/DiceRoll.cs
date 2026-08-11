using UnityEngine;

public class DiceRoll : MonoBehaviour
{
    [Header("Visual Feedback")]
    public Vector3 defaultScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1.15f);

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void Start()
    {
        transform.localRotation = Quaternion.identity;
        Update3DDiceRotation();
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleDiceSelection(gameObject);
        }
    }

    public void Roll()
    {
        transform.localScale = defaultScale;
        Update3DDiceRotation();
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

    public void UpdateVisualState()
    {
        if (GameManager.Instance == null) return;

        bool isSelected = false;
        if (gameObject == GameManager.Instance.dice1Object)
            isSelected = GameManager.Instance.isDice1Selected;
        else if (gameObject == GameManager.Instance.dice2Object)
            isSelected = GameManager.Instance.isDice2Selected;

        transform.localScale = isSelected ? selectedScale : defaultScale;
    }
}