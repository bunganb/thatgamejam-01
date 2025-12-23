using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [Header("Slider")]
    public Slider sfxVolumeSlider;
    public Slider bgmVolumeSlider;
    
    [Header("AudioSource")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource bgmSource;
   
    [Header("Audio Clip")]
    [SerializeField] AudioClip bgmClip;
    [SerializeField] AudioClip sfxClip;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.3f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.3f);

        bgmVolumeSlider.value = bgmVolume;
        sfxVolumeSlider.value = sfxVolume;

        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;

        bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
    }
    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
        PlayerPrefs.SetFloat("BGMVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    
    public void PlayBgm()
    {
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySfx()
    {
        sfxSource.PlayOneShot(sfxClip);
    }
    
}
