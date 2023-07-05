using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderUpdater : MonoBehaviour
{
    // Start is called before the first frame update

    public Slider staminaSlider;
    public Slider healthSlider;

    public Image potUsesImg;
    public Image potContentImg;

    public Sprite[] pUses;
    public Sprite[] pContent;

    public float staminaValue;
    public float healthValue;

    public TMP_Text goldText;
    public int gold;
    public bool updateGold;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        staminaSlider.value = staminaValue;
        healthSlider.value = healthValue;
        if(updateGold)
            goldText.text = gold.ToString();
    }

    public void UpdateIcons(int uses)
    {
        potUsesImg.sprite = pUses[uses];
        potContentImg.sprite = pContent[uses];
    }
}
