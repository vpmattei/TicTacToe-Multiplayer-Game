using System;
using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Color winColor;
    [SerializeField] private Color loseColor;

    void Start()
    {
        Hide();

        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (e.winPlayerType == GameManager.Instance.GetLocalPlayerType())
        {
            gameOverText.SetText("You Win!");
            gameOverText.color = winColor;
        }
        else
        {
            gameOverText.SetText("You Lose!");
            gameOverText.color = loseColor;
        }
        Show();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
}
