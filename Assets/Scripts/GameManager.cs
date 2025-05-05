using System;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using System.Collections.Generic;

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
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
    };

    public enum PlayerType
    {
        None,
        Cross,
        Circle
    }

    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB
    }

    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation lineOrientation;
    }

    public PlayerType LocalPlayerType { get; private set; }
    public NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager Instance!");
        }
        Instance = this;

        playerTypeArray = new PlayerType[3, 3];

        lineList = new List<Line>
        {
            // Horizontal
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)},
                centerGridPosition = new Vector2Int(1, 0),
                lineOrientation = Orientation.Horizontal
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)},
                centerGridPosition = new Vector2Int(1, 1),
                lineOrientation = Orientation.Horizontal
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(1, 2),
                lineOrientation = Orientation.Horizontal
            },

            // Vertical
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)},
                centerGridPosition = new Vector2Int(0, 1),
                lineOrientation = Orientation.Vertical
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2)},
                centerGridPosition = new Vector2Int(1, 1),
                lineOrientation = Orientation.Vertical
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(2, 1),
                lineOrientation = Orientation.Vertical
            },

            // Diagonal
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(1, 1),
                lineOrientation = Orientation.DiagonalA
            },
            new Line {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0)},
                centerGridPosition = new Vector2Int(1, 1),
                lineOrientation = Orientation.DiagonalB
            },
        };
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

    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]);
    }

    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        bool isWinner =
        aPlayerType != PlayerType.None &&
        aPlayerType == bPlayerType &&
        bPlayerType == cPlayerType;

        return isWinner;
    }

    private void TestWinner()
    {
        foreach (Line line in lineList)
        {
            if (TestWinnerLine(line))
            {
                Debug.Log(playerTypeArray[0, 0].ToString() + " wins!");
                OnGameWin.Invoke(this, new OnGameWinEventArgs
                {
                    line = line
                });
                currentPlayablePlayerType.Value = PlayerType.None;
                break;
            }
        }

    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }
}
