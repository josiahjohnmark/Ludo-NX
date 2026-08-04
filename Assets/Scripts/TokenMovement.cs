using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenMovement : MonoBehaviour
{
    [Header("Token Identity")]
    public GameManager.PlayerColor tokenColor;

    [Header("Path References")]
    public LudoPath mainPath;             
    public LudoPath homeStretchPath;      
    
    [Header("Home Base Position")]
    public Transform homeBasePosition;    
    
    [Header("Movement Settings")]
    public float stepDuration = 0.20f;
    public int startWaypointIndex = 0; 
    
    public bool isInHomeBase { get; private set; } = true;
    public bool isFinished { get; private set; } = false;
    
    public int stepsTaken { get; private set; } = 0;
    public bool isOnHomeStretch { get; private set; } = false;
    public int currentPathIndex { get; private set; } = -1;
    
    private TileNode currentTileNode = null;
    private bool isMoving = false;

    private void Start()
    {
        ResetToHome();
    }

    public void ResetToHome()
    {
        if (currentTileNode != null)
        {
            currentTileNode.RemoveToken(this);
            currentTileNode = null;
        }

        isInHomeBase = true;
        isFinished = false;
        isOnHomeStretch = false;
        stepsTaken = 0;
        currentPathIndex = -1;

        if (homeBasePosition != null)
        {
            transform.position = new Vector3(homeBasePosition.position.x, homeBasePosition.position.y, -1f);
        }
    }

    private void OnMouseDown()
    {
        if (!GameManager.Instance.IsMyTurn(tokenColor))
        {
            Debug.Log("It is " + GameManager.Instance.currentTurn + "'s turn right now!");
            return;
        }

        if (!isMoving && DiceRoll.lastRolledValue > 0)
        {
            TryMove(DiceRoll.lastRolledValue);
        }
    }

    public void TryMove(int diceRoll)
    {
        if (isMoving || isFinished) return;

        if (isInHomeBase)
        {
            if (diceRoll == 6)
            {
                StartCoroutine(LeaveHomeBase());
            }
        }
        else
        {
            int totalPathLength = 50 + homeStretchPath.waypoints.Count;
            if (stepsTaken + diceRoll <= totalPathLength)
            {
                StartCoroutine(MoveRoutine(diceRoll));
            }
        }
    }

    private IEnumerator LeaveHomeBase()
    {
        isMoving = true;
        currentPathIndex = startWaypointIndex;
        stepsTaken = 0;
        isOnHomeStretch = false;
        isInHomeBase = false;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySpawn();

        TileNode targetNode = mainPath.waypoints[currentPathIndex];
        yield return StartCoroutine(HopToNode(targetNode));

        SetOccupiedNode(targetNode);
        CheckForCaptureOnNode(targetNode);

        GameManager.Instance.hasBonusRoll = true; 
        DiceRoll.lastRolledValue = 0; 
        isMoving = false;
        
        GameManager.Instance.NextTurn();
    }

    private IEnumerator MoveRoutine(int steps)
    {
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            if (currentTileNode != null)
            {
                currentTileNode.RemoveToken(this);
            }

            if (!isOnHomeStretch && stepsTaken >= 50)
            {
                isOnHomeStretch = true;
                currentPathIndex = 0; 
                stepsTaken++;
            }
            else if (!isOnHomeStretch)
            {
                stepsTaken++;
                currentPathIndex = (currentPathIndex + 1) % mainPath.waypoints.Count;
            }
            else
            {
                stepsTaken++;
                currentPathIndex++;
            }

            TileNode targetNode;
            if (isOnHomeStretch)
            {
                int safeIndex = Mathf.Clamp(currentPathIndex, 0, homeStretchPath.waypoints.Count - 1);
                targetNode = homeStretchPath.waypoints[safeIndex];

                if (AudioManager.Instance != null) 
                    AudioManager.Instance.PlayHomeStretchHop(safeIndex, homeStretchPath.waypoints.Count);
            }
            else
            {
                targetNode = mainPath.waypoints[currentPathIndex];

                if (AudioManager.Instance != null) AudioManager.Instance.PlayHop();
            }

            yield return StartCoroutine(HopToNode(targetNode));
            SetOccupiedNode(targetNode);
        }

        if (isOnHomeStretch && currentPathIndex == homeStretchPath.waypoints.Count - 1)
        {
            isFinished = true;
            Debug.Log(gameObject.name + " reached the final home triangle!");
        }

        bool capturedSomeone = CheckForCaptureOnNode(currentTileNode);

        if (DiceRoll.lastRolledValue == 6 || capturedSomeone)
        {
            GameManager.Instance.hasBonusRoll = true;
        }

        DiceRoll.lastRolledValue = 0; 
        isMoving = false;
        
        GameManager.Instance.NextTurn();
    }

    private IEnumerator HopToNode(TileNode targetNode)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetNode.transform.position;
        float elapsed = 0f;

        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / stepDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            float hopFactor = Mathf.Sin(t * Mathf.PI);
            currentPos.z = -1f - (hopFactor * 0.3f);
            
            transform.position = currentPos;
            yield return null;
        }

        transform.position = new Vector3(endPos.x, endPos.y, -1f);
    }

    private void SetOccupiedNode(TileNode targetNode)
    {
        if (currentTileNode != null)
        {
            currentTileNode.RemoveToken(this);
        }

        currentTileNode = targetNode;
        if (currentTileNode != null)
        {
            currentTileNode.AddToken(this);
        }
    }

    private bool CheckForCaptureOnNode(TileNode targetNode)
    {
        if (isOnHomeStretch || targetNode == null || targetNode.isSafeZone) return false;

        bool didCapture = false;
        List<TokenMovement> occupants = new List<TokenMovement>(targetNode.tokensOnTile);

        foreach (TokenMovement enemyToken in occupants)
        {
            if (enemyToken != this && enemyToken.tokenColor != this.tokenColor)
            {
                Debug.Log("💥 " + gameObject.name + " captured " + enemyToken.gameObject.name + " on Tile " + targetNode.tileIndex + "!");
                
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCapture();
                
                enemyToken.ResetToHome();
                didCapture = true;
            }
        }

        return didCapture;
    }
}