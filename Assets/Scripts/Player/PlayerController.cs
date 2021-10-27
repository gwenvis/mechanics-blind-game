using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    public bool IsFireOn => _fireOn;
    
    [SerializeField] private float walkingSpeed;
    [SerializeField] private AudioSource footStepAudioSource;
    [SerializeField] private AudioSource waterFootstepAudioSource;
    [SerializeField] private AudioSource keyGrabAudioSource;
    [SerializeField] private AudioClip[] footStepAudioClips;
    [SerializeField] private AudioClip[] waterAudioClips;
    [SerializeField] private float footStepInterval = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float waterFootStepChance = 0.1f;

    [Title("Fire")] [SerializeField] private Light2D _light;
    [SerializeField] private float _fireDuration = 10.0f;
    [SerializeField] private float _fireRechargeRatio = 2.0f;
    [SerializeField] private float _fireToggleDelay = 2.0f;
    [SerializeField] private AudioSource _fireOnAudioSource;
    [SerializeField] private AudioSource _fireOffAudioSource;
    [SerializeField] private AudioSource _constantFireSource;
    [SerializeField] private UnityEngine.UI.Image _fireBar;

    private new Rigidbody2D rigidbody;
    private Vector2 lastFootstep;
    private float moveAmount;
    private float _currentFireValue;
    private bool _fireOn = true;
    private bool _hasKey;
    private float _fireDelay;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        lastFootstep = transform.position;
        _currentFireValue = _fireDuration;
        _fireDelay = Time.time + _fireToggleDelay;
    }

    private void Update()
    {
        var velocity = new Vector2(
            Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
        );

        if (velocity != Vector2.zero)
        {
            if (velocity.sqrMagnitude > 1.0f) velocity.Normalize();
            velocity *= walkingSpeed;
        }

        _currentFireValue = Mathf.Clamp(_currentFireValue + (_fireOn ? -Time.deltaTime : Time.deltaTime * _fireRechargeRatio), 0, _fireDuration);

        if (_fireOn && _currentFireValue < Mathf.Epsilon)
        {
            TurnOffFire();
        }
        
        _fireBar.transform.localScale = new Vector3(_currentFireValue / _fireDuration, 1, 1);

        if (Input.GetKeyDown(KeyCode.E) && Time.time > _fireDelay)
        {
            ToggleFire();
        }

        rigidbody.velocity = (Vector3)velocity;
    }

    private void TurnOffFire()
    {
        _fireOffAudioSource.Play();
        _constantFireSource.Pause();
        _light.enabled = false;
        _fireOn = false;
        _fireDelay = Time.time + _fireToggleDelay;
    }

    private void TurnOnFire()
    {
        _fireOnAudioSource.Play();
        _constantFireSource.UnPause();
        _light.enabled = true;
        _fireOn = true;
        _fireDelay = Time.time + _fireToggleDelay;
    }

    private void ToggleFire()
    {
        if (_currentFireValue > Mathf.Epsilon && !_fireOn) TurnOnFire();
        else if(_fireOn) TurnOffFire();
    }

    private void FixedUpdate()
    {
        moveAmount += ((Vector2)transform.position - lastFootstep).magnitude;
        if (moveAmount > footStepInterval)
        {
            moveAmount = 0.0f;
            PlayRandomClip(footStepAudioSource, footStepAudioClips);

            if (Random.Range(0.0f, 1.0f) > waterFootStepChance)
                PlayRandomClip(waterFootstepAudioSource, waterAudioClips);
        }

        lastFootstep = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Trapdoor") && _hasKey)
        {
            ScoreManager.CurrentScore++;
            SceneManager.LoadScene(3);
        }
        else if (other.gameObject.CompareTag("Key"))
        {
            keyGrabAudioSource.Play();
            _hasKey = true;
            Destroy(other.gameObject);
        }
    }

    private void PlayRandomClip(AudioSource audioSource, AudioClip[] clips)
    {
        audioSource.clip = clips[Random.Range(0, clips.Length)];
        audioSource.Stop();
        audioSource.Play();
    }
}