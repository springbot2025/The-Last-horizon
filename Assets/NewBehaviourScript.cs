using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
   

[RequireComponent(typeof(RawImage))]
public class FullScreenRawImage : MonoBehaviour
{
    void Start()
    {
        FitToScreen();
    }

    void FitToScreen()
    {
        RawImage rawImage = GetComponent<RawImage>();
        RectTransform rect = GetComponent<RectTransform>();

        float screenRatio = (float)Screen.width / Screen.height;
        float imageRatio = (float)rawImage.texture.width / rawImage.texture.height;

        if (screenRatio > imageRatio)
        {
            // 
            float scale = screenRatio / imageRatio;
            rect.localScale = new Vector3(scale, 1, 1);
        }
        else
        {
            // 
            float scale = imageRatio / screenRatio;
            rect.localScale = new Vector3(1, scale, 1);
        }
    }
}
