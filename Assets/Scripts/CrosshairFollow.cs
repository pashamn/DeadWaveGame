using UnityEngine;

public class CrosshairFollow : MonoBehaviour
{
    void Update()
    {
        transform.position = Input.mousePosition;
    }
}