using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardSounds : MonoBehaviour
{
    public AudioClip tpIn;
    public AudioClip tpOut;
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

    public void PlayTpIn()
    {
        audioS.PlayOneShot(tpIn);
    }

    public void PlayTpOut()
    {
        audioS.PlayOneShot(tpOut);
    }
}
