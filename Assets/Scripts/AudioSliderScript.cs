using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSliderScript : MonoBehaviour
{
    public Slider slider;
    public AudioMixer mixer;
    public string mixerName;
    public AudioSource audioS;


    // Start is called before the first frame update
    void Start()
    {
        LoadSlider();
        slider.onValueChanged.AddListener(delegate
        {
            SliderNewValue();
        });

    }

    public void SliderNewValue()
    {
        mixer.SetFloat(mixerName, Mathf.Log10(slider.value)*20);
        PlayerPrefs.SetFloat(mixerName, slider.value);
    }

    public void LoadSlider()
    {
        if (PlayerPrefs.HasKey(mixerName))
            slider.value = PlayerPrefs.GetFloat(mixerName);
        mixer.SetFloat(mixerName, Mathf.Log10(slider.value) * 20);
        SliderNewValue();
    }


    public void PlaySound()
    {
        audioS.Play();
    }
    

}
