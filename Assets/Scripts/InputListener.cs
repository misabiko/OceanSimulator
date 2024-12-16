using FMODUnity;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputListener : MonoBehaviour
{
    PlayerInput playerInput;
    private BoatInputAction _inputActions;
    private Boolean isMoving = false;


    [SerializeField] private Vector2 moveInput;
    [SerializeField] private Component rightpaddle;
    [SerializeField] private Component leftpaddle;
    [SerializeField] private float intensity =0f;

    private void Awake()
    {
        _inputActions = new BoatInputAction();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _inputActions.BoatController.Moveforward.performed += onMove;
        _inputActions.BoatController.Moveforward.canceled += stopMove;
        _inputActions.Enable();
        //Test- to remove
// AudioManager.instance.PlayOneShotWParameters(FMODEvents.instance.waveLtoR, this.transform.position,"WaveSize",intensity);
        DSXManager.instance.changeTrigger(1);
    }
    private void onMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Dampen towards the target rotation
        isMoving = true;
        
    }

    private void stopMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Dampen towards the target rotation
        isMoving = false;

    }

    // Update is called once per frame
    void Update()
    {
        transform.position += new Vector3(moveInput.x, 0, moveInput.y) * Time.deltaTime;
        if (isMoving)
        {
            rightpaddle.transform.Rotate(0, 0, 2.5f);
            leftpaddle.transform.Rotate(0, 0, 2.5f);
        }
    }
}
