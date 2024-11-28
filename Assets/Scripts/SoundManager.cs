using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static Dictionary<string, SoundData> _playableSounds;

    private static AudioSource _audioSource;
    private static SoundManager Instance;
    [SerializeField] private List<SoundData> playableSounds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            playableSounds = FindAllSoundTemplates();
            _audioSource = GetComponent<AudioSource>();
            _playableSounds = new Dictionary<string, SoundData>();

            foreach (var sound in playableSounds) _playableSounds.Add(sound.name, sound);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private List<SoundData> FindAllSoundTemplates()
    {
        var soundTemplates = new List<SoundData>();

        Resources.LoadAll<SoundData>("SFX");
        foreach (var soundTemplate in Resources.FindObjectsOfTypeAll(typeof(SoundData)) as SoundData[])
            soundTemplates.Add(soundTemplate);

        return soundTemplates;
    }

    public static void HandleLocalPlaySound(string name)
    {
        if (_playableSounds.TryGetValue(name, out var sound))
        {
            var soundName = sound.name;
            var overrideVolume = sound.volume;

            _audioSource.pitch = sound.GetRandomPitch();
            _audioSource.PlayOneShot(sound.GetRandomClip(), overrideVolume != -1f ? overrideVolume : sound.volume);
        }
    }
}