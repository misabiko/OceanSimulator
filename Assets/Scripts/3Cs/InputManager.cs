using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public Vector2 moveDirection;
    public Vector2 lookDirection;

    public float Yaw;

    private PlayerInput myPlayerInput;
    private InputAction RightYawAction;
    private InputAction LeftYawAction;
    public event Action fire;
    private InputAction RowAction;
    public Boolean isMoving;
    private void Awake()
    {
        myPlayerInput = GetComponent<PlayerInput>();
        RightYawAction = myPlayerInput.actions.FindAction("Right Yaw");
        LeftYawAction = myPlayerInput.actions.FindAction("Left Yaw");
        RowAction = myPlayerInput.actions.FindAction("row");
        RowAction.performed += OnPaddle;
        RowAction.started += OnPaddle;
        RowAction.canceled += OffPaddle;

        RightYawAction.canceled += OnYawCancel;
        LeftYawAction.canceled += OnYawCancel;
    }
    private void OnPaddle(InputAction.CallbackContext context)
    {
        isMoving = true;
    }
    private void OffPaddle(InputAction.CallbackContext context)
    {
        isMoving = false;
    }
    private void OnDestroy()
    {
        RightYawAction.canceled -= OnYawCancel;
        LeftYawAction.canceled -= OnYawCancel;
    }
    public void OnMove(InputValue aValue)
    {
        MoveInput(aValue.Get<Vector2>());
    }

    private void MoveInput(Vector2 aVector)
    {
	    if (aVector.sqrMagnitude > 1f)
		    aVector.Normalize();
        moveDirection = aVector;
    }
    
    public void OnLook(InputValue aValue)
    {
        LookInput(aValue.Get<Vector2>());
    }

    public void OnFire(InputValue aValue)
    {
        fire?.Invoke();
    }

    private void LookInput(Vector2 aVector)
    {
        if (aVector.sqrMagnitude > 1f)
            aVector.Normalize();
        lookDirection = aVector;
    }
    
    public void OnLeftYaw(InputValue aValue)
    {
        Yaw = aValue.isPressed ? -1f : 0f;
    }

    public void OnRightYaw(InputValue aValue)
    {
        Yaw = aValue.isPressed ? 1f : 0f;
    }

    private void OnYawCancel(InputAction.CallbackContext obj)
    {
        Yaw = 0;
    }
    
    private void OnGoToBoat(InputValue aValue)
    {
        PlayerStateManager.SwitchTo(PlayerState.Boat);
    }
}