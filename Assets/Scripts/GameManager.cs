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

    public enum PlayerType
    {
        None,
        Cross,
        Circle
    }

    public PlayerType LocalPlayerType { get; private set; }
    private PlayerType currentPlayablePlayerType;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager Instance!");
        }
        Instance = this;
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
            currentPlayablePlayerType = PlayerType.Cross;
        }
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        Debug.Log("Clicked on the grid: " + x + ", " + y);

        if (playerType != currentPlayablePlayerType) return;

        OnClickedOnGridPosition.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType
        });

        // Switch Current Playable Player Type
        currentPlayablePlayerType = currentPlayablePlayerType == PlayerType.Cross ? PlayerType.Circle : PlayerType.Cross;
    }
}
