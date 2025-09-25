using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject crossArrowGameObject;
    [SerializeField] private GameObject circleArrowGameObject;
    [SerializeField] private GameObject crossYouTextGameObject;
    [SerializeField] private GameObject circleYouTextGameObject;

    
    private void Awake()
    {
        crossArrowGameObject.SetActive(false);
        circleArrowGameObject.SetActive(false);
        crossYouTextGameObject.SetActive(false);
        circleYouTextGameObject.SetActive(false);
    }


    private void Start()
    {
        GameManager.Instance.OnGameStarted += GameManager_OnGameStarted;
        GameManager.Instance.OnCurrentPlayablePlayerTypeChange += GameManager_OnCurrentPlayablePlayerTypeChange;
    }


    /// <summary>
    /// setup correct "you" text when game started. also initiate currentArrow
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void GameManager_OnGameStarted(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.GetLocalPlayerType() == GameManager.PlayerType.Cross)
        {
            crossYouTextGameObject.SetActive(true);
        }
        else
        {
            circleYouTextGameObject.SetActive(true);
        }

        UpdateCurrentArrow();
    }


    /// <summary>
    /// updates ui arrow when player turn changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void GameManager_OnCurrentPlayablePlayerTypeChange(object sender, System.EventArgs e)
    {
        UpdateCurrentArrow();
    }


    /// <summary>
    /// updates playerUI current player arrow based on GameManager.localPlayerType (network variable)
    /// </summary>
    private void UpdateCurrentArrow()
    {
        if (GameManager.Instance.GetCurrentPlayablePlayerType() == GameManager.PlayerType.Cross)
        {
            crossArrowGameObject.SetActive(true);
            circleArrowGameObject.SetActive(false);
        }
        else
        {
            crossArrowGameObject.SetActive(false);
            circleArrowGameObject.SetActive(true);
        }
    }
}
