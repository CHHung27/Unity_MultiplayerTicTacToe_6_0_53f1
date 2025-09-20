﻿using Unity.Netcode;
using UnityEngine;

/// <summary>
/// inherit from NetworkBehaviour (and attached NetworkObject to this game object) to enable the use of RPCs
/// </summary>
public class GameVisualManager : NetworkBehaviour
{
    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;


    private const float GRID_SIZE = 3.1f; // grid size interval is 3.1f

    private void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManager_OnClickedOnGridPosition;
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
