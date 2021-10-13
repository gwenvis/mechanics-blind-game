using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageFadeOut : MonoBehaviour
{
    [SerializeField] private RawImage _image;
    [SerializeField] private float _fadeOutStartTime = 0.6f;
    [SerializeField] private float _fadeOutDuration = 1.0f;

    private int _phase = 0;
    private float _nextPhaseTime;

    private void Start()
    {
        _nextPhaseTime = Time.time + _fadeOutDuration;
    }

    private void Update()
    {
        if (_phase > 1) return;
        
        if (_phase == 1)
        {
            Color color = _image.color;
            Color goal = Color.clear;
            color.a = Mathf.MoveTowards(color.a, goal.a, _fadeOutDuration * Time.deltaTime);
            if (color.a == goal.a) _phase = 2;
            _image.color = color;
            return;
        }
        
        if (Time.time < _nextPhaseTime) return;

        _phase = 1;
    }
}