using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public Vector2 moveDirection;
    public float Yaw;
    private InputAction LeftYawAction;

    private PlayerInput myPlayerInput;
    private InputAction RightYawAction;

    private void Awake()
    {
        myPlayerInput = GetComponent<PlayerInput>();
        RightYawAction = myPlayerInput.actions.FindAction("Right Yaw");
        LeftYawAction = myPlayerInput.actions.FindAction("Left Yaw");

        RightYawAction.canceled += OnYawCancel;
        LeftYawAction.canceled += OnYawCancel;
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
        moveDirection = aVector;
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
}