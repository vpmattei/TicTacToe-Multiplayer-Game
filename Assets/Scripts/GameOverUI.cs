using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Image gameOverImage;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Color winColor;
    [SerializeField] private Color loseColor;
    [SerializeField] private Color tieColor;
    [SerializeField] private Button rematchButton;

    void Awake()
    {
        rematchButton.onClick.AddListener(() =>
        {
            GameManager.Instance.RematchRpc();
        });
    }

    void Start()
    {
        Hide();

        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnGameTie += GameManager_OnGameTie;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
    }

    private void GameManager_OnRematch(object sender, EventArgs e)
    {
        Hide();
    }

    private void GameManager_OnGameTie(object sender, EventArgs e)
    {
        gameOverImage.color = tieColor;
        gameOverText.SetText("It's a Tie!");

        Show();
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (e.winPlayerType == GameManager.Instance.GetLocalPlayerType())
        {
            gameOverImage.color = winColor;
            gameOverText.SetText("You Win!");
        }
        else
        {
            gameOverImage.color = loseColor;
            gameOverText.SetText("You Lose!");
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
