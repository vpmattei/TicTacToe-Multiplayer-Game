using System;
using UnityEngine;
using Unity.Netcode;
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
        public PlayerType winPlayerType;
    };
    public event EventHandler OnRematch;
    public event EventHandler OnGameTie;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlacedObject;

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

    private PlayerType localPlayerType;
    public NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;
    private PlayerType winnerPlayerType;
    private NetworkVariable<int> playerCrossScore = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();

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

        winnerPlayerType = PlayerType.None;
    }

    public override void OnNetworkSpawn()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log("OnNetworkSpawn: " + localClientId);
        if (localClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) =>
        {
            OnCurrentPlayablePlayerTypeChanged.Invoke(this, EventArgs.Empty);
        };

        playerCrossScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
        playerCircleScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
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
        TriggerOnPlaceObjectRpc();

        OnClickedOnGridPosition.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType
        });

        // Switch Current Playable Player Type
        currentPlayablePlayerType.Value = currentPlayablePlayerType.Value == PlayerType.Cross ? PlayerType.Circle : PlayerType.Cross;

        TestWinner();

        TestTie();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlaceObjectRpc()
    {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }

    private void TestTie()
    {
        if (!AreAllGridsFilled()) return; // If all the grids are not filled, we return as the game is still going


        for (int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];
            if (TestWinnerLine(line))
            {
                return; // If we find a winner we return as this is not a tie
            }
        }

        // All the grids are filled and we have not found a winner, so it is a tie
        TriggerOnGameTieRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTieRpc()
    {
        winnerPlayerType = PlayerType.None;
        OnGameTie.Invoke(this, EventArgs.Empty);
    }

    private bool AreAllGridsFilled()
    {
        bool areAllGridsFiled = true;
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(0); y++)
            {
                PlayerType playerType = playerTypeArray[x, y];

                // If we find an empty grid, i.e not placed with a Cross or Circle, then that means the game is not over yet
                if (playerType == PlayerType.None)
                {
                    areAllGridsFiled = false;
                    break;
                }
            }
        }
        return areAllGridsFiled;
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
        for (int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];
            if (TestWinnerLine(line))
            {
                currentPlayablePlayerType.Value = PlayerType.None;
                winnerPlayerType = playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y];

                // Setting the score for each player
                if (winnerPlayerType == PlayerType.Cross) playerCrossScore.Value++;
                else if (winnerPlayerType == PlayerType.Circle) playerCircleScore.Value++;

                TriggerOnGameWinRpc(i, winnerPlayerType);
                break;
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];
        OnGameWin.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType
        });
    }

    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(0); y++)
            {
                playerTypeArray[x, y] = PlayerType.None;
            }
        }

        // Next starting player is the loser of this round, if it is a tie, then it is a random player
        if (winnerPlayerType == PlayerType.Cross) currentPlayablePlayerType.Value = PlayerType.Circle;
        else if (winnerPlayerType == PlayerType.Circle) currentPlayablePlayerType.Value = PlayerType.Cross;
        else if (winnerPlayerType == PlayerType.None)
        {
            int randomIntValue = UnityEngine.Random.Range(0, 2);

            if (randomIntValue == 0) currentPlayablePlayerType.Value = PlayerType.Circle;
            else currentPlayablePlayerType.Value = PlayerType.Cross;
        }

        winnerPlayerType = PlayerType.None;

        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }

    public PlayerType GetWinnerPlayerType()
    {
        return winnerPlayerType;
    }

    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }

    public void GetScores(out int playerCrossScore, out int playerCircleScore)
    {
        playerCrossScore = this.playerCrossScore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }
}
