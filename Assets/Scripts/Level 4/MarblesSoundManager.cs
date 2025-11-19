using UnityEngine;

public class MarblesSoundManager : MonoBehaviour
{
    public static MarblesSoundManager Instance { get; private set; }

    [Header("UI Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound_MarblePlace;
    public AudioClip confirmSound_Bet;

    [Header("Guess Result Sounds")]
    public AudioClip guessCorrectSound;
    public AudioClip guessWrongSound;

    [Header("Score Sounds")]
    public AudioClip scoreSwooshSound;
    public AudioClip winChimeSound;
    public AudioClip loseThudSound;

    [Header("Ambience & Tension")]
    public AudioClip ambientWindSound;
    public AudioClip tensionTickTockSound;
    public AudioClip guessStartGongSound;
    public AudioClip turnChangeChimeSound;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.7f;
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}