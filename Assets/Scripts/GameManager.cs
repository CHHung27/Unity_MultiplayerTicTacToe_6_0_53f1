using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// inherit from NetworkBehaviour to allow use of network related functions
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance {  get; private set; }

    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }

    private PlayerType localPlayerType;
    private PlayerType currentPlayablePlayerType;

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition; // Event listened to by GameVisualManager
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    } // pass in x and y with EventArgs


    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Error: More than one {name}");
        }

        Instance = this;
    }

    /// <summary>
    /// runs when network established and assigns playerType based on their client id
    /// exists within NetworkBehaviour;
    /// </summary>
    public override void OnNetworkSpawn()
    {
        Debug.Log("Local Client id: " + NetworkManager.Singleton.LocalClientId); // accessing LocalClientId via NetworkManager singleton instance
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }

        // initialize currentPlayablePlayerType, starting player turn flip-flop
        if (IsServer)
        {
            currentPlayablePlayerType = PlayerType.Cross;
        }
    }

    /// <summary>
    /// Clicking grid position logic. Switches player turn on successful taking turns.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="playerType"></param>
    [Rpc(SendTo.Server)] // only runs on server; clients send message for server to execute
    public void ClickedOnGridPositionRPC(int x, int y, PlayerType playerType)
    {
        Debug.Log("Clicked On " + x + ", " + y);
        if (playerType != currentPlayablePlayerType)
        {
            return;
        } // disallow clicking if not current player turn

        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType,
        }); // listened to by GameVisualManager

        // switch playerType to rotate turns
        switch (currentPlayablePlayerType)
        {
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType= PlayerType.Cross;
                break;
        }
    }

    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }
}
