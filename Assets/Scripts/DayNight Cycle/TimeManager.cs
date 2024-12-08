using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TimeSettings timeSettings;
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private AnimationCurve lightIntensityCurve;
    [SerializeField] private float maxSunIntensity = 1;
    [SerializeField] private float maxMoonIntensity = 0.5f;
    [SerializeField] private Color dayAmbientLight;
    [SerializeField] private Color nightAmbientLight;
    [SerializeField] private Color fogDayColour;
    [SerializeField] private Color fogNightColour;
    [SerializeField] private Volume volume;
    [SerializeField] private Material skyboxMaterial;
    
    private ColorAdjustments colorAdjustments;
    
    private TimeService timeService;

    private void Start()
    {
        timeService = new TimeService(timeSettings);
        volume.profile.TryGet(out colorAdjustments);
    }

    private void Update()
    {
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();
        UpdateSkyBlend();
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            timeSettings.timeMultiplier *= 2;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            timeSettings.timeMultiplier /= 2;
        }
    }

    private void UpdateSkyBlend()
    {
        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.up);
        float blend = Mathf.Lerp(0, 1, lightIntensityCurve.Evaluate(dotProduct));
        skyboxMaterial.SetFloat("_CubemapTransition", blend);
    }

    private void UpdateLightSettings()
    {
        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.down);
        sun.intensity = Mathf.Lerp(0, maxSunIntensity, lightIntensityCurve.Evaluate(dotProduct));
        moon.intensity = Mathf.Lerp(0, maxMoonIntensity, lightIntensityCurve.Evaluate(dotProduct));

        if (colorAdjustments == null)
            return;

        colorAdjustments.colorFilter.value = Color.Lerp(nightAmbientLight, dayAmbientLight, lightIntensityCurve.Evaluate(dotProduct));
        RenderSettings.fogColor =
            Color.Lerp(fogNightColour, fogDayColour, lightIntensityCurve.Evaluate(dotProduct));
    }

    private void RotateSun()
    {
        float rotation = timeService.CalculateSunAngle();
        sun.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.right);
    }

    private void UpdateTimeOfDay()
    {
        timeService.UpdateTime(Time.deltaTime);
    }
}
