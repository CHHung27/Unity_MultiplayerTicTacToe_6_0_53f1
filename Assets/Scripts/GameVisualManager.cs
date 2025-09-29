using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// inherit from NetworkBehaviour (and attached NetworkObject to this game object) to enable the use of RPCs
/// </summary>
public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 3.1f; // grid size interval is 3.1f

    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab;

    private List<GameObject> visualGameObjectList; // keeps track of all spawned visual objects so we can clear them OnRematch

    
    private void Awake()
    {
        visualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManager_OnClickedOnGridPosition;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
    }

    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        // only run if IsServer
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // clear visualGameObjectList
        foreach (GameObject visualGameObject in visualGameObjectList)
        {
            Destroy(visualGameObject);
        }

        visualGameObjectList.Clear();
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        // only run placing winning line code if IsServer
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // figure out winning line orientation
        float eulerZ = 0f;
        switch (e.line.orientation)
        {
            case GameManager.Orientation.Horizontal:   eulerZ = 0f; break;
            case GameManager.Orientation.Vertical:     eulerZ = 90f; break;
            case GameManager.Orientation.DiagonalA:    eulerZ = 45f; break;
            case GameManager.Orientation.DiagonalB:    eulerZ = -45f; break;
        }

        // place win line on given line with correct orientation
        Transform lineCompleteTransform = 
            Instantiate(lineCompletePrefab,
            GetGridWorldPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y),
            Quaternion.Euler(0, 0, eulerZ));
        lineCompleteTransform.GetComponent<NetworkObject>().Spawn(true);

        visualGameObjectList.Add(lineCompleteTransform.gameObject);
    }

    private void GameManager_OnClickedOnGridPosition(object sender, GameManager.OnClickedOnGridPositionEventArgs e)
    {
        Debug.Log("GameManager_OnClickedOnGridPosition");
        SpawnObjectRPC(e.x, e.y, e.playerType);
    }

    /// <summary>
    /// Spawns cross or circle prefab based on playerType at provided location
    /// 
    /// Server authority is a system where the server is the ultimate source of truth and manages critical actions
    /// This RPC method ensures that spawning object is always handled by the server only
    /// Server: spawns locally, syncs to client via NetworkObject
    /// Client: RPC to server to spawn object, NEVER spawns locally
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="playerType">must be included to pass playerType. As attempting to grab localPlayerType 
    ///                          will return Cross every single time (since RPC runs on server only)</param>
    [Rpc(SendTo.Server)]  // SendTo parameter defines where the RPC will run
    private void SpawnObjectRPC(int x, int y, GameManager.PlayerType playerType)
    {
        Debug.Log("SpawnObjectRPC");
        Transform spawnedPrefab;
        switch (playerType)
        {
            default:
            case GameManager.PlayerType.Cross:
                spawnedPrefab = crossPrefab;
                break;
            case GameManager.PlayerType.Circle:
                spawnedPrefab = circlePrefab;
                break;
        }
        Transform spawnedPrefabTransform = Instantiate(spawnedPrefab, GetGridWorldPosition(x, y), Quaternion.identity);
        spawnedPrefabTransform.GetComponent<NetworkObject>().Spawn(true); // syncs Network Object across server/client

        visualGameObjectList.Add(spawnedPrefabTransform.gameObject);
    }

    /// <summary>
    /// takes in x and y value and translates that to actual game world position
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}