using System;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class GameManager : NetworkBehaviour
{

    public static GameManager Instance { get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public event EventHandler OnGameStarted;
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs {
        public Vector2Int centerGridPosition;
    };

    public enum PlayerType
    {
        None,
        Cross,
        Circle
    }

    public PlayerType LocalPlayerType { get; private set; }
    public NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypeArray;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager Instance!");
        }
        Instance = this;

        playerTypeArray = new PlayerType[3, 3];
    }

    public override void OnNetworkSpawn()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (localClientId == 0)
        {
            LocalPlayerType = PlayerType.Cross;
        }
        else
        {
            LocalPlayerType = PlayerType.Circle;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) =>
        {
            OnCurrentPlayablePlayerTypeChanged.Invoke(this, EventArgs.Empty);
        };
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        Debug.Log("Clicked on the grid: " + x + ", " + y);

        if (playerType != currentPlayablePlayerType.Value) return;

        if (playerTypeArray[x, y] != PlayerType.None) return;

        playerTypeArray[x, y] = playerType;

        OnClickedOnGridPosition.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType
        });

        // Switch Current Playable Player Type
        currentPlayablePlayerType.Value = currentPlayablePlayerType.Value == PlayerType.Cross ? PlayerType.Circle : PlayerType.Cross;

        TestWinner();
    }

    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        bool isWinner =
        aPlayerType != PlayerType.None &&
        aPlayerType == bPlayerType &&
        bPlayerType == cPlayerType;

        if (isWinner)
        {
            currentPlayablePlayerType.Value = PlayerType.None;
            //OnGameWin.Invoke(this, EventArgs.Empty);
        }

        return isWinner;
    }
    private void TestWinner()
    {
        // Horizontal Wins
        if (TestWinnerLine(playerTypeArray[0, 0], playerTypeArray[1, 0], playerTypeArray[2, 0]))
        {
            Debug.Log(playerTypeArray[0, 0].ToString() + " wins!");
        }
        if (TestWinnerLine(playerTypeArray[0, 1], playerTypeArray[1, 1], playerTypeArray[2, 1]))
        {
            Debug.Log(playerTypeArray[0, 1].ToString() + " wins!");
        }
        if (TestWinnerLine(playerTypeArray[0, 2], playerTypeArray[1, 2], playerTypeArray[2, 2]))
        {
            Debug.Log(playerTypeArray[0, 2].ToString() + " wins!");
        }

        // Vertical Wins
        if (TestWinnerLine(playerTypeArray[0, 0], playerTypeArray[0, 1], playerTypeArray[0, 2]))
        {
            Debug.Log(playerTypeArray[0, 0].ToString() + " wins!");
        }
        if (TestWinnerLine(playerTypeArray[1, 0], playerTypeArray[1, 1], playerTypeArray[1, 2]))
        {
            Debug.Log(playerTypeArray[1, 0].ToString() + " wins!");
        }
        if (TestWinnerLine(playerTypeArray[2, 0], playerTypeArray[2, 1], playerTypeArray[2, 2]))
        {
            Debug.Log(playerTypeArray[2, 0].ToString() + " wins!");
        }

        // Diagonal Wins
        if (TestWinnerLine(playerTypeArray[0, 0], playerTypeArray[1, 1], playerTypeArray[2, 2]))
        {
            Debug.Log(playerTypeArray[0, 0].ToString() + " wins!");
        }
        if (TestWinnerLine(playerTypeArray[0, 2], playerTypeArray[1, 1], playerTypeArray[2, 0]))
        {
            Debug.Log(playerTypeArray[0, 2].ToString() + " wins!");
        }

    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }
}
