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
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>(); // can include optional parameters; default: everybody can read, but only server can write
    private PlayerType[,] playerTypeArray;

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition; // Event listened to by GameVisualManager
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    } // pass in x and y with EventArgs

    public event EventHandler OnGameStarted; // fire this when both players joined; listened by PlayerUI
    public event EventHandler OnCurrentPlayablePlayerTypeChange; // fire when turn change; listened by PlayerUI

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Error: More than one {name}");
        }

        Instance = this;

        playerTypeArray = new PlayerType[3, 3];
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
            // runs whenever a client connects
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        // NetworkVariable delegate; detects currentPlayablePlayerType value change
        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) =>
        {
            OnCurrentPlayablePlayerTypeChange?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>
    /// triggered whenever a client connects to the network
    /// </summary>
    /// <param name="obj"> id of the client connected </param>
    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        // start game if 2 players connected
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross; // set server player to cross
            TriggerOnGameStartedRPC();
        }
    }

    /// <summary>
    /// invoke OnGameStarted as a RPC so both clients and hosts can listen to it
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRPC()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty); // invoke OnGameStarted
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
        if (playerType != currentPlayablePlayerType.Value)
        {
            return;
        } // disallow clicking if not current player turn

        if (playerTypeArray[x, y] != PlayerType.None)
        {
            return;
        } // disallow clicking position is non-empty

        playerTypeArray[x , y] = playerType;

        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType,
        }); // listened to by GameVisualManager

        // switch playerType to rotate turns
        switch (currentPlayablePlayerType.Value)
        {
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType.Value = PlayerType.Cross;
                break;
        }
    }

    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }
}