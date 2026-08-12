using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int maxHP = 100;
    public int currentHP;
    public int score;
    public int wave;
    public int kills;
    public bool isGameOver;

    public event Action<int, int> OnHPChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnWaveChanged;
    public event Action<int> OnKillChanged;
    public event Action OnGameOver;
    public event Action<float> OnHitStop;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        if (isGameOver) return;
        currentHP = Mathf.Max(0, currentHP - damage);
        OnHPChanged?.Invoke(currentHP, maxHP);
        if (currentHP <= 0)
        {
            isGameOver = true;
            OnGameOver?.Invoke();
        }
    }

    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    public void AddKill()
    {
        kills++;
        OnKillChanged?.Invoke(kills);
    }

    public void NextWave()
    {
        wave++;
        OnWaveChanged?.Invoke(wave);
    }

    public void HitStop(float duration)
    {
        OnHitStop?.Invoke(duration);
    }

    public void Restart()
    {
        currentHP = maxHP;
        score = 0;
        wave = 0;
        kills = 0;
        isGameOver = false;
        OnHPChanged?.Invoke(currentHP, maxHP);
        OnScoreChanged?.Invoke(score);
        OnWaveChanged?.Invoke(wave);
        OnKillChanged?.Invoke(kills);
    }
}
