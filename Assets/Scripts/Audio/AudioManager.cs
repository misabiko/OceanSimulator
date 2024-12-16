using UnityEngine;
using FMODUnity;
using System.Collections.Generic;
using System;

public class AudioManager : MonoBehaviour
{
   public static AudioManager instance { get; private set;}
   [SerializeField] private List<string> OutputForHaptics = new List<string>();
   private FMOD.RESULT result;
   private FMOD.ChannelGroup channelGroup;
   private FMOD.Channel channel;
   private FMOD.Sound sound1;
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one audio manager in the scene");
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
        PlayHaptics("Assets/Audio/Haptics/ocean-waves-LtoR.wav");

        instance = this;
    }

    public void PlayOneShot(EventReference sound, Vector3 wordPos)
    {
        RuntimeManager.PlayOneShot(sound, wordPos);
    }

    public void PlayHaptics(String sound) {
        
        result = FMODUnity.RuntimeManager.HapticsSystem.createSound(sound, FMOD.MODE.LOOP_NORMAL, out sound1);
        CheckFMODResult(result, "createSound");
        // Créer un groupe de canaux
        FMODUnity.RuntimeManager.HapticsSystem.createChannelGroup("MyChannelGroup", out channelGroup);
        // Jouer le son en l'assignant au groupe
        result = FMODUnity.RuntimeManager.HapticsSystem.playSound(sound1, channelGroup, false, out channel);
        CheckFMODResult(result, "playSound");
    }

    public void StopHaptics()
    {
        channel.stop();
    }

    public void AdjustHapticsVolume(float vol)
    {
        channel.setVolume(vol);
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

    void Update()
    {
        // Si vous voulez arrêter la boucle après un certain temps ou événement
        if (Input.GetKeyDown(KeyCode.Space))
        {
            channel.stop();
            Debug.Log("Boucle arrêtée.");
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            channel.setVolume(2f);
            Debug.Log("Volume down");
        }
    }
}