using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;

    [SerializeField] private AudioSource soundFXObject; 

    private void Awake() {

        if(instance == null) {
            instance = this;
        }
    }


    public void PlaySound(AudioClip audio, Transform spawn, float volume) {

                if (audio == null || soundFXObject == null) return;


        AudioSource audioSource = Instantiate(soundFXObject, spawn.position, Quaternion.identity);

        audioSource.clip = audio;
        audioSource.volume = volume;
        audioSource.Play();


        float audioLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, audioLength);
    }

}
