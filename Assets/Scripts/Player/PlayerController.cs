using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkingSpeed;
    [SerializeField] private AudioSource footStepAudioSource;
    [SerializeField] private AudioSource waterFootstepAudioSource;
    [SerializeField] private AudioClip[] footStepAudioClips;
    [SerializeField] private AudioClip[] waterAudioClips;
    [SerializeField] private float footStepInterval = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float waterFootStepChance = 0.1f;

    private new Rigidbody2D rigidbody;
    private Vector2 lastFootstep;
    private float moveAmount;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        lastFootstep = transform.position;
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

        rigidbody.velocity = (Vector3)velocity;
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

    private void PlayRandomClip(AudioSource audioSource, AudioClip[] clips)
    {
        audioSource.clip = clips[Random.Range(0, clips.Length)];
        audioSource.Stop();
        audioSource.Play();
    }
}