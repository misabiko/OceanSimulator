using UnityEngine;

public class PlayerStateComponent : MonoBehaviour
{
    [SerializeField] private PlayerState type;
    [SerializeField] private GameObject controller;
    private void Awake()
    {
        PlayerStateManager.OnStateChange += SetObjectRelativeToState;
    }

    private void OnDestroy()
    {
        PlayerStateManager.OnStateChange -= SetObjectRelativeToState;
    }

    private void SetObjectRelativeToState(PlayerState aState)
    {
        controller.SetActive(aState == type);
        Cursor.lockState = aState == PlayerState.Boat ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
