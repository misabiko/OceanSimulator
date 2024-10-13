using Cinemachine;
using UnityEngine;

public class CameraFOVController : MonoBehaviour
{
    [SerializeField] private BirdController myBirdController;
    [SerializeField] private float defaultFOV = 40f;
    
    private CinemachineVirtualCamera myCinemachineVirtualCamera;

    [SerializeField] private float lerpSpeed = 2f;
    private float myCurrentFOV;
    
    private void Awake()
    {
        myCinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    private void Update()
    {
        float valFOV = myBirdController.IsFast() ? defaultFOV + myBirdController.SpeedDifference() : defaultFOV;
        myCurrentFOV = Mathf.Lerp(myCurrentFOV, valFOV, lerpSpeed * Time.deltaTime);
        myCinemachineVirtualCamera.m_Lens.FieldOfView = myCurrentFOV;
    }
}
