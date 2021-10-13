using System;
using System.Collections;
using System.Collections.Generic;
using QTea.MazeGeneration;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class EnemyWalker : MonoBehaviour
{
    public event Action<State> StateChangedEvent;
    public event Action PlayerHitEvent;

    public enum State
    {
        Idle,
        Dwelling,
        Chasing,
        Stunned
    }

    [Title("Player")] 
    [SerializeField] private GameObject _player;
    [SerializeField] private LayerMask _collisionMask;

    [Title("Movement")]
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _runningMovementSpeed;
    [SerializeField] [MinMaxSlider(0.0f, 10.0f)]
    private Vector2 _idleTime;
    [SerializeField] private State _startState = State.Idle;

    [Title("Audio")]
    [SerializeField] private AudioSource _footStepAudioSource;
    [SerializeField] private AudioSource _closeFootstepAudioSource;
    [SerializeField] private AudioClip[] _footStepClips;
    [FormerlySerializedAs("_closeFootSteps")] [SerializeField] private AudioClip[] _closeFootStepClips;
    [SerializeField] private float _footStepMoveInterval = 0.8f;

    [Title("Audio Volume")] 
    [SerializeField] private AnimationCurve _farFootStepVolume;
    [SerializeField] private AnimationCurve _closeFootStepVolume;

    private float _moveAmount;

    private State _currentState;

    // pathing
    private EnemyPathFinder _pathFinder;
    private Path<Vector2Int> _path;
    private Vector2 _headingPosition;
    private float _idleEndTime;
    
    // debugging
    private Vector2 _debugDirection;
    private float _debugRaycastLength;

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
                SearchForPlayer();
                break;
            case State.Dwelling:
                UpdateMovementState();
                SearchForPlayer();
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
        UpdatePathMovement(_runningMovementSpeed, State.Stunned);
    }

    private void SearchForPlayer()
    {
        // fire a raycast to the player
        Vector2 direction =  _player.transform.position - transform.position;
        _debugDirection = direction;
        _debugRaycastLength = Mathf.Infinity;
        
        RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, direction, Mathf.Infinity, _collisionMask);
        if (!raycastHit) return;
        
        _debugRaycastLength = raycastHit.distance;
        if(raycastHit.transform == _player.transform) EnterState(State.Chasing);
    }

    private void UpdateIdleState()
    {
        if (Time.time > _idleEndTime) EnterState(State.Dwelling);
    }

    private void UpdateMovementState()
    {
        UpdatePathMovement(_movementSpeed, State.Idle);
    }

    private void UpdatePathMovement(float speed, State noPathState)
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

        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, _headingPosition, speed * Time.deltaTime);
        Vector2 moveDelta = (Vector2)currentPosition - nextPosition;
        float moveLength = moveDelta.magnitude;
        _moveAmount += moveLength;
        currentPosition = nextPosition;
        transform.position = currentPosition;

        if (!(_moveAmount > _footStepMoveInterval)) return;
        
        _moveAmount = 0;
        float dist = (_player.transform.position - currentPosition).magnitude;
        PlayRandomAudioClip(_footStepAudioSource, _footStepClips, _farFootStepVolume.Evaluate(dist));
        PlayRandomAudioClip(_closeFootstepAudioSource, _closeFootStepClips, _closeFootStepVolume.Evaluate(dist));
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.transform != _player.transform) return;

        //PlayerHitEvent?.Invoke();
        SceneManager.LoadScene(1);
    }

    private void PlayRandomAudioClip(AudioSource source, AudioClip[] clips, float volume)
    {
        source.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        source.volume = volume;
        source.Stop();
        source.Play();
    }

    private bool IsPathValid(Path<Vector2Int> path)
    {
        return path.Count > 1;
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
                if (IsPathValid(_path))
                {
                    _headingPosition = _path.Peek();
                }
                else
                {
                    EnterState(state);
                }
                break;
            case State.Chasing:
                _path = _pathFinder.GetPathTo(_player.transform.position);
                if (IsPathValid(_path))
                {
                    _headingPosition = _path.Peek();
                }
                else
                {
                    EnterState(state);
                }
                break;
            case State.Stunned:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        StateChangedEvent?.Invoke(state);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, _debugDirection.normalized * _debugRaycastLength);
    }
}