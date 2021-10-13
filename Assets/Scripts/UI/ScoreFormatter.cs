using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreFormatter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    
    // Start is called before the first frame update
    void Awake()
    {
        _text.text = string.Format(_text.text, ScoreManager.CurrentScore);
    }
}
