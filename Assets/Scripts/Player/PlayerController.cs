using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    public bool IsFireOn => _isLightOn;
    
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float _runMultiplier;
    [SerializeField] private AudioSource footStepAudioSource;
    [SerializeField] private AudioSource waterFootstepAudioSource;
    [SerializeField] private AudioSource keyGrabAudioSource;
    [SerializeField] private AudioClip[] footStepAudioClips;
    [SerializeField] private AudioClip[] waterAudioClips;
    [SerializeField] private float footStepInterval = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float waterFootStepChance = 0.1f;

    [Title("Light")] 
    [SerializeField] private LightFlickerEffect _light;
    [SerializeField] private float _fireToggleDelay = 2.0f;
    [SerializeField] private float _mashMaxTime = 1.0f;
    [SerializeField] private float _mashAddTime = 0.2f;
    [SerializeField] private AudioSource _fireOnAudioSource;
    [SerializeField] private AudioSource _fireOffAudioSource;
    [SerializeField] private AudioSource _constantFireSource;
    [SerializeField] private TextMeshProUGUI _lightOnText; // text when light IS on
    [SerializeField] private TextMeshProUGUI _lightOffText; // text when light IS off

    [Title("Stamina")]
    [SerializeField] private float _staminaMaxValue = 10.0f;
    [SerializeField] private float _runningStaminaDrain = 2.0f;
    [SerializeField] private float _minimumRunStamina = 1.0f;
    [SerializeField] private float _lightStaminaDrain = 1.0f;
    [SerializeField] private float _staminaRechargeRate = 4.5f;
    [SerializeField, FormerlySerializedAs("_fireBar")] private UnityEngine.UI.Image _staminaBar;

    private Rigidbody2D _rigidbody;
    private Vector2 _lastFootstep;
    private float _moveAmount;
    private float _stamina;
    private bool _isLightOn = true;
    private bool _hasKey;
    private bool _running = false;
    private float _fireDelay;
    private float _mashTime;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _lastFootstep = transform.position;
        _stamina = _staminaMaxValue;
        _fireDelay = Time.time + _fireToggleDelay;
    }

    private void Update()
    {
        var velocity = new Vector2(
            Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
        );
        
        if(!_running && Input.GetKeyDown(KeyCode.LeftShift) && _stamina > _minimumRunStamina)
        {
            _running = true;
        }
        else if(_running && Input.GetKeyUp(KeyCode.LeftShift))
        {
            _running = false;
        }

        if (velocity != Vector2.zero)
        {
            if (velocity.sqrMagnitude > 1.0f) velocity.Normalize();
            velocity *= walkingSpeed * (_running ? _runMultiplier : 1);
        }

        float staminaDrain = (_running ? _runningStaminaDrain : 0) + (_isLightOn ? _lightStaminaDrain : 0);

        StaminaDrain(staminaDrain);

        if (_stamina < Mathf.Epsilon)
        {
            if(_isLightOn) TurnOffFire();
            if (_running) _running = false;
        }

        
        _staminaBar.transform.localScale = new Vector3(_stamina / _staminaMaxValue, 1, 1);

        LightMash();

        _rigidbody.velocity = (Vector3)velocity;
    }

    private void StaminaDrain(float drain)
    {
        _stamina = Mathf.Clamp(_stamina + (drain > Mathf.Epsilon ? -drain : _staminaRechargeRate) * Time.deltaTime, 0, _staminaMaxValue);
    }

    private void LightMash()
    {
        if(!_isLightOn)
        {
            _mashTime -= Time.deltaTime;
            if (_mashTime < 0)
            {
                _mashTime = 0;
                _light.Scaling = 0;
                _light.SetActive(false);
            }

            if(Input.GetKeyDown(KeyCode.Space) && _stamina > Mathf.Epsilon)
            {
                _mashTime += _mashAddTime;
                _light.SetActive(true);

                if(_mashTime > _mashMaxTime)
                {
                    TurnOnFire();
                }
            }

            if(!_isLightOn) _light.Scaling = _mashTime / (_mashMaxTime * 2);
        }
        else if(Time.time > _fireDelay && Input.GetKeyDown(KeyCode.Space))
        {
            TurnOffFire();
        }
    }

    private void TurnOffFire()
    {
        _fireOffAudioSource.Play();
        _constantFireSource.Pause();
        _light.SetActive(false);
        _isLightOn = false;
        _fireDelay = Time.time + _fireToggleDelay;
        _lightOnText.gameObject.SetActive(false);
        _lightOffText.gameObject.SetActive(true);
    }

    private void TurnOnFire()
    {
        _fireOnAudioSource.Play();
        _constantFireSource.UnPause();
        _light.SetActive(true);
        _isLightOn = true;
        _light.Scaling = 1;
        _fireDelay = Time.time + _fireToggleDelay;
        _lightOnText.gameObject.SetActive(true);
        _lightOffText.gameObject.SetActive(false);
    }

    private void ToggleFire()
    {
        if (_stamina > Mathf.Epsilon && !_isLightOn) TurnOnFire();
        else if(_isLightOn) TurnOffFire();
    }

    private void FixedUpdate()
    {
        _moveAmount += ((Vector2)transform.position - _lastFootstep).magnitude;
        if (_moveAmount > footStepInterval)
        {
            _moveAmount = 0.0f;
            PlayRandomClip(footStepAudioSource, footStepAudioClips);

            if (Random.Range(0.0f, 1.0f) > waterFootStepChance)
                PlayRandomClip(waterFootstepAudioSource, waterAudioClips);
        }

        _lastFootstep = transform.position;
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