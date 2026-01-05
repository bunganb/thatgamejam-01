using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private float bgmVolume;
    private float sfxVolume;
    [Header("Slider")]
    public Slider sfxVolumeSlider;
    public Slider bgmVolumeSlider;

    [Header("AudioSource")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("BGM")]
    [SerializeField] private AudioClip bgmClip;

    [Header("SFX List")]
    [SerializeField] private List<SFXData> sfxList;

    private Dictionary<SFXType, AudioClip> sfxDict;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitSFX();
    }

    private void InitSFX()
    {
        sfxDict = new Dictionary<SFXType, AudioClip>();

        foreach (var sfx in sfxList)
        {
            if (!sfxDict.ContainsKey(sfx.type))
                sfxDict.Add(sfx.type, sfx.clip);
        }
    }

    private void Start()
    {
         bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.3f);
         sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.3f);

        bgmVolumeSlider.value = bgmVolume;
        sfxVolumeSlider.value = sfxVolume;

        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;

        bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        PlayBgm();
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
        if (!bgmSource.isPlaying)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void PlaySFX(SFXType type)
    {
        if (sfxDict.TryGetValue(type, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"SFX {type} belum terdaftar");
        }
    }
    public void EnterResultState()
    {
        bgmSource.volume = bgmVolume * 0.3f;   // BGM dikecilkan
        sfxSource.volume = sfxVolume * 1.5f;   // SFX dikuatkan
    }
    public void ResetAudioState()
    {
        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;
    }
}
