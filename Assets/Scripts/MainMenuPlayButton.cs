using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class MainMenuPlayButton : MonoBehaviour
{
    [SerializeField] private TimelineAsset _timeline;
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private Button _button;

    private void Start()
    {
        _button.onClick.AddListener(ButtonClickedEvent);
    }

    private void ButtonClickedEvent()
    {
        _button.onClick.RemoveListener(ButtonClickedEvent);
        _button.interactable = false;
        _director.Play(_timeline, DirectorWrapMode.Hold);
    }
}
