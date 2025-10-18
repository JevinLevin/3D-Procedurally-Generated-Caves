using System;
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
    }
}
