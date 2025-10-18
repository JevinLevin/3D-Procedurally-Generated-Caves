using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using Sequence = PrimeTween.Sequence;

public class Timer : MonoBehaviour
{
    public static Action OnTimerEnd;
    
    [Header("Components")] 
    [SerializeField] private TextMeshProUGUI text;

    [Header("Attributes")] 
    [SerializeField] private float startTime;
    [SerializeField] private string textFormat = "Timer:\n{0}";
    
    private float time;
    private Sequence timerSequence;

    private void OnEnable()
    {
        GameManager.OnRoundStart += NewTimer;
    }
    private void OnDisable()
    {
        GameManager.OnRoundStart -= NewTimer;
    }

    public void NewTimer()
    {
        time = startTime;
        text.text = string.Format(textFormat, time);

        timerSequence = Sequence.Create(-1).
            ChainDelay(1).
            ChainCallback(UpdateTimer);
    }

    private void UpdateTimer()
    {
        time--;
        text.text = string.Format(textFormat, time);
        
        Tween.PunchScale(text.transform, new ShakeSettings(Vector3.one * 2, 0.2f, 1));
        
        if (time <= 0)
            EndTimer();

    }

    private void EndTimer()
    {
        if(timerSequence.isAlive)
            timerSequence.Complete();
        
        OnTimerEnd?.Invoke();
    }
}
