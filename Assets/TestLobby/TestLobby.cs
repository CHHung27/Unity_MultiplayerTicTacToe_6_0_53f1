using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class TestLobby : MonoBehaviour
{

    private Lobby hostLobby;
    private float heartBeatTimer;
    

    private async void Start()
    {
        // initialize services
        await UnityServices.InitializeAsync(); // sends request and pauses here until receiving response

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        // sign in and create account for user
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); // no need for username + password; can upgrade to other sign in services if needed
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    /// <summary>
    /// keeps lobby alive by pinging them every heartBeatTimerMax amount of seconds
    /// (lobbies die without receiving connection after 30 seconds by default)
    /// </summary>
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0f)
            {
                float heartBeatTimerMax = 15f;
                heartBeatTimer = heartBeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id); // sends heartbeat
            }
        }
    }

    private async void CreateLobby()
    {
        try {

            string lobbyName = "MyLobby";
            int maxPlayers = 4;
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

            hostLobby = lobby;

            Debug.Log("Created lobby! " + lobby.Name + " " + lobby.MaxPlayers);
        } catch (LobbyServiceException e) { 
            Debug.Log(e);
        }
    }

    /// <summary>
    /// List existing lobbies to the console
    /// </summary>
    private async void ListLobbies()
    {
        try
        {
            // QueryLobbiesOptions example: can be used to filter queryResponse according to given parameters
            // ideally use an UI to set these parameters in the final built game
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                // return 25 lobbies
                Count = 25,
                // lobby filter options
                Filters = new List<QueryFilter>
                {
                    // example filter option "available slot > 0"
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                // lobby sort order options
                Order = new List<QueryOrder>
                {
                    // sorted order: oldest to newest
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                },
            };

            // list containing existing lobbies; can optionally pass in queryLobbiesOptions to filter through lobbies
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
        } catch (LobbyServiceException e) { 
            Debug.Log(e);
        }
    }

    private async void JoinLobby()
    {
        try
        {
            // join lobby by id
            await LobbyService.Instance.JoinLobbyByIdAsync(hostLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}