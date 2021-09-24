using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkingSpeed;

    private new Rigidbody2D rigidbody;
    
    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Vector2 velocity = new Vector2(
            Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
        );

        if(velocity.sqrMagnitude > 1.0f) velocity.Normalize();
        velocity *= walkingSpeed;

        transform.position += (Vector3)velocity * Time.deltaTime;
    }
}
