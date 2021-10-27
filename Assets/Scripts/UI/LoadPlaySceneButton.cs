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
    [SerializeField] private int _scene = 0;

    private void OnEnable()
    {
        _button.onClick.AddListener(ButtonClicked);
    }
    
    private void OnDisable()
    {
        _button.onClick.RemoveListener(ButtonClicked);
    }

    private void ButtonClicked()
    {
        SceneManager.LoadScene(_scene);

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
