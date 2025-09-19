using Unity.Netcode;
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
        SpawnObjectRPC(e.x, e.y);
    }

    /// <summary>
    /// Server authority is a system where the server is the ultimate source of truth and manages critical actions
    /// This RPC method ensures that spawning object is always handled by the server only
    /// Server: spawns locally, syncs to client via NetworkObject
    /// Client: RPC to server to spawn object, NEVER spawns locally
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    [Rpc(SendTo.Server)]  // SendTo parameter defines where the RPC will run
    private void SpawnObjectRPC(int x, int y)
    {
        Debug.Log("SpawnObjectRPC");
        Transform spawnedCrossTransform = Instantiate(crossPrefab);
        spawnedCrossTransform.GetComponent<NetworkObject>().Spawn(true);
        spawnedCrossTransform.position = GetGridWorldPosition(x, y);
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
