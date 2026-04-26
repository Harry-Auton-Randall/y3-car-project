using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    GameObject[] panels;
    int currentPanel = 0;

    //List<TrackInfo> trackInfos = new List<TrackInfo>();
    List<TrackInfo> trackInfos;

    int currentTrack = 0;
    TMPro.TextMeshProUGUI trackNameText, trackMaxRacersText;
    RawImage trackImage;

    RaceData raceData;

    int lapInput = 0;
    int carInput = 0;
    int pspInput = 0;
    bool lapValid, carValid, pspValid;
    Button trackPlayButton;
    TMP_InputField trackLapInput, trackCarInput, trackPSPInput;

    public GameObject scoreManagerObj; //assign in inspector

    //NEW
    ScoreManager scoreManager;

    //Slider settingsVolumeSlider;

    void Awake()
    {
        //NEW
        if (GameObject.Find("/ScoreManagerObj(Clone)") == null)
        {
            Instantiate(scoreManagerObj);
        }

        panels = new GameObject[]
        {
            GameObject.Find("MenuCanvas/TitlePanel"),
            GameObject.Find("MenuCanvas/TrackSelectPanel"),
            GameObject.Find("MenuCanvas/SettingsPanel")
        };

        trackNameText = panels[1].transform.Find("TrackNameText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        trackMaxRacersText = panels[1].transform.Find("TrackMaxRacersText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        trackImage = panels[1].transform.Find("TrackImage")
            .GetComponent<RawImage>();

        trackPlayButton = panels[1].transform.Find("PlayButton")
            .GetComponent<Button>();
        trackLapInput = panels[1].transform.Find("TrackLapInput")
            .GetComponent<TMP_InputField>();
        trackCarInput = panels[1].transform.Find("TrackRacersInput")
            .GetComponent<TMP_InputField>();
        trackPSPInput = panels[1].transform.Find("TrackPSPInput")
            .GetComponent<TMP_InputField>();

        //settingsVolumeSlider = panels[2].transform.Find("VolumeSlider")
        //    .GetComponent<Slider>();

        raceData = GameObject.Find("/RaceDataPasser").GetComponent<RaceData>();

        //trackInfos.Add(new TrackInfo("Track 1", 4, 
        //    Resources.Load("TrackImages/track1Image", typeof(Texture2D)) as Texture2D, "Track1"));
        //trackInfos.Add(new TrackInfo("Track 2", 8,
        //    Resources.Load("TrackImages/track2Image", typeof(Texture2D)) as Texture2D, null));
        //trackInfos.Add(new TrackInfo("Track 3", 2,
        //    Resources.Load("TrackImages/track3Image", typeof(Texture2D)) as Texture2D, null));
        //trackInfos.Add(new TrackInfo("Track with Unusual Name", 99,
        //    Resources.Load("TrackImages/trackUnusualImage", typeof(Texture2D)) as Texture2D, null));
    }

    void Start()
    {
        GetComponent<SettingsPanelManager>().parentReturnFunc = this.ReturnPressed;

        //CHANGED
        scoreManager = GameObject.Find("/ScoreManagerObj(Clone)").
            GetComponent<ScoreManager>();
        //References ScoreManager's array directly, not a copy
        trackInfos = scoreManager.trackInfos;

        //IN START NOW
        DisplayTrackInfo();
        TrackCheckValidInputs();
        SwitchPanel();
    }

    void SwitchPanel()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (i == currentPanel) { panels[i].SetActive(true); }
            else { panels[i].SetActive(false); }
        }
    }

    public void TitlePlayPressed()
    {
        currentPanel = 1;
        SwitchPanel();
    }
    public void TitleSettingsPressed()
    {
        //CHANGED
        GetComponent<SettingsPanelManager>().SettingsOpened();
        //Debug.Log(PlayerPrefs.GetFloat("volume"));
        //GetComponent<SettingsPanelManager>().volumeInput
        //    = PlayerPrefs.GetFloat("volume");
        //settingsVolumeSlider.value = PlayerPrefs.GetFloat("volume");

        currentPanel = 2;
        SwitchPanel();
    }
    public void TitleQuitPressed()
    {
        Debug.Log("Quit pressed in Title");
        Application.Quit();
    }

    public void ReturnPressed()
    {
        currentPanel = 0;
        SwitchPanel();
    }
    void DisplayTrackInfo()
    {
        trackNameText.text = trackInfos[currentTrack].name;
        trackMaxRacersText.text = ("Max. " + trackInfos[currentTrack].maxRacers + " Racers");
        trackImage.texture = trackInfos[currentTrack].image;
    }
    public void SwitchCurrentTrack(int amount)
    {
        currentTrack += amount;
        if (currentTrack < 0) { currentTrack = trackInfos.Count - 1; }
        else if (currentTrack >= trackInfos.Count) { currentTrack = 0; }

        DisplayTrackInfo();

        //Re-checks CarInput, because it's outcome is dependent on the current track
        //TrackCheckValidInputs called in CheckPSPInput which is called in CheckCarInput
        TrackCheckCarInput(trackCarInput.text);
    }
    public void TrackPlayPressed()
    {
        raceData.lapCount = lapInput;
        raceData.carCount = carInput;
        raceData.playerStartingPos = pspInput;
        raceData.trackName = trackInfos[currentTrack].name;

        SceneManager.LoadScene(trackInfos[currentTrack].sceneName);
    }
    public void TrackCheckLapInput(string input)
    {
        //Check input from Lap field
        if (int.TryParse(input, out lapInput) && lapInput >= 1)
        {
            lapValid = true;
        }
        else { lapValid = false; }
        TrackCheckValidInputs();
    }
    public void TrackCheckCarInput(string input)
    {
        //Check input from Racers field
        if (int.TryParse(input, out carInput) && carInput >= 2
            && carInput <= trackInfos[currentTrack].maxRacers)
        {
            carValid = true;
        }
        else { carValid = false; }

        //Re-checks PSPinput, because it's outcome is dependent on carInput
        //TrackCheckValidInputs called in that function, not also needed here
        TrackCheckPSPInput(trackPSPInput.text);
    }
    public void TrackCheckPSPInput(string input)
    {
        //Check input from PSP field
        if (int.TryParse(input, out pspInput) && pspInput >= 1
            && pspInput <= carInput)
        {
            pspValid = true;
        }
        else { pspValid = false; }

        TrackCheckValidInputs();
    }
    void TrackCheckValidInputs()
    {
        //Determines if all fields are valid
        if (lapValid && carValid && pspValid)
        {
            trackPlayButton.interactable = true;
        }
        else { trackPlayButton.interactable = false; }
    }
}

public class TrackInfo
{
    public string name;
    public int maxRacers;
    public Texture2D image;
    public string sceneName;

    public TrackInfo(string nameIn, int maxIn, Texture2D imageIn, string sceneIn)
    {
        name = nameIn;
        maxRacers = maxIn;
        image = imageIn;
        sceneName = sceneIn;
    }
}

