using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LapManager : MonoBehaviour
{
    public Collider[] lapWaypoints;
    
    public Collider[] startWaypoints;
    public int cars, playerStartingPosition;
    public GameObject playerCar, aiCar;
    GameObject instance;
    
    CarMovement[] carMovements;
    int[] carPositions;
    List<int> stillRacing;

    public int totalLaps = 3;
    float[] totalLapTimes;
    float[] bestLapTimes;
    List<int> finalPositions;

    TMPro.TextMeshProUGUI countdownText;
    TMPro.TextMeshProUGUI resultPositionText, resultTotalTimeText, resultBestTimeText;

    //Sfx stuff - NEW
    AudioSource audioSource;
    public AudioClip ding, ding2;

    void Awake()
    {
        audioSource = transform.Find("LapAudio").GetComponent<AudioSource>(); //NEW;

        countdownText = transform.Find("LapManagerCanvas/CountdownText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        resultPositionText = transform.Find("LapManagerCanvas/ResultPositionText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        resultTotalTimeText = transform.Find("LapManagerCanvas/ResultTotalTimeText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        resultBestTimeText = transform.Find("LapManagerCanvas/ResultBestTimeText")
            .GetComponent<TMPro.TextMeshProUGUI>();

        resultPositionText.enabled = false;
        resultTotalTimeText.enabled = false;
        resultBestTimeText.enabled = false;

        for (int i=0;i<lapWaypoints.Length;i++)
        {
            lapWaypoints[i].GetComponent<Waypoint>().lapWaypointValue = i;

            if (i == lapWaypoints.Length - 1)
            {
                lapWaypoints[i].GetComponent<Waypoint>().nextLapWaypoint = lapWaypoints[0];
            }
            else
            {
                lapWaypoints[i].GetComponent<Waypoint>().nextLapWaypoint = lapWaypoints[i+1];
            }
        }

        //Prevent incorrect cars/playerStartingPosition values
        cars = Mathf.Clamp(cars, 2, startWaypoints.Length);
        playerStartingPosition = Mathf.Clamp(playerStartingPosition, 1, cars);

        //initialise arrays
        carMovements = new CarMovement[cars];
        carPositions = new int[cars];
        stillRacing = new List<int>(cars);

        totalLapTimes = new float[cars];
        bestLapTimes = new float[cars];
        finalPositions = new List<int>(cars);

        if (totalLaps < 1) { totalLaps = 1; }

        //Instantiates player and AI cars in the correct positions
        for (int i=0;i<cars;i++)
        {
            if (i+1 == playerStartingPosition)
            {
                instance = Instantiate(playerCar);
            }
            else
            {
                instance = Instantiate(aiCar);
            }
            
            carMovements[i] = instance.GetComponent<CarMovement>();
            carMovements[i].SetStartPosition(startWaypoints[i], i, totalLaps);
            carPositions[i] = i;
            stillRacing.Add(i);
        }

        StartCoroutine(StartRace());
    }

    public void RegisterFinish(int idIn, float totalTimeIn, float bestTimeIn)
    {
        finalPositions.Add(idIn);
        stillRacing.Remove(idIn);
        totalLapTimes[idIn] = totalTimeIn;
        bestLapTimes[idIn] = bestTimeIn;
    }

    void Update()
    {
        //stillRacing is now sorted
        stillRacing.Sort((a, b) =>
            (carMovements[b].lapPub, 
             carMovements[b].lapWaypointPub, 
             carMovements[a].nextLapWaypointDistPub)
          .CompareTo(
            (carMovements[a].lapPub, 
             carMovements[a].lapWaypointPub, 
             carMovements[b].nextLapWaypointDistPub))
        );

        //NEW - copy results from finalPositions and stillRacing into carPositions
        for (int i=0; i<finalPositions.Count; i++)
        {
            carPositions[i] = finalPositions[i];
        }
        for (int i=0; i<stillRacing.Count; i++)
        {
            carPositions[i + finalPositions.Count] = stillRacing[i];
        }

        for (int i=0;i<carPositions.Length;i++)
        {
            carMovements[carPositions[i]].positionPub = i + 1;
        }
    }

    IEnumerator StartRace()
    {
        //NEW
        countdownText.enabled = false;
        yield return new WaitForSeconds(5);
        countdownText.enabled = true;

        countdownText.text = "3";
        audioSource.PlayOneShot(ding, 1f); //NEW
        yield return new WaitForSeconds(1);

        countdownText.text = "2";
        audioSource.PlayOneShot(ding); //NEW
        yield return new WaitForSeconds(1);

        countdownText.text = "1";
        audioSource.PlayOneShot(ding); //NEW
        yield return new WaitForSeconds(1);

        for (int i=0;i<carMovements.Length;i++)
        {
            carMovements[i].EnableRaceStarted();
        }

        countdownText.text = "GO!";
        audioSource.PlayOneShot(ding2); //NEW
        yield return new WaitForSeconds(1);

        countdownText.enabled = false;
    }

    public IEnumerator LastLapAlert()
    {
        countdownText.enabled = true;
        countdownText.text = "Last Lap!";
        yield return new WaitForSeconds(1);
        countdownText.enabled = false;
    }

    public void DisplayPlayerResults(int idIn)
    {
        resultPositionText.enabled = true;
        resultTotalTimeText.enabled = true;
        resultBestTimeText.enabled = true;

        int finalPosition = finalPositions.IndexOf(idIn) + 1;

        switch (finalPosition)
        {
            case 1:
                resultPositionText.text = "You came 1st!";
                break;
            case 2:
                resultPositionText.text = "You came 2nd!";
                break;
            case 3:
                resultPositionText.text = "You came 3rd!";
                break;
            default:
                resultPositionText.text = ("You came " + finalPosition + "th!");
                break;
        }

        resultTotalTimeText.text = ("Total time - " +
            PlayerHudManager.FormatTime(totalLapTimes[idIn]));

        resultBestTimeText.text = ("Best lap - " +
            PlayerHudManager.FormatTime(bestLapTimes[idIn]));

        audioSource.PlayOneShot(ding2); //NEW
    }

    //NEW
    public void LapUpdateSound()
    {
        audioSource.PlayOneShot(ding, 0.5f);
    }
}
