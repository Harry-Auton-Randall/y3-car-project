using UnityEngine;
using System;

public class SettingsPanelManager : MonoBehaviour
{
    public Action parentReturnFunc;

    public bool changingSettings;
    public float volumeInput;

    public void VolumeInputChange(float input)
    {
        volumeInput = input;
        changingSettings = true;
    }
    public void SavePressed()
    {
        //NEW
        PlayerPrefs.SetFloat("volume", volumeInput);
        PlayerPrefs.Save();
        Debug.Log(PlayerPrefs.GetFloat("volume"));

        changingSettings = false;
        parentReturnFunc?.Invoke();
    }
    public void CancelPressed()
    {
        volumeInput = PlayerPrefs.GetFloat("volume"); //NEW
        changingSettings = false;
        parentReturnFunc?.Invoke();
    }
}

