using System;
using System.Collections;
using System.Collections.Generic;
using QTea.MazeGeneration;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

public class EnemyWalker : MonoBehaviour
{
    private enum State
    {
        Idle,
        Dwelling,
        Chasing,
        Stunned
    }

    [Title("Movement")]
    [SerializeField] private float _movementSpeed;
    [SerializeField] [MinMaxSlider(0.0f, 10.0f)]
    private Vector2 _idleTime;
    [SerializeField] private State _startState = State.Idle;

    [Title("Audio")]
    [SerializeField] private AudioSource _footStepAudioSource;
    [SerializeField] private AudioClip[] _footStepClips;
    [SerializeField] private float _footStepMoveInterval = 0.8f;
    private float _moveAmount;

    private State _currentState;

    // pathing
    private EnemyPathFinder _pathFinder;
    private Path<Vector2Int> _path;
    private Vector2 _headingPosition;
    private float _idleEndTime;

    private void Start()
    {
        _pathFinder = GetComponent<EnemyPathFinder>();
    }

    private void Update()
    {
        switch (_currentState)
        {
            case State.Idle:
                UpdateIdleState();
                break;
            case State.Dwelling:
                UpdateMovementState();
                break;
            case State.Chasing:
                UpdateChasingState();
                break;
            case State.Stunned:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateChasingState()
    {
    }

    private void UpdateIdleState()
    {
        if (Time.time > _idleEndTime) EnterState(State.Dwelling);
    }

    private void UpdateMovementState()
    {
        Vector3 currentPosition = transform.position;
        if (((Vector2)currentPosition - _headingPosition).magnitude < 0.001)
        {
            if (!_path.TryNext(out Vector2Int nextPos))
            {
                _headingPosition = Vector2.zero;
                EnterState(State.Idle);
                return;
            }

            _headingPosition = nextPos;
        }

        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, _headingPosition, _movementSpeed * Time.deltaTime);
        Vector2 moveDelta = (Vector2)currentPosition - nextPosition;
        float moveLength = moveDelta.magnitude;
        _moveAmount += moveLength;
        currentPosition = nextPosition;
        transform.position = currentPosition;

        if (_moveAmount > _footStepMoveInterval)
        {
            _moveAmount = 0;
            PlayRandomAudioClip(_footStepAudioSource, _footStepClips);
        }
    }

    private void PlayRandomAudioClip(AudioSource source, AudioClip[] clips)
    {
        source.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        source.Stop();
        source.Play();
    }

    private void EnterState(State state)
    {
        _currentState = state;
        switch (_currentState)
        {
            case State.Idle:
                _idleEndTime = Time.time + UnityEngine.Random.Range(_idleTime.x, _idleTime.y);
                break;
            case State.Dwelling:
                // get a random path
                _path = _pathFinder.GetRandomPath();
                _headingPosition = _path.Peek();
                break;
            case State.Chasing:
                break;
            case State.Stunned:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}