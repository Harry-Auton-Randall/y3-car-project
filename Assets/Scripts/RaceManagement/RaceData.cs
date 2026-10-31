using UnityEngine;

public class RaceData : MonoBehaviour
{
    public int lapCount;
    public int carCount;
    public int playerStartingPos;
    public string trackName;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}

