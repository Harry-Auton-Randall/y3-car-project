using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class ScoreManager : MonoBehaviour
{
    //Moved from MainMenuManager
    public List<TrackInfo> trackInfos = new List<TrackInfo>();

    AllTimeInfo allTimeInfo;
    string fileName = "bestTimes.json";
    string filePath;
    string allTimeInfoJson;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        //Moved from MainMenuManager
        trackInfos.Add(new TrackInfo("Basic Track 1", 4,
            Resources.Load("TrackImages/track1Image", typeof(Texture2D)) as Texture2D, "Track1"));
        trackInfos.Add(new TrackInfo("Basic Track 2", 8,
            Resources.Load("TrackImages/track2Image", typeof(Texture2D)) as Texture2D, "Track2"));
        trackInfos.Add(new TrackInfo("Ampersand Circuit", 12,
            Resources.Load("TrackImages/track3Image", typeof(Texture2D)) as Texture2D, "Track3"));
        trackInfos.Add(new TrackInfo("Multiple Routes Track", 10,
            Resources.Load("TrackImages/track4Image", typeof(Texture2D)) as Texture2D, "Track4"));

        filePath = Path.Combine(Application.persistentDataPath, fileName);
        Debug.Log(filePath);

        if (!File.Exists(filePath))
        {
            //File doesn't exist, initialise allTimeInfo and write to file
            Debug.Log("file doesn't exist");

            allTimeInfo = new AllTimeInfo();
            for (int i=0;i<trackInfos.Count;i++)
            {
                allTimeInfo.trackTimeInfo.Add(new TrackTimeInfo(trackInfos[i].name, 0));
            }

            allTimeInfoJson = JsonUtility.ToJson(allTimeInfo);
            Debug.Log(allTimeInfoJson);

            File.WriteAllText(filePath, allTimeInfoJson);
        }
        else
        {
            //File exists, read into allTimeInfo
            Debug.Log("file exists");

            allTimeInfoJson = File.ReadAllText(filePath);
            Debug.Log(allTimeInfoJson);
            allTimeInfo = JsonUtility.FromJson<AllTimeInfo>(allTimeInfoJson);

            //Debug.Log(allTimeInfo.trackTimeInfo.Count);
            //for (int i=0;i< allTimeInfo.trackTimeInfo.Count;i++)
            //{
            //    Debug.Log(allTimeInfo.trackTimeInfo[i].trackName);
            //    Debug.Log(allTimeInfo.trackTimeInfo[i].bestLap);
            //    Debug.Log(allTimeInfo.trackTimeInfo[i].timeInfo.Count);
            //}

            //NEW
            //Finds out if any tracks are missing from allTimeInfo, adds them, re-saves
            bool present;
            bool updated = false;

            for (int i = 0; i < trackInfos.Count; i++)
            {
                present = false;
                for (int j = 0; j < allTimeInfo.trackTimeInfo.Count; j++)
                {
                    if (trackInfos[i].name == 
                        allTimeInfo.trackTimeInfo[j].trackName)
                    {
                        present = true;
                        break;
                    }
                }

                if (!present)
                {
                    allTimeInfo.trackTimeInfo.Add(new TrackTimeInfo(trackInfos[i].name, 0));
                    updated = true;
                }
            }

            if (updated)
            {
                allTimeInfoJson = JsonUtility.ToJson(allTimeInfo);
                Debug.Log(allTimeInfoJson);

                File.WriteAllText(filePath, allTimeInfoJson);
            } 
        }
    }


    public void SaveBestTimes(out bool lapSuc, out bool totalSuc, int lapCountIn, 
                              float lapIn, float totalIn, string trackNameIn)
    {
        bool present = false;
        bool updated = false;

        TrackTimeInfo trackIn = null;
        for (int i=0;i<allTimeInfo.trackTimeInfo.Count;i++)
        {
            if (allTimeInfo.trackTimeInfo[i].trackName == trackNameIn)
            {
                trackIn = allTimeInfo.trackTimeInfo[i];
                break;
            }
        }

        //Check bestLap first
        if (lapIn < trackIn.bestLap || trackIn.bestLap == 0)
        {
            trackIn.bestLap = lapIn;
            lapSuc = true;
            updated = true;
        }
        else
        {
            lapSuc = false;
        }

        //Check timeInfo list 2nd
        totalSuc = false;
        for (int i = 0; i < trackIn.timeInfo.Count; i++)
        {
            if (trackIn.timeInfo[i].lapCount == lapCountIn)
            {
                if (totalIn < trackIn.timeInfo[i].time)
                {
                    trackIn.timeInfo[i].time = totalIn;
                    totalSuc = true;
                    updated = true;
                }

                present = true;
                break;
            }
        }
        //If no TimeInfo found with the correct lapCount, adds a new one
        //Also happens if timeInfo list is empty
        if (!present)
        {
            trackIn.timeInfo.Add(new TimeInfo(lapCountIn, totalIn));
            totalSuc = true;
            updated = true;
        }

        //Re-save, if necessary
        if (updated)
        {
            allTimeInfoJson = JsonUtility.ToJson(allTimeInfo);
            File.WriteAllText(filePath, allTimeInfoJson);
        }
    }



    public void FindTimesWithCount(out float lapTimeOut, out float totalTimeOut, 
                                   int lapCountIn, string trackNameIn)
    {
        TrackTimeInfo trackIn = null;
        for (int i = 0; i < allTimeInfo.trackTimeInfo.Count; i++)
        {
            if (allTimeInfo.trackTimeInfo[i].trackName == trackNameIn)
            {
                trackIn = allTimeInfo.trackTimeInfo[i];
                break;
            }
        }

        lapTimeOut = trackIn.bestLap;

        totalTimeOut = 0;
        for (int i = 0; i < trackIn.timeInfo.Count; i++)
        {
            if (trackIn.timeInfo[i].lapCount == lapCountIn)
            {
                totalTimeOut = trackIn.timeInfo[i].time;
                break;
            }
        }
    }



    public void FindTimesWithoutCount(out float lapTimeOut, out float totalTimeOut,
                                      out int lapCountOut, string trackNameIn)
    {
        TrackTimeInfo trackIn = null;
        for (int i = 0; i < allTimeInfo.trackTimeInfo.Count; i++)
        {
            if (allTimeInfo.trackTimeInfo[i].trackName == trackNameIn)
            {
                trackIn = allTimeInfo.trackTimeInfo[i];
                break;
            }
        }

        lapTimeOut = trackIn.bestLap;

        lapCountOut = 0;
        totalTimeOut = 0;
        for (int i = 0; i < trackIn.timeInfo.Count; i++)
        {
            if (trackIn.timeInfo[i].time < totalTimeOut || totalTimeOut == 0)
            {
                lapCountOut = trackIn.timeInfo[i].lapCount;
                totalTimeOut = trackIn.timeInfo[i].time;
            }
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
    public string trackName;
    public float bestLap;
    public List<TimeInfo> timeInfo;

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
    public List<TrackTimeInfo> trackTimeInfo;

    public AllTimeInfo()
    {
        trackTimeInfo = new List<TrackTimeInfo>();
    }
}
