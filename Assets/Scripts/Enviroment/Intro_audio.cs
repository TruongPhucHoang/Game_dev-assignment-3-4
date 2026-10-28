using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip scaredMusic;
    [SerializeField] private AudioClip deadMusic;

    AudioSource audioSource;
    bool moodHeld;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        StartCoroutine(PlayMusicSequence());
    }

    public void PlayNormal()
    {
        PlayLoop(normalMusic);
    }

    public void PlayScared()
    {
        PlayLoop(scaredMusic);
    }

    public void PlayDead()
    {
        PlayLoop(deadMusic);
    }

    void PlayLoop(AudioClip clip)
    {
        moodHeld = true;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (clip == null || audioSource == null)
            return;
        if (audioSource.clip == clip && audioSource.isPlaying && audioSource.loop)
            return;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private IEnumerator PlayMusicSequence()
    {
        // play intro
        audioSource.clip = introMusic;
        audioSource.loop = false;
        audioSource.Play();

        // wait time
        float introDuration = introMusic != null ? introMusic.length : 0f;
        float waitTime = Mathf.Min(introDuration, 3f);
        yield return new WaitForSeconds(waitTime);

        if (moodHeld)
            yield break;

        audioSource.clip = normalMusic;
        audioSource.loop = true;
        audioSource.Play();
    }
}
