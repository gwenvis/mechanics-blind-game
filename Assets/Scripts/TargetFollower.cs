using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetFollower : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private Transform _transform;

    private void Start()
    {
        _transform = transform;
    }

    private void LateUpdate()
    {
        _transform.position = _target.position;
    }
}
