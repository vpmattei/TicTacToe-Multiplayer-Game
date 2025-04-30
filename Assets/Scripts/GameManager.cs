using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager Instance!");
        }
        Instance = this; 
    }

    public void ClickedOnGridPosition(int x, int y)
    {
        Debug.Log("Clicked on the grid: " + x + ", " + y);
    }
}
