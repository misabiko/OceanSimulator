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
    [SerializeField] private float rotLerpSpeed = 0.1f;
    [SerializeField] private Component rightpaddle;
    [SerializeField] private Component leftpaddle;

    private float currentSpeed;
    private Rigidbody myRigidbody;
    private Vector3 forwardMovement;
    private Quaternion targetRotation;
    
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
        targetRotation = Quaternion.Euler(0, moveInput.x * rotationSpeed, 0) * transform.rotation;

        // Smoothly interpolate towards target rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotLerpSpeed * Time.deltaTime);        forwardMovement = transform.forward * forwardSpeed;
        
        if(Input.GetKeyDown(KeyCode.P))
            Paddle();
    }

    public void Paddle()
    {
        myRigidbody.AddForce(new Vector3(forwardMovement.x,0,forwardMovement.z), ForceMode.Impulse);
        rightpaddle.transform.Rotate(0, 0, -10f);
        leftpaddle.transform.Rotate(0, 0, -10f);
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
