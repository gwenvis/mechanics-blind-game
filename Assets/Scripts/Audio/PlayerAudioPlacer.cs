using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioPlacer : MonoBehaviour
{
    // place in order of north, east, south, west
    [SerializeField] private AudioSource[] audioSourceNESW;
    [SerializeField] private float minDistance = 1;
    [SerializeField] private float offset = 1;
    [SerializeField] private float positionalSmoothing = 7f;
    [SerializeField] private float volumeSmoothing = 12f;
    [SerializeField] private LayerMask layerMask;

    private void LateUpdate()
    {
        UpdateAudioSource(new Vector2(0, 1), audioSourceNESW[0]); // north
        UpdateAudioSource(new Vector2(1, 0), audioSourceNESW[1]); // east
        UpdateAudioSource(new Vector2(0, -1), audioSourceNESW[2]); // south
        UpdateAudioSource(new Vector2(-1, 0), audioSourceNESW[3]); // west
    }

    private void UpdateAudioSource(Vector2 direction, AudioSource audioSource)
    {
        // fire a ray
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Mathf.Infinity, layerMask);
        if (hit)
        {
            // calculate distance from player
            audioSource.volume = Mathf.Lerp(audioSource.volume,
                ((Vector2)transform.position - hit.point).magnitude < minDistance ? 0 : 1,
                volumeSmoothing * Time.deltaTime);
            Vector2 wantedPosition = hit.point - direction * offset;
            audioSource.transform.position = Vector2.Lerp(audioSource.transform.position, wantedPosition,
                positionalSmoothing * Time.deltaTime);
        }
    }
}