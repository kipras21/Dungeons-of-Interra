using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpSounds : MonoBehaviour
{
    public List<AudioClip> swingSounds;
    public AudioClip deathSound;
    private AudioSource audioS;

    // Start is called before the first frame update
    void Start()
    {
        audioS = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void PlaySwing(int index)
    {
        audioS.PlayOneShot(swingSounds[index]);
    }

    public void PlayDeath()
    {
        audioS.PlayOneShot(deathSound);
    }
}
