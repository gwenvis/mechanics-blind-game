using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDispatcher : MonoBehaviour
{
    public Action<State, Collider2D> OnTrigger;
    public Action<State, Collision2D> OnCollider;
    
    public enum State
    {
        Enter,
        Stay,
        Exit
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        OnTrigger?.Invoke(State.Enter, other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        
        OnTrigger?.Invoke(State.Exit, other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        OnTrigger?.Invoke(State.Stay, other);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        OnCollider?.Invoke(State.Enter, other);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        OnCollider?.Invoke(State.Stay, other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        OnCollider?.Invoke(State.Exit, other);
    }
}
