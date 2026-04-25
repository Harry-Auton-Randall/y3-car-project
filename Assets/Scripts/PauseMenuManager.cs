using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    InputActionMap menuActions;
    InputAction pauseUnpause;

    InputAction carResetPosAction;

    Canvas lapManagerCanvas, pauseMenuCanvas;

    public bool isPaused;

    GameObject[] panels;
    int currentPanel = 0;
    //Slider settingsVolumeSlider;

    void Awake()
    {
        menuActions = InputSystem.actions.FindActionMap("Menu");
        pauseUnpause = menuActions.FindAction("PauseUnpause");

        carResetPosAction = InputSystem.actions.FindAction("ResetPosition");

        lapManagerCanvas = transform.Find("LapManagerCanvas").GetComponent<Canvas>();
        pauseMenuCanvas = transform.Find("PauseMenuCanvas").GetComponent<Canvas>();

        panels = new GameObject[]
        {
            GameObject.Find("PauseMenuCanvas/MainPanel"),
            GameObject.Find("PauseMenuCanvas/SettingsPanel")
        };
        //settingsVolumeSlider = panels[1].transform.Find("VolumeSlider")
        //    .GetComponent<Slider>();
        SwitchPanel();

        Unpause();
    }

    void Start()
    {
        GetComponent<SettingsPanelManager>().parentReturnFunc = this.ReturnPressed;
    }

    void OnEnable()
    {
        pauseUnpause.performed += OnEscapePress;
    }
    void OnDisable()
    {
        pauseUnpause.performed -= OnEscapePress;
    }

    void OnEscapePress(InputAction.CallbackContext context)
    {
        if (isPaused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0;
        lapManagerCanvas.enabled = false;
        pauseMenuCanvas.enabled = true;

        carResetPosAction.Disable();
    }
    public void Unpause()
    {
        if (currentPanel == 1)
        {
            GetComponent<SettingsPanelManager>().CancelPressed();
        }

        isPaused = false;
        Time.timeScale = 1;
        lapManagerCanvas.enabled = true;
        pauseMenuCanvas.enabled = false;

        carResetPosAction.Enable();
    }


    void SwitchPanel()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (i == currentPanel) { panels[i].SetActive(true); }
            else { panels[i].SetActive(false); }
        }
    }

    public void MainSettingsPressed()
    {
        GetComponent<SettingsPanelManager>().SettingsOpened();
        //Debug.Log(PlayerPrefs.GetFloat("volume"));
        //GetComponent<SettingsPanelManager>().volumeInput
        //    = PlayerPrefs.GetFloat("volume");
        //settingsVolumeSlider.value = PlayerPrefs.GetFloat("volume");

        currentPanel = 1;
        SwitchPanel();
    }

    public void ReturnPressed()
    {
        currentPanel = 0;
        SwitchPanel();
    }
}
