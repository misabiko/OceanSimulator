using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private float forwardSpeed;
    [SerializeField] private float rotationSpeed;

    private Rigidbody myRigidbody;
    private Vector3 forwardMovement;

    // Start is called before the first frame update
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
            PlayerStateManager.SwitchTo(PlayerState.Bird);
        
        Vector2 moveInput = inputManager.moveDirection;
        transform.Rotate(0,   moveInput.x * rotationSpeed, 0, Space.Self);
        forwardMovement = transform.forward * forwardSpeed;
      
        myRigidbody.MovePosition(transform.position + forwardMovement * Time.deltaTime);
    }
}
