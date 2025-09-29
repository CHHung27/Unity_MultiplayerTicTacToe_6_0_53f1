using System;
using System.Collections.Generic;
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

    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }

    /// <summary>
    /// struct that represents a line
    /// a line contains three points -> three grid positions stored in gridVector2IntList
    /// and a center point -> stored in centerGridPosition
    /// 
    /// all lines initialized in Awake()
    /// </summary>
    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }

    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>(); // can include optional parameters; default: everybody can read, but only server can write
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList; // stores all possible lines; initialized in Awake

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition; // Event listened to by GameVisualManager to place objects
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    } // pass in x, y, and player placing the object with EventArgs

    public event EventHandler OnGameStarted; // fired when both players joins; listened by PlayerUI
    public event EventHandler<OnGameWinEventArgs> OnGameWin; // fire this when one player wins; listened by GameVisualManager
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winningPlayerType;
    }
    public event EventHandler OnCurrentPlayablePlayerTypeChange; // fire when turn change; listened by PlayerUI
    public event EventHandler OnRematch; // fire when RematchRpc() runs; listened by GameVisualManager to destroy previous visuals

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Error: More than one {name}");
        }

        Instance = this;

        playerTypeArray = new PlayerType[3, 3]; // initialize our playerTypeArray as a 3x3 grid

        lineList = new List<Line>
        {
            // Horizontal Lines
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)},
                centerGridPosition = new Vector2Int(1, 0),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(1, 2),
                orientation = Orientation.Horizontal,
            },

            // Vertical Lines
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)},
                centerGridPosition = new Vector2Int(0, 1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(2, 1),
                orientation = Orientation.Vertical,
            },

            // Diagonal Lines
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalA,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalB,
            },
        };
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

        TestWinner();
    }


    /// <summary>
    /// checks if grid has a winning line
    /// </summary>
    private void TestWinner()
    {
        // iterates the lineList, testing all lines for winner
        for (int i = 0; i < lineList.Count; i++) {
            Line line = lineList[i];
            if (TestWinnerLine(line))
            {
                // Win!
                Debug.Log("Winner!");
                currentPlayablePlayerType.Value = PlayerType.None; // stops play
                TriggerOnGameWinRPC(i, playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y]); // invoke OnGameWin event as RPC
                break;
            }
        }
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRPC(int lineIndex, PlayerType winningPlayerType)
    {
        Line line = lineList[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winningPlayerType = winningPlayerType,
        });
    }


    /// <summary>
    /// returns true if given player type are not PlayerType.None and all match
    /// </summary>
    /// <param name="aPlayerType"></param>
    /// <param name="bPlayerType"></param>
    /// <param name="cPlayerType"></param>
    /// <returns></returns>
    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        return
            aPlayerType != PlayerType.None &&
            aPlayerType == bPlayerType &&
            bPlayerType == cPlayerType;
    }

    /// <summary>
    /// returns true if given line is a winner i.e. are of the same player type (crosses or circles)
    /// uses the other TestWinnerLine to test
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]);
    }


    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }


    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        // reset playerTypeArray
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                playerTypeArray[x, y] = PlayerType.None;
            }
        }

        // current playable player
        currentPlayablePlayerType.Value = PlayerType.Cross;

        // invoke event for GameVisualManager to destroy all visuals; using Rpc so client and server receive event
        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }
}