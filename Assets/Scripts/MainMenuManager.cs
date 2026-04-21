using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    GameObject[] panels;
    int currentPanel = 0;

    List<TrackInfo> trackInfos = new List<TrackInfo>();
    int currentTrack = 0;
    TMPro.TextMeshProUGUI trackNameText, trackMaxRacersText;


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


        trackInfos.Add(new TrackInfo("Track 1", 4, null, null));
        trackInfos.Add(new TrackInfo("Track 2", 8, null, null));
        trackInfos.Add(new TrackInfo("Track 3", 2, null, null));
        trackInfos.Add(new TrackInfo("Track with Unusual Name", 99, null, null));


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
    }
    public void SwitchCurrentTrack(int amount)
    {
        currentTrack += amount;
        if (currentTrack < 0) { currentTrack = trackInfos.Count - 1; }
        else if (currentTrack >= trackInfos.Count) { currentTrack = 0; }

        DisplayTrackInfo();
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

