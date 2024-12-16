using UnityEngine;
using FMODUnity;
using System.Collections.Generic;
using System;
using FMOD;
using Unity.VisualScripting;
using System.Dynamic;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
   public static AudioManager instance { get; private set;}
   [SerializeField] private List<string> OutputForHaptics = new List<string>();
    private FMOD.RESULT result;
    private FMOD.ChannelGroup channelGroup;
    private FMOD.Channel channel;
    private FMOD.Sound turningLeft;
    private FMOD.Sound turningRight;
    private FMOD.Sound birdDiving;
    private bool isActivated=false;
    private FMOD.Studio.EventInstance ambienceEvent;
    private void Awake()
    {
        if (instance != null)
        {
            UnityEngine.Debug.LogError("More than one audio manager in the scene");
        }

        var resGetDrivers_core = FMODUnity.RuntimeManager.CoreSystem.getNumDrivers(out int totalDrivers);
        var resGetDrivers_haptic = FMODUnity.RuntimeManager.HapticsSystem.getNumDrivers(out int totalDriversSys2);
        var resSysInit_haptics = FMODUnity.RuntimeManager.HapticsSystem.init(4, FMOD.INITFLAGS.NORMAL, (System.IntPtr)0);

        for (int i = 0; i < totalDrivers; i++)
        {
            String driverName;
            Guid guid;
            int systemRate;
            FMOD.SPEAKERMODE speakerMode;
            int speakerModeChannels;
            FMODUnity.RuntimeManager.CoreSystem.getDriverInfo(i, out driverName, 256, out guid, out systemRate, out speakerMode, out speakerModeChannels);
            UnityEngine.Debug.Log($"Driver {i}: {driverName} - Mode: {speakerMode} - Rate: {systemRate}Hz");
            OutputForHaptics.Add( driverName );
        }
        result = FMODUnity.RuntimeManager.HapticsSystem.setDriver(2);
        CheckFMODResult(result, "setDriver");
        result = FMODUnity.RuntimeManager.HapticsSystem.createSound("Assets/Audio/Haptics/LeftRota.wav", FMOD.MODE.LOOP_NORMAL, out turningLeft);
        CheckFMODResult(result, "createSound");
        result = FMODUnity.RuntimeManager.HapticsSystem.createSound("Assets/Audio/Haptics/WindBirdDive.wav", FMOD.MODE.LOOP_NORMAL, out birdDiving);
        CheckFMODResult(result, "createSound");
        result = FMODUnity.RuntimeManager.HapticsSystem.createSound("Assets/Audio/Haptics/RightRota.wav", FMOD.MODE.LOOP_NORMAL, out turningRight);
        CheckFMODResult(result, "createSound");
        FMODUnity.RuntimeManager.HapticsSystem.createChannelGroup("MyChannelGroup", out channelGroup);
        instance = this;
    }

    private void Start()
    {
        initialiseAmbience(FMODEvents.instance.ambience);
    }

    private void initialiseAmbience(EventReference ambience)
    {
        ambienceEvent = RuntimeManager.CreateInstance(ambience);
        result=ambienceEvent.set3DAttributes(this.transform.position.To3DAttributes());
        CheckFMODResult(result, "Start");
        result = ambienceEvent.start();
        CheckFMODResult(result, "Start");
    }

    

    public void PlayOneShot(EventReference sound, Vector3 wordPos)
    {
        RuntimeManager.PlayOneShot(sound, wordPos);
    }

    public void PlayHaptics(String sound) {
        if(isActivated) {
            FMOD.Sound playing = birdDiving;
            switch (sound)
            {
                case "left": playing = turningLeft;
                    break;
                case "right":
                    playing = turningRight;
                    break;
            }
            result = FMODUnity.RuntimeManager.HapticsSystem.playSound(playing, channelGroup, false, out channel);
            CheckFMODResult(result, "playSound");
        }
        isActivated = true;
    }

    public void StopHaptics()
    {
        if(channel.hasHandle())
        {
            result = channelGroup.setVolume(1f);
            CheckFMODResult(result, "reset volume");
            result = channelGroup.stop();
            CheckFMODResult(result, "stop");
        }
        isActivated = false;
    }

    public void AdjustHapticsVolume(float vol)
    {
        channelGroup.setVolume(vol);
    }


    public void PlayOneShotWParameters(EventReference sound, Vector3 wordPos, string name, float parameterValue)
    {
        FMOD.Studio.EventInstance eventI = RuntimeManager.CreateInstance(sound);
        eventI.setParameterByName(name, parameterValue);
        eventI.set3DAttributes(wordPos.To3DAttributes());
        eventI.start();
        eventI.release();
    }

    private void CheckFMODResult(FMOD.RESULT result, string operation)
    {
        if (result != FMOD.RESULT.OK)
        {
            UnityEngine.Debug.LogError($"FMOD error during {operation}: {result} - {FMOD.Error.String(result)}");
        }
        else
        {
            UnityEngine.Debug.Log($"Operation {operation} succeeded.");
        }
    }

    private void OnDestroy()
    {
        ambienceEvent.release();
    }

}