using UnityEngine;

public class LightManager : MonoBehaviour
{
    public static LightManager Instance;

    public GameObject greenLight;
    public GameObject redLight;

    void Awake()
    {
        Instance = this;
    }

    public void SetGreen()
    {
        greenLight.SetActive(true);
        redLight.SetActive(false);
    }

    public void SetRed()
    {
        greenLight.SetActive(false);
        redLight.SetActive(true);
    }

    public void SetLightsOff()
    {
        greenLight.SetActive(false);
        redLight.SetActive(false);
    }
}
