using System;
using UnityEngine;

/// Tracks the player's score during a Last Light run.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int currentScore;

    public event Action<int> ScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ScoreChanged?.Invoke(currentScore);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddScore(int pointsToAdd)
    {
        if (pointsToAdd <= 0)
            return;

        currentScore += pointsToAdd;
        ScoreChanged?.Invoke(currentScore);
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public void ResetScore()
    {
        currentScore = 0;
        ScoreChanged?.Invoke(currentScore);
    }
}