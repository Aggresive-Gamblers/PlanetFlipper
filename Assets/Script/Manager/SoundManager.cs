using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    List<AudioSource> musicSources;
    List<AudioSource> sfxSources;

    [Header("SFX Settings")]
    public int sfxPoolSize = 10;
    public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;

    private AudioClip lastSFXClip;


    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // Init Music Pool
        musicSources = new List<AudioSource>();
        for (int i = 0; i < 3; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true; // en général la musique est en boucle
            source.volume = musicVolume;
            musicSources.Add(source);
        }
        
        // Init SFX Pool
        sfxSources = new List<AudioSource>();
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = sfxVolume;
            sfxSources.Add(source);
        }
    }

    public void PlayMusic(AudioClip clip, int id,bool loop = true)
    {
        if (musicSources[id].clip == clip)
        {
            return;
        }
        
        musicSources[id].clip = clip;
        musicSources[id].loop = loop;
        musicSources[id].volume = musicVolume;
        musicSources[id].Play();
    }
    public void StopMusic(bool fadeOut,int id)
    {
        if (musicSources[id].isPlaying == false)
        {
            return;
        }
        
        if (fadeOut)
        {
            StartCoroutine(FadeOutMusic(0.5f,id));
        }
        else
        {
            sfxSources[id].Stop();
        }
    }
    

    public void PlaySFX(AudioClip clip, float volume)
    {
        AudioSource freeSource = sfxSources.Find(source => !source.isPlaying);
        if (freeSource != null)
        {
            freeSource.clip = clip;
            freeSource.volume = volume;
            freeSource.Play();
        }
        else
        {
            // Si tous les sources sont prises, on peut en choisir une au hasard ou l�ignorer
            Debug.LogWarning("All SFX audio sources are busy!");
        }
    }
    public void PlayLoopSound(AudioClip clip, float volume, int id)
    {
        if (sfxSources[id].volume < volume || !sfxSources[id].isPlaying)
        {
            sfxSources[id].loop = true;
            sfxSources[id].clip = clip;
            sfxSources[id].volume = volume;
            sfxSources[id].Play();
        }
    }
    
    public void StopLoopSound( bool fadeOut, int id)
    {
        
        if (fadeOut)
        {
            StartCoroutine(FadeOutLoopSound(sfxSources[id].clip,0.3f,id));
            sfxSources[id].Stop();
        }
        else
        {
            sfxSources[id].Stop();
        }
    }


    public void PlaySFX(List<AudioClip> clips, float volume)
    {
        if (clips == null || clips.Count() == 0)
        {
            Debug.LogWarning("No AudioClips provided!");
            return;
        }

        AudioClip chosenClip;

        if (clips.Count() == 1)
        {
            chosenClip = clips[0];
        }
        else
        {
            int attempts = 0;
            do
            {
                chosenClip = clips[Random.Range(0, clips.Count())];
                attempts++;
            }
            while (chosenClip == lastSFXClip && attempts < 5); // �vite boucle infinie
        }

        lastSFXClip = chosenClip;
        PlaySFX(chosenClip,volume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        foreach (var source in musicSources)
        {
            source.volume = volume;
        }

    }

    public void StopSound(AudioClip clip)
    {
        AudioSource freeSource = sfxSources.Find(source => source.isPlaying);
        while(freeSource != null)
        {
            freeSource.clip = clip;
            freeSource.Stop();
            freeSource = sfxSources.Find(source => source.isPlaying);
        }
    }

    private System.Collections.IEnumerator FadeOutLoopSound(AudioClip clip,float duration,int id)
    {
        float startVolume = sfxSources[id].volume;
        float elapsed = 0f;

        sfxSources[id + 3].clip = clip;
        sfxSources[id + 3].loop = true;
        sfxSources[id + 3].Play();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sfxSources[id+3].volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        sfxSources[id+3].Stop();
        sfxSources[id+3].volume = startVolume;
    }
    
    private System.Collections.IEnumerator FadeOutMusic(float duration,int id)
    {
        float startVolume = musicSources[id].volume;
        float elapsed = 0f;
        
        musicSources[id].Play();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSources[id].volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSources[id].Stop();
        musicSources[id].volume = startVolume;
    }
}
