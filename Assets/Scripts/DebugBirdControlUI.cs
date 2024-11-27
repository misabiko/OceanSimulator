using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugBirdControlUI : MonoBehaviour
{
    [SerializeField] private TMP_Text forwardVector_txt;
    [SerializeField] private TMP_Text eulerAngles_txt;
    [SerializeField] private TMP_Text momentumFactor_txt;
    [SerializeField] private TMP_Text velocity_txt;
    [SerializeField] private TMP_Text forwardSpeed_txt;
    [SerializeField] private TMP_Text altitude_txt;
    [SerializeField] private GameObject container;
    [SerializeField] private Image filling_img;
    [SerializeField] private BirdController birdController;

    private void Update()
    {
        var t_bird = birdController.transform;
        var eulerAngles = t_bird.eulerAngles;
        var altitude = t_bird.position.y;

        var fillValue = Mathf.Clamp(altitude / 200, 0, 1f);
        filling_img.fillAmount = fillValue;
        altitude_txt.text = Mathf.RoundToInt(altitude).ToString();

        forwardVector_txt.text = "Forward Vector: " + birdController.GetForwardMovement();
        eulerAngles_txt.text = "Euler Angles: " + eulerAngles;
        momentumFactor_txt.text = "Acceleration Multiplier: " + Math.Round(birdController.GetAccelerationMultiplier(), 3, MidpointRounding.AwayFromZero);
        velocity_txt.text = "Velocity: " + birdController.GetVelocity();
        forwardSpeed_txt.text = "Forward Speed: " + birdController.ForwardSpeed();
    }

    public void ToggleContainer()
    {
        container.SetActive(!container.activeSelf);
    }
}