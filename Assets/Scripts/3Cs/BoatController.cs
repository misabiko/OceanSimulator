using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    [SerializeField] private bool isUnderWater = false;
    [SerializeField] private int currentTrigger = 2;
    [SerializeField] private PlayerStateComponent state;
    private bool hasPlayedSound = false;


    private void Awake()
    {
        state.OnActivate += OnActivate;
        state.OnDeactivate += OnDeactivate;

        inputManager.fire += Switch;
    }

    private void OnDestroy()
    {
        state.OnActivate -= OnActivate;
        state.OnDeactivate -= OnDeactivate;

        inputManager.fire -= Switch;
    }

    private void OnActivate()
    {
        PlayerInput playerInput =  GetComponent<PlayerInput>();
        playerInput.enabled = true;
        playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
        playerInput.SwitchCurrentControlScheme(Gamepad.current);
    }

    
    private void OnDeactivate()
    {
        GetComponent<PlayerInput>().enabled = false;
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
	    if (AudioManager.instance == null)
		    Debug.LogError("AudioManager is missing in the scene");
        if (Input.GetKeyDown(KeyCode.M))
            PlayerStateManager.SwitchTo(PlayerState.Bird);

        Vector2 moveInput = inputManager.moveDirection;
        targetRotation = Quaternion.Euler(0, moveInput.x * rotationSpeed, 0) * transform.rotation;

        // Smoothly interpolate towards target rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotLerpSpeed * Time.deltaTime); 
        forwardMovement = transform.forward * forwardSpeed;

        if (moveInput.x < -0.1f)
        {
            AudioManager.instance.PlayHaptics("left");
            AudioManager.instance.AdjustHapticsVolume(NormalizeValueForHaptics(moveInput.x));
        }
        else if (moveInput.x > 0.1f)
        {
            AudioManager.instance.PlayHaptics("right");
            AudioManager.instance.AdjustHapticsVolume(NormalizeValueForHaptics(moveInput.x));
        }
        else { 
            AudioManager.instance.StopHaptics();
        }
        if (Input.GetKeyDown(KeyCode.P))
            Paddle();

        if (inputManager.isMoving)
        {
            rightpaddle.transform.Rotate(0, 0, -10f);
            leftpaddle.transform.Rotate(0, 0, -10f);
            float zRotation = rightpaddle.transform.localEulerAngles.z;
            if (zRotation <= 75f && zRotation >= -75f) 
            {
                //j'aimrai juste declncehr �a une fois, quand on passe en dessous de 75 deg
                if(!hasPlayedSound)AudioManager.instance.PlayOneShot(FMODEvents.instance.rowSound, this.transform.position);
                hasPlayedSound = true;

                isUnderWater = true;
                myRigidbody.AddForce(new Vector3(forwardMovement.x, 0, forwardMovement.z), ForceMode.VelocityChange);
                // Debug.Log("IMPULSE: "+new Vector3(forwardMovement.x, 0, forwardMovement.z));

            }
            else
            {
                hasPlayedSound = false;
                isUnderWater = false;
            }

            currentTrigger = isUnderWater ? 1 : 2;
            if (DSXManager.instance.currentTrigger != currentTrigger)
            {
                DSXManager.instance.changeTrigger(currentTrigger);
            }
        }
    }

    public void Paddle()
    {
        myRigidbody.AddForce(new Vector3(forwardMovement.x, 0, forwardMovement.z), ForceMode.Impulse);
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
        if (other.collider.CompareTag("Rocks"))
            currentSpeed = Mathf.Clamp(currentSpeed, -forwardSpeed, 0);
    }

    private float NormalizeValueForHaptics(float value)
    {
        value = Mathf.Abs(value);
        float minInput = 0.1f;
        float maxInput = 1f;
        float minOutput = 0f;
        float maxOutput = 1f;

        // Clamp pour limiter la valeur � la plage
        value = Mathf.Clamp(value, minInput, maxInput);

        // Normalisation
        return minOutput + (value - minInput) * (maxOutput - minOutput) / (maxInput - minInput);
    }
}