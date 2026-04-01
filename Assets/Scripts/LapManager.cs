using UnityEngine;
using System;
using System.Collections; //NEW
using System.Collections.Generic; //NEW

public class LapManager : MonoBehaviour
{
    public Collider[] lapWaypoints;
    
    public Collider[] startWaypoints;
    public int cars, playerStartingPosition;
    public GameObject playerCar, aiCar;
    GameObject instance;
    
    CarMovement[] carMovements;
    int[] carPositions;

    //NEW
    public int totalLaps = 3;
    float[] totalLapTimes;
    float[] bestLapTimes;
    List<int> finalPositions;


    void Awake()
    {
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
        //NEW
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
            carMovements[i].SetStartPosition(startWaypoints[i], i, totalLaps); //NEW INPUTS
            carPositions[i] = i;
        }

        //NEW
        StartCoroutine(StartRace());
    }

    //NEW
    public void RegisterFinish(int idIn, float totalTimeIn, float bestTimeIn)
    {
        finalPositions.Add(idIn);
        totalLapTimes[idIn] = totalTimeIn;
        bestLapTimes[idIn] = bestTimeIn;
    }

    void Update()
    {
        Array.Sort(carPositions, (a, b) =>
            (carMovements[b].lapPub, 
             carMovements[b].lapWaypointPub, 
             carMovements[a].nextLapWaypointDistPub)
          .CompareTo(
            (carMovements[a].lapPub, 
             carMovements[a].lapWaypointPub, 
             carMovements[b].nextLapWaypointDistPub))
        );

        for (int i=0;i<carPositions.Length;i++)
        {
            carMovements[carPositions[i]].positionPub = i + 1;
        }
    }

    //NEW
    IEnumerator StartRace()
    {
        yield return new WaitForSeconds(3);
        for (int i=0;i<carMovements.Length;i++)
        {
            carMovements[i].EnableRaceStarted();
        }
    }
}
