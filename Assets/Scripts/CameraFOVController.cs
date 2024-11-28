using Unity.Cinemachine;
using UnityEngine;

public class CameraFOVController : MonoBehaviour
{
    [SerializeField] private BirdController myBirdController;
    [SerializeField] private float defaultFOV = 40f;

    [SerializeField] private float lerpSpeed = 2f;

    private CinemachineVirtualCamera myCinemachineVirtualCamera;
    private float myCurrentFOV;

    private void Awake()
    {
        myCinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    private void Update()
    {
        var valFOV = myBirdController.IsFast() ? defaultFOV + myBirdController.SpeedDifference() : defaultFOV;
        myCurrentFOV = Mathf.Lerp(myCurrentFOV, valFOV, lerpSpeed * Time.deltaTime);
        myCinemachineVirtualCamera.m_Lens.FieldOfView = myCurrentFOV;
    }
}