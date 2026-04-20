using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : SingletonComponent<SoundManager>
{
    #region Classes

    [Serializable]
    private class SoundInfo
    {
        public string id = "";
        public List<AudioClip> audioClips = null;
        public SoundType type = SoundType.SoundEffect;
        public bool playAndLoopOnStart = false;

        [Range(0, 1)] public float clipVolume = 1;

        public bool isPitchVariable = false;
        [Range(0f, 3f)] public float minVarPitch = 0f;
        [Range(0f, 3f)] public float maxVarPitch = 1f;
        [Range(0f, 3f)] public float fixedPitch = 1f;
    }

    private class PlayingSound
    {
        public SoundInfo soundInfo = null;
        public AudioSource audioSource = null;
    }

    #endregion

    #region Enums

    public enum SoundType
    {
        SoundEffect,
        Music,
    }

    #endregion

    #region Inspector Variables

    [SerializeField] private string defaultThemeId;
    [SerializeField] private List<SoundInfo> soundInfos = null;

    #endregion

    #region Member Variables

    private List<PlayingSound> playingAudioSources = new();
    private List<PlayingSound> loopingAudioSources = new();

    private bool isFocused = false;
    private bool isPaused = false;

    #endregion

    #region Properties

    public bool IsMusicOn { get; private set; }
    public bool IsSoundEffectsOn { get; private set; }
    public bool IsInitialized { get; private set; }

    #endregion

    #region Unity Methods

    private void Start()
    {
        InitSave();
        IsInitialized = true;
    }

    private void Update()
    {
        for (int i = 0; i < playingAudioSources.Count; i++)
        {
            AudioSource audioSource = playingAudioSources[i].audioSource;

            if (!audioSource.isPlaying && (isFocused || !isPaused))
            {
                Destroy(audioSource.gameObject);
                playingAudioSources.RemoveAt(i);
                i--;
            }
        }
    }

    #endregion

    #region Public Methods

    public void Play(string id)
    {
        Play(id, false, 0);
    }

    public void Play(string id, bool loop, float playDelay)
    {
        SoundInfo soundInfo = GetSoundInfo(id);
        if (soundInfo == null) return;

        if ((soundInfo.type == SoundType.Music && !IsMusicOn) ||
            (soundInfo.type == SoundType.SoundEffect && !IsSoundEffectsOn))
        {
            return;
        }

        AudioSource audioSource = CreateAudioSource(id);

        audioSource.clip = soundInfo.audioClips.Count > 1 ? RandomizeSound(soundInfo.audioClips) : soundInfo.audioClips[0];
        audioSource.loop = loop;
        audioSource.time = 0;
        audioSource.volume = soundInfo.clipVolume;
        audioSource.pitch = soundInfo.isPitchVariable ? RandomizePitch(soundInfo.minVarPitch, soundInfo.maxVarPitch) : soundInfo.fixedPitch;

        if (playDelay > 0)
            audioSource.PlayDelayed(playDelay);
        else
            audioSource.Play();

        var playingSound = new PlayingSound { soundInfo = soundInfo, audioSource = audioSource };

        if (loop)
            loopingAudioSources.Add(playingSound);
        else
            playingAudioSources.Add(playingSound);
    }

    public void Stop(string id)
    {
        StopAllSounds(id, playingAudioSources);
        StopAllSounds(id, loopingAudioSources);
    }

    public void Stop(SoundType type)
    {
        StopAllSounds(type, playingAudioSources);
        StopAllSounds(type, loopingAudioSources);
    }

    public void SetSoundTypeOnOff(SoundType type, bool isOn)
    {
        switch (type)
        {
            case SoundType.SoundEffect:
                if (isOn == IsSoundEffectsOn) return;
                IsSoundEffectsOn = isOn;
                break;
            case SoundType.Music:
                if (isOn == IsMusicOn) return;
                IsMusicOn = isOn;
                break;
        }

        if (!isOn)
        {
            Stop(type);
        }
        else
        {
            if (type == SoundType.Music)
            {
                Play(defaultThemeId, true, 0);
            }
        }
        Save();
    }

    #endregion

    #region Private Methods

    private void StopAllSounds(string id, List<PlayingSound> playingSounds)
    {
        for (int i = 0; i < playingSounds.Count; i++)
        {
            if (id == playingSounds[i].soundInfo.id)
            {
                playingSounds[i].audioSource.Stop();
                Destroy(playingSounds[i].audioSource.gameObject);
                playingSounds.RemoveAt(i);
                i--;
            }
        }
    }

    private void StopAllSounds(SoundType type, List<PlayingSound> playingSounds)
    {
        for (int i = 0; i < playingSounds.Count; i++)
        {
            if (type == playingSounds[i].soundInfo.type)
            {
                playingSounds[i].audioSource.Stop();
                Destroy(playingSounds[i].audioSource.gameObject);
                playingSounds.RemoveAt(i);
                i--;
            }
        }
    }

    private SoundInfo GetSoundInfo(string id)
    {
        return soundInfos.Find(x => x.id == id);
    }

    private AudioSource CreateAudioSource(string id)
    {
        GameObject obj = new GameObject("sound_" + id);
        obj.transform.SetParent(transform);
        return obj.AddComponent<AudioSource>();
    }

    private float RandomizePitch(float min, float max)
    {
        return UnityEngine.Random.Range(min, max);
    }

    private AudioClip RandomizeSound(List<AudioClip> clips)
    {
        return clips[UnityEngine.Random.Range(0, clips.Count)];
    }

    #endregion

    #region Save Methods

    private void InitSave()
    {
        if (!PlayerPrefs.HasKey("IsMusicOn"))
        {
            IsMusicOn = true;
            IsSoundEffectsOn = true;
            Save();
        }
        else
        {
            Load();
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt("IsMusicOn", IsMusicOn ? 1 : 0);
        PlayerPrefs.SetInt("IsSoundEffectsOn", IsSoundEffectsOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        IsMusicOn = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
        IsSoundEffectsOn = PlayerPrefs.GetInt("IsSoundEffectsOn", 1) == 1;
    }

    #endregion

    private void OnApplicationFocus(bool focus) => isFocused = focus;
    private void OnApplicationPause(bool pause) => isPaused = pause;
}
