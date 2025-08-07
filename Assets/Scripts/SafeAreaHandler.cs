using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeAreaHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    void SafeArea()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

    }

}
