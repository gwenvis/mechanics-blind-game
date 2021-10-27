using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WarnUI : MonoBehaviour
{
    [SerializeField] private Transform _warnText;
    [SerializeField] private float _startSize = 1.0f;
    [SerializeField] private float _endSize = 1.2f;
    [SerializeField] private float _duration = 2.0f;
    [SerializeField] private float _alphaDuration = 0.3f;
    [SerializeField] private float _fadeOutStart = 1.8f;

    private Coroutine _coroutine;

    private void Awake()
    {
        _warnText.gameObject.SetActive(false);
    }

    public void StartWarn()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        
        _coroutine = StartCoroutine(WarnCoroutine());
    }

    private IEnumerator WarnCoroutine()
    {
        var text = _warnText.GetComponent<TextMeshProUGUI>();
        _warnText.gameObject.SetActive(true);
        float elapsed = 0;
        
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float size = _warnText.localScale.x;
            float wantedSize = Mathf.Lerp(_startSize, _endSize, elapsed / _duration);

            _warnText.localScale = new Vector3(wantedSize, wantedSize, 1);
            if (elapsed < _fadeOutStart) text.alpha = Mathf.Lerp(0, 1, elapsed / _alphaDuration);
            else text.alpha = Mathf.Lerp(1, 0, (elapsed - _fadeOutStart) / (_duration - _fadeOutStart));
            yield return new WaitForEndOfFrame();
        }

        _warnText.gameObject.SetActive(false);
        _coroutine = null;
    }
}
