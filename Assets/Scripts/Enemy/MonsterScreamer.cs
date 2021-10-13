using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonsterScreamer : MonoBehaviour
{
    [Title("Components")] [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioSource _musicAudioSource;
    
    [Title("Audio")]
    [SerializeField] private AudioClip[] _gruntAudioClips;
    [SerializeField] private AudioClip[] _screamAudioClips;
    
    [Title("Settings")]
    [SerializeField] private float _timeBetweenGrunt;
    [SerializeField] private float _timeBetweenScream;
    [SerializeField] private bool _alternateGruntAndScreamWhileChasing = true;
    [SerializeField] private float _musicVolume = 0.5f;
    
    private EnemyWalker _enemyWalker;
    private EnemyWalker.State _currentState;
    private bool _alterScream = true; // should I scream?
    private float _nextGruntTime;

    private void Awake()
    {
        _enemyWalker = GetComponent<EnemyWalker>();
    }

    private void OnEnable()
    {
        _enemyWalker.StateChangedEvent += OnEnemyStateChangedEvent;
    }
    
    private void OnDisable()
    {
        _enemyWalker.StateChangedEvent -= OnEnemyStateChangedEvent;
    }

    private void Update()
    {
        AudioClip clip;
        
        if ((_currentState == EnemyWalker.State.Idle || _currentState == EnemyWalker.State.Dwelling) && Time.time > _nextGruntTime)
        {
            clip = PlayRandomAudioClip(_audioSource, _gruntAudioClips);
            _nextGruntTime = Time.time + clip.length + _timeBetweenGrunt;
        }
        else if (_currentState == EnemyWalker.State.Chasing && Time.time > _nextGruntTime)
        {
            var clips = _alterScream ? _screamAudioClips : _gruntAudioClips;
            if (_alternateGruntAndScreamWhileChasing) _alterScream = !_alterScream;
            clip = PlayRandomAudioClip(_audioSource, clips);
            _nextGruntTime = Time.time + clip.length + _timeBetweenScream;
        }
    }

    private AudioClip PlayRandomAudioClip(AudioSource a, AudioClip[] clips)
    {
        a.clip = clips[Random.Range(0, clips.Length)];
        a.Stop();
        a.Play();
        return a.clip;
    }

    private void OnEnemyStateChangedEvent(EnemyWalker.State state)
    {
        _currentState = state;
        
        Debug.Log($"Entered state {_currentState}");
        
        // reset the last grunt time
        _nextGruntTime = 0;
        _alterScream = true;

        _musicAudioSource.volume = state == EnemyWalker.State.Chasing ? _musicVolume : 0;
    }
}
