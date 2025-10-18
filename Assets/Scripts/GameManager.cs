using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static Action OnGameStart;
    public static Action OnRoundStart;
    public static Action OnRoundEnd;

    [Header("Components")] 
    public CanvasGroup roundEndScreen;
    public Button newRoundButton;
    private void Start()
    {
        StartGame();
    }

    private void OnEnable()
    {
        Timer.OnTimerEnd += EndRound;
    }

    private void OnDisable()
    {
        Timer.OnTimerEnd -= EndRound;
    }

    private void StartGame()
    {
        OnGameStart?.Invoke();
        NewRound();
    }

    private void EndRound()
    {
        newRoundButton.interactable = false;
        Tween.Alpha(roundEndScreen, 1.0f, 0.5f).OnComplete(() => newRoundButton.interactable = true);
        OnRoundEnd?.Invoke();
        Cursor.lockState = CursorLockMode.None;
    }

    public void NewRound()
    {
        roundEndScreen.alpha = 0.0f;
        OnRoundStart?.Invoke();
        Cursor.lockState = CursorLockMode.Locked;
    }
}
