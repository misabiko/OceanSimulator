using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private float forwardSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private BoatRaycaster boatRaycaster;
    [SerializeField] private BirdController birdController;
    
    private float currentSpeed;
    private Rigidbody myRigidbody;
    private Vector3 forwardMovement;

    private void Awake()
    {
        inputManager.fire += Switch;
    }

    private void OnDestroy()
    {
        inputManager.fire -= Switch;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentSpeed = 0;
        myRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
            PlayerStateManager.SwitchTo(PlayerState.Bird);
        
        Vector2 moveInput = inputManager.moveDirection;
        currentSpeed = Mathf.Clamp(currentSpeed + moveInput.y, -forwardSpeed, forwardSpeed);
        
        transform.Rotate(0,   moveInput.x * rotationSpeed, 0, Space.Self);
        forwardMovement = transform.forward * currentSpeed;
      
        myRigidbody.MovePosition(transform.position + forwardMovement * Time.deltaTime);
    }

    public void Switch()
    {
        if (boatRaycaster.currentSelectedBoid == null)
            return;
        
        birdController.transform.position = boatRaycaster.currentSelectedBoid.transform.position;
        boatRaycaster.currentSelectedBoid = null;
        PlayerStateManager.SwitchTo(PlayerState.Bird);
    }

    private void OnCollisionStay(Collision other)
    {
        if(other.collider.CompareTag("Rocks"))
            currentSpeed = Mathf.Clamp(currentSpeed, -forwardSpeed, 0);
    }
}
