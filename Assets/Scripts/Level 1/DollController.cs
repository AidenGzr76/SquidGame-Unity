using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DollController : MonoBehaviour
{
    public static DollController Instance;

    [Header("Light Timings")]
    public float minRedLightTime = 1.5f;
    public float maxRedLightTime = 3f;
    private float currentRedLightTime;

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public AudioClip[] greenLightClips;

    [Header("UI Image Settings")]
    public Image dollImage;   // به Image کامپوننت وصل کن
    public Sprite backSprite; // چراغ سبز → پشت
    public Sprite frontSprite; // چراغ قرمز → جلو

    [Header("Scale Settings")]
    public Vector3 backScale = new Vector3(0.3f, 0.3f, 0.3f);
    public Vector3 frontScale = new Vector3(0.36f, 0.36f, 0.36f);

    [Header("Start Settings")]
    public float initialDelay = 2f;
    private bool isActive = true;
    private bool stopAfterTurn = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!dollImage) dollImage = GetComponent<Image>();
        StartCoroutine(StartDollWithDelay());
    }

    IEnumerator StartDollWithDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        StartCoroutine(DollCycle());
    }

    IEnumerator DollCycle()
    {
        while (isActive)
        {
            // چراغ سبز (پشت)
            currentRedLightTime = Random.Range(minRedLightTime, maxRedLightTime);

            AudioClip randomClip = greenLightClips[Random.Range(0, greenLightClips.Length)];
            musicSource.clip = randomClip;

            LightManager.Instance.SetGreen();
            if (dollImage && backSprite)
            {
                dollImage.sprite = backSprite;
                transform.localScale = backScale;
            }

            musicSource.Play();
            yield return new WaitForSeconds(musicSource.clip.length);

            // چراغ قرمز (جلو)
            musicSource.Stop();
            LightManager.Instance.SetRed();
            if (dollImage && frontSprite)
            {
                dollImage.sprite = frontSprite;
                transform.localScale = frontScale;
            }

            currentRedLightTime = Random.Range(minRedLightTime, maxRedLightTime);
            yield return new WaitForSeconds(currentRedLightTime);

            if (stopAfterTurn)
            {
                isActive = false;
                break;
            }
        }
    }

    public void StopDollAfterCurrentTurn()
    {
        stopAfterTurn = true;
    }

    public bool IsRedLight()
    {
        return LightManager.Instance != null && LightManager.Instance.redLight.activeSelf;
    }
}
