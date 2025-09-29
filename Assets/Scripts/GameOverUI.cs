using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultTextMesh; // says "You Win!" or "You Lose!" based on result
    [SerializeField] private Color winColor;
    [SerializeField] private Color loseColor;

    private void Start()
    {
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        Hide();
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (e.winningPlayerType == GameManager.Instance.GetLocalPlayerType())
        {
            // is winning player
            resultTextMesh.text = "YOU WIN!";
            resultTextMesh.color = winColor;
            Show();
        }
        else
        {
            // is not winning player
            resultTextMesh.text = "YOU LOSE!";
            resultTextMesh.color = loseColor;
            Show();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}