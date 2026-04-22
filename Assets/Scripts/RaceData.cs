using UnityEngine;

public class RaceData : MonoBehaviour
{
    public int lapCount;
    public int carCount;
    public int playerStartingPos;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}

