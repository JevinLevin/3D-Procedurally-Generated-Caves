using System;
using PrimeTween;
using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    public static Action<int> OnAddScore;

    [Header("Components")] 
    [SerializeField] private TextMeshProUGUI text;

    [Header("Attributes")]
    [SerializeField] private string textFormat = "Score:\n{0}";

    private int score;

    private void Start()
    {
        AddScore(0);
    }

    private void OnEnable()
    {
        OnAddScore += AddScore;
    }
    private void OnDisable()
    {
        OnAddScore -= AddScore;
    }
    

    private void AddScore(int value)
    {
        score += value;
        text.text = string.Format(textFormat, score);
        
        Tween.PunchScale(text.transform, new ShakeSettings(Vector3.one * 2, 0.2f, 1));
    }
}
