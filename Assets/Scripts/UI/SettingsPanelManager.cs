using UnityEngine;
using System;
using UnityEngine.Audio; //NEW
using UnityEngine.UI; //NEW

public class SettingsPanelManager : MonoBehaviour
{
    public Action parentReturnFunc;

    public bool changingSettings;
    public float volumeInput;

    //NEW
    public AudioMixer mixer;
    string mixerVolume = "MasterVolume";

    public Slider volumeSlider;

    //NEW
    void Start()
    {
        mixer.SetFloat(mixerVolume, Mathf.Log10(PlayerPrefs.GetFloat("volume")) * 20);
    }

    //NEW
    public void SettingsOpened()
    {
        volumeInput = PlayerPrefs.GetFloat("volume");
        Debug.Log(volumeInput);
        volumeSlider.value = volumeInput;
        mixer.SetFloat(mixerVolume, Mathf.Log10(volumeInput) * 20);
    }

    public void VolumeInputChange(float input)
    {
        volumeInput = input;

        //NEW
        if (volumeInput < 0.0001f)
        {
            volumeInput = 0.0001f;
        }
        mixer.SetFloat(mixerVolume, Mathf.Log10(volumeInput) * 20);

        changingSettings = true;

    }
    public void SavePressed()
    {
        PlayerPrefs.SetFloat("volume", volumeInput);
        PlayerPrefs.Save();
        Debug.Log(PlayerPrefs.GetFloat("volume"));

        changingSettings = false;
        parentReturnFunc?.Invoke();
    }
    public void CancelPressed()
    {
        volumeInput = PlayerPrefs.GetFloat("volume");

        //NEW
        mixer.SetFloat(mixerVolume, Mathf.Log10(volumeInput) * 20);

        changingSettings = false;
        parentReturnFunc?.Invoke();
    }
}

