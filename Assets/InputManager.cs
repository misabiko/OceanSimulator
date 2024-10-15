using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private BirdController birdController;
    
    public PlayerInput playerInput;
    public Vector2 moveDirection;

    private void OnEnable()
    {
        /**
        move.started += OnMove;
        move.performed += OnMove;
        move.canceled += OnMove;

        leftYaw.started += OnLeftYaw;
        leftYaw.canceled += OnYawCancel;
        
        RightYaw.started += OnRightYaw;
        RightYaw.canceled += OnYawCancel;
        */
    }

    private void OnDestroy()
    {
        /*move.started -= OnMove;
        move.performed -= OnMove;
        move.canceled -= OnMove;
        
        leftYaw.started -= OnLeftYaw;
        leftYaw.canceled -= OnYawCancel;
        
        RightYaw.started -= OnRightYaw;
        RightYaw.canceled -= OnYawCancel;*/
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Debug.Log("Move");
        moveDirection = context.ReadValue<Vector2>();
    }

    public void OnLeftYaw(InputAction.CallbackContext context)
    {
        birdController.SetYaw(-1f);
    }
    
    public void OnRightYaw(InputAction.CallbackContext context)
    {
        birdController.SetYaw(1f);
    }

    private void OnYawCancel(InputAction.CallbackContext context)
    {
        birdController.SetYaw(0f);
    }
}
