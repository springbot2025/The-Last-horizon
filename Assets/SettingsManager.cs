using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    // Start is called before the first frame update
   

   
    public GameObject infoWindow;  // 
    public Button openInfoButton;  
    public Button closeInfoButton; 

    void Start()
    {
        // 
        openInfoButton.onClick.AddListener(OpenInfoWindow);
        closeInfoButton.onClick.AddListener(CloseInfoWindow);

        // 
        infoWindow.SetActive(false);
    }

    // 
    void OpenInfoWindow()
    {
        infoWindow.SetActive(true);
    }

   
    void CloseInfoWindow()
    {
        infoWindow.SetActive(false);
    }
}


