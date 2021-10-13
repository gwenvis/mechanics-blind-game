using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadPlaySceneButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private bool _resetScore = false;

    private void OnEnable()
    {
        _button.onClick.AddListener(ButtonClicked);
    }

    private void ButtonClicked()
    {
        SceneManager.LoadScene(0);

        if (_resetScore)
        {
            ScoreManager.CurrentScore = 0;
        }
    }
}

public static class ScoreManager
{
    public static int CurrentScore { get; set; } = 0;
}
