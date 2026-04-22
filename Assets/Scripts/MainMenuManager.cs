using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    GameObject[] panels;
    int currentPanel = 0;

    List<TrackInfo> trackInfos = new List<TrackInfo>();
    int currentTrack = 0;
    TMPro.TextMeshProUGUI trackNameText, trackMaxRacersText;
    RawImage trackImage;

    RaceData raceData;

    void Awake()
    {
        panels = new GameObject[]
        {
            GameObject.Find("MenuCanvas/TitlePanel"),
            GameObject.Find("MenuCanvas/TrackSelectPanel")
        };

        trackNameText = panels[1].transform.Find("TrackNameText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        trackMaxRacersText = panels[1].transform.Find("TrackMaxRacersText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        trackImage = panels[1].transform.Find("TrackImage")
            .GetComponent<RawImage>();

        raceData = GameObject.Find("/RaceDataPasser").GetComponent<RaceData>();

        trackInfos.Add(new TrackInfo("Track 1", 4, 
            Resources.Load("TrackImages/track1Image", typeof(Texture2D)) as Texture2D, "Track1")); //CHANGED
        trackInfos.Add(new TrackInfo("Track 2", 8,
            Resources.Load("TrackImages/track2Image", typeof(Texture2D)) as Texture2D, null));
        trackInfos.Add(new TrackInfo("Track 3", 2,
            Resources.Load("TrackImages/track3Image", typeof(Texture2D)) as Texture2D, null));
        trackInfos.Add(new TrackInfo("Track with Unusual Name", 99,
            Resources.Load("TrackImages/trackUnusualImage", typeof(Texture2D)) as Texture2D, null));


        DisplayTrackInfo();

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
        Debug.Log("Settings pressed in Title");
    }
    public void TitleQuitPressed()
    {
        Debug.Log("Quit pressed in Title");
    }

    public void TrackReturnPressed()
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
    }
    public void TrackPlayPressed()
    {
        raceData.lapCount = 2;
        raceData.carCount = 2;
        raceData.playerStartingPos = 2;

        SceneManager.LoadScene(trackInfos[currentTrack].sceneName);
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

