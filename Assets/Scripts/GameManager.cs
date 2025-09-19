using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition; // Event listened to by GameVisualManager
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
    } // pass in x and y with EventArgs


    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Error: More than one {name}");
        }

        Instance = this;
    }

    public void ClickedOnGridPosition(int x, int y)
    {
        Debug.Log("Clicked On " + x + ", " + y);
        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
        });
    }
}
