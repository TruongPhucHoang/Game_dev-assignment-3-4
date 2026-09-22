using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip normalMusic;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(PlayMusicSequence());
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

        // switch audio to normal
        audioSource.clip = normalMusic;
        audioSource.loop = true;
        audioSource.Play();
    }
}
