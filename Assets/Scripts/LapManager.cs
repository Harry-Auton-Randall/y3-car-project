using UnityEngine;

public class LapManager : MonoBehaviour
{
    public Collider[] lapWaypoints;
    //NEW
    public Collider[] startWaypoints;
    public int cars, playerStartingPosition;
    public GameObject playerCar, aiCar;
    GameObject instance;

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

        //Prevent incorrect cars/playerStartingPosition values - NEW
        cars = Mathf.Clamp(cars, 2, startWaypoints.Length);
        playerStartingPosition = Mathf.Clamp(playerStartingPosition, 1, cars);

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
            instance.GetComponent<CarMovement>().SetStartPosition(startWaypoints[i]);
        }
    }

    void Update()
    {
        
    }
}
