using UnityEngine;
using TMPro;

public class PlayerHudManager : MonoBehaviour
{
    CarMovement carMovement;
    LapManager lapManager;
    int totalLaps;

    TMPro.TextMeshProUGUI positionText, speedText, lapText;
    TMPro.TextMeshProUGUI totalTimeText, currentTimeText, bestTimeText;

    //for the time formatting
    //int minute, second, ms;

    void Awake()
    {
        carMovement = GetComponent<CarMovement>();
        lapManager = GameObject.Find("/LapManager").GetComponent<LapManager>();
        totalLaps = lapManager.totalLaps;

        positionText = transform.Find("HudCanvas/PositionText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        speedText = transform.Find("HudCanvas/SpeedText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        lapText = transform.Find("HudCanvas/LapText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        totalTimeText = transform.Find("HudCanvas/TotalTimeText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        currentTimeText = transform.Find("HudCanvas/CurrentTimeText")
            .GetComponent<TMPro.TextMeshProUGUI>();
        bestTimeText = transform.Find("HudCanvas/BestTimeText")
            .GetComponent<TMPro.TextMeshProUGUI>();
    }

    void Update()
    {
        //set position text
        switch(carMovement.positionPub)
        {
            case 1:
                positionText.text = "1st";
                break;
            case 2:
                positionText.text = "2nd";
                break;
            case 3:
                positionText.text = "3rd";
                break;
            default:
                positionText.text = (carMovement.positionPub + "th");
                break;
        }

        //set speed text (converts from m/s to mph)
        speedText.text = (Mathf.Floor(carMovement.currentSpeed * 2.23694f) + "mph");

        //set lap text
        lapText.text = ("Lap " + carMovement.lapPub + " / " + totalLaps);

        //set time texts using function
        totalTimeText.text = (FormatTime(carMovement.lapTimeTotal) + " - Total");
        currentTimeText.text = (FormatTime(carMovement.lapTimeCurrent) + " - Current");
        bestTimeText.text = (FormatTime(carMovement.lapTimeBest) + " - Best");
    }

    //function for formatting the time texts
    public static string FormatTime(float input) //PUBLIC AND STATIC NOW
    {
        int minute, second, ms; //DECLARED HERE NOW

        //minute
        minute = Mathf.FloorToInt(input / 60);

        //second
        second = Mathf.FloorToInt(input - (60 * minute));

        //milliseconds
        ms = Mathf.FloorToInt((input - (second + (60 * minute))) * 100);
        
        //toString("D2") ensures at least 2 digits, aka number 3 will be displayed as "03"
        return (minute.ToString("D2") + ":" + 
            second.ToString("D2") + ":" + ms.ToString("D2"));
    }

    public void Disable()
    {
        positionText.enabled = false;
        speedText.enabled = false;
        lapText.enabled = false;
        totalTimeText.enabled = false;
        currentTimeText.enabled = false;
        bestTimeText.enabled = false;
    }
}
