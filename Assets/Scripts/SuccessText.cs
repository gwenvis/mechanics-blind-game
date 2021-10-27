using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SuccessText : MonoBehaviour
{
    [Multiline] [SerializeField] private string[] _lines;
    [SerializeField] [Multiline] private string _noText;
    [SerializeField] private TextMeshProUGUI _text;


    private void Start()
    {
        SetText(ScoreManager.CurrentScore >= _lines.Length ? _noText : _lines[ScoreManager.CurrentScore]);
    }

    private void SetText(string text) => _text.text = text;
}
