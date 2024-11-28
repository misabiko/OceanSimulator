using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
   public static AudioManager instance { get; private set;}

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one audio manager in the scene");
        }
        instance = this;
    }

    public void PlayOneShot(EventReference sound, Vector3 wordPos)
    {
        RuntimeManager.PlayOneShot(sound, wordPos);
    }

    public void PlayOneShotWParameters(EventReference sound, Vector3 wordPos, string name, float parameterValue)
    {
        FMOD.Studio.EventInstance eventI = RuntimeManager.CreateInstance(sound);
        eventI.setParameterByName(name, parameterValue);
        eventI.set3DAttributes(wordPos.To3DAttributes());
        eventI.start();
        eventI.release();
    }
}