using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private List<SoundData> playableSounds;
    private  static Dictionary<string, SoundData> _playableSounds;

    private static AudioSource _audioSource;
    private static SoundManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            _audioSource = GetComponent<AudioSource>();
            playableSounds = FindAllSoundTemplates();
            _audioSource = GetComponent<AudioSource>();
            _playableSounds = new Dictionary<string, SoundData>();

            foreach (var sound in playableSounds)
            {
                _playableSounds.Add(sound.name, sound);
            }
            
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    List<SoundData> FindAllSoundTemplates()
    {
        List<SoundData> soundTemplates = new List<SoundData>();

        Resources.LoadAll<SoundData>("SFX");
        foreach (SoundData soundTemplate in Resources.FindObjectsOfTypeAll(typeof(SoundData)) as SoundData[])
        {
            soundTemplates.Add(soundTemplate);
        }

        return soundTemplates;
    }
    
    public static void HandleLocalPlaySound(string name)
    {
        if (_playableSounds.TryGetValue(name, out SoundData sound))
        {
            string soundName = sound.name;
            float overrideVolume = sound.volume;
            
            _audioSource.pitch = sound.GetRandomPitch();
            _audioSource.PlayOneShot(sound.GetRandomClip(),((overrideVolume != -1f) ? overrideVolume : sound.volume));
        }
    } 
}