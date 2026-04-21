using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    //NEW
    GameObject[] panels;
    int currentPanel = 0;

    //NEW
    void Awake()
    {
        panels = new GameObject[]
        {
            GameObject.Find("MenuCanvas/TitlePanel"),
            GameObject.Find("MenuCanvas/TrackSelectPanel")
        };
        SwitchPanel();
    }

    //NEW
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
        //NEW
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

    //NEW
    public void TrackReturnPressed()
    {
        currentPanel = 0;
        SwitchPanel();
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

