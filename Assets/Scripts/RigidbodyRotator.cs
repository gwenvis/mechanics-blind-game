using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Rotates the object towards the direction of the rigidbody
/// </summary>
public class RigidbodyRotator : MonoBehaviour
{
    [SerializeField] private bool _useSmoothing = true;

    [SerializeField, ShowIf(nameof(_useSmoothing))]
    private float _lerpTime = 15.0f;
    
    private Rigidbody2D _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        
        if (_rigidbody) return;
        
        Debug.Log("Could not get Rigidbody on myself!");
        Destroy(this);
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        if (_rigidbody.velocity.magnitude < Mathf.Epsilon) return;

        float currentAngle = Vector2.SignedAngle(Vector2.right, transform.right);
        float wantedAngle = Vector2.SignedAngle(Vector2.right, _rigidbody.velocity);

        if (_useSmoothing)
        {
            Quaternion currentRotation = transform.rotation;
            Quaternion wantedRotation = Quaternion.AngleAxis(wantedAngle, Vector3.forward);
            Quaternion endRotation = Quaternion.Lerp(currentRotation, wantedRotation, _lerpTime * Time.deltaTime);
            _rigidbody.SetRotation(endRotation);
        }
        else
        {
            _rigidbody.SetRotation(wantedAngle);
        }

        _rigidbody.angularVelocity = 0;
    }
}
