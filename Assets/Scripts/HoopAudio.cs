using UnityEngine;

public class HoopAudio : MonoBehaviour
{
    public static HoopAudio Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource crowdSource;

    [SerializeField] private AudioClip dribbleClip;
    [SerializeField] private AudioClip squeakClip;
    [SerializeField] private AudioClip swishClip;
    [SerializeField] private AudioClip slamClip;
    [SerializeField] private AudioClip whistleClip;
    [SerializeField] private AudioClip dodgeClip;
    [SerializeField] private AudioClip cheerClip;
    [SerializeField] private AudioClip crowdOohClip;
    [SerializeField] private AudioClip defenseChantClip;
    [SerializeField] private AudioClip streetballBgmClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
        }
        if (crowdSource == null) crowdSource = gameObject.AddComponent<AudioSource>();

        if (streetballBgmClip != null && bgmSource != null)
        {
            bgmSource.clip = streetballBgmClip;
            bgmSource.volume = 0.5f;
            bgmSource.Play();
        }
    }

    public void PlayDribble() => PlaySFX(dribbleClip, Random.Range(0.92f, 1.08f), 0.85f);
    public void PlaySqueak() => PlaySFX(squeakClip, Random.Range(0.95f, 1.05f), 0.7f);
    public void PlayDodge() => PlaySFX(dodgeClip, 1f, 0.75f);
    public void PlayWhistle() => PlaySFX(whistleClip, 1f, 0.8f);

    public void PlaySwish(float comboPitch = 1f)
    {
        PlaySFX(swishClip, comboPitch, 0.9f);
        PlayCrowd(cheerClip, 0.6f);
    }

    public void PlaySlam(float comboPitch = 1f)
    {
        PlaySFX(slamClip, comboPitch, 1f);
        PlayCrowd(cheerClip, 0.85f);
    }

    public void PlayDefenseChant()
    {
        PlayCrowd(defenseChantClip, 0.9f);
    }

    public void PlayCrowdOoh()
    {
        PlayCrowd(crowdOohClip, 0.9f);
    }

    public void PlayCheer()
    {
        PlayCrowd(cheerClip, 0.85f);
    }

    public void PlayBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            if (streetballBgmClip != null) bgmSource.clip = streetballBgmClip;
            bgmSource.volume = 0.5f;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    private void PlaySFX(AudioClip clip, float pitch, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayCrowd(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (crowdSource != null)
        {
            crowdSource.pitch = 1f;
            crowdSource.PlayOneShot(clip, volume);
        }
        else if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
