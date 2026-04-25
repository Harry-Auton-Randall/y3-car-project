using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class ScoreManager : MonoBehaviour
{
    //Moved from MainMenuManager
    public List<TrackInfo> trackInfos = new List<TrackInfo>();

    AllTimeInfo allTimeInfo;
    string fileName = "/bestTimes.json";

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        //Moved from MainMenuManager
        trackInfos.Add(new TrackInfo("Track 1", 4,
            Resources.Load("TrackImages/track1Image", typeof(Texture2D)) as Texture2D, "Track1"));
        trackInfos.Add(new TrackInfo("Track 2", 8,
            Resources.Load("TrackImages/track2Image", typeof(Texture2D)) as Texture2D, null));
        trackInfos.Add(new TrackInfo("Track 3", 2,
            Resources.Load("TrackImages/track3Image", typeof(Texture2D)) as Texture2D, null));
        trackInfos.Add(new TrackInfo("Track with Unusual Name", 99,
            Resources.Load("TrackImages/trackUnusualImage", typeof(Texture2D)) as Texture2D, null));

        Debug.Log(Application.persistentDataPath);
        if (!File.Exists(Application.persistentDataPath + fileName))
        {
            //File doesn't exist, initialise allTimeInfo and write to file
            Debug.Log("file doesn't exist");
        }
        else
        {
            //File exists, read into allTimeInfo
            Debug.Log("file exists");
        }
    }
}

//Connects time to lap count
[Serializable] //allows the class to be converted to JSON
public class TimeInfo
{
    public int lapCount;
    public float time;
    public TimeInfo(int lapCountIn, float timeIn)
    {
        lapCount = lapCountIn;
        time = timeIn;
    }
}

//Stores all best times for a single track
[Serializable]
public class TrackTimeInfo
{
    string trackName;
    float bestLap;
    List<TimeInfo> timeInfo;
    public TrackTimeInfo(string trackNameIn, float bestLapIn)
    {
        trackName = trackNameIn;
        bestLap = bestLapIn;
        timeInfo = new List<TimeInfo>();
    }
}

//Stores a list of all track's TimeInfos
[Serializable]
public class AllTimeInfo
{
    List<TrackTimeInfo> trackTimeInfo;
    public AllTimeInfo()
    {
        trackTimeInfo = new List<TrackTimeInfo>();
    }
}
