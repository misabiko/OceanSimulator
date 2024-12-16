using FMOD;
using FMODUnity;
using System;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public class FMODEvents : MonoBehaviour
{
    [field: Header("WaveCrash")]
    [field: SerializeField] public EventReference waveLtoR { get; private set; }
    [field: SerializeField] public EventReference waveRtoL { get; private set; }

    [field: Header("Bird")]
    [field: SerializeField] public EventReference wingFlap { get; private set; }
    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        
        if (instance != null)
        {
            UnityEngine.Debug.LogError("Found more than ne FMOD events instance");
        }
        instance = this;
    }
}
