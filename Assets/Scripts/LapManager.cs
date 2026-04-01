using UnityEngine;
using System; //NEW

public class LapManager : MonoBehaviour
{
    public Collider[] lapWaypoints;
    
    public Collider[] startWaypoints;
    public int cars, playerStartingPosition;
    public GameObject playerCar, aiCar;
    GameObject instance;
    //NEW
    CarMovement[] carMovements;
    int[] carPositions;

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

        //initialise arrays - NEW
        carMovements = new CarMovement[cars];
        carPositions = new int[cars];

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
            //NEW
            carMovements[i] = instance.GetComponent<CarMovement>();
            carMovements[i].SetStartPosition(startWaypoints[i]);
            carPositions[i] = i;
        }
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
}
