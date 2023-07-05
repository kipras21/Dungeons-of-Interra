using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopMenuManager : MonoBehaviour
{
    public Slider dmgSlider;
    public TMP_Text dmgCostTxt;
    private int dmgMaxUpgrade = 5;
    private List<int> dmgCosts = new List<int>() { 25, 50, 75, 100, 150 };
    private int dmgDelta = 5;

    public Slider armorSlider;
    public TMP_Text armorCostTxt;
    private int armorMaxUpgrade = 5;
    private List<int> armorCosts = new List<int>() { 25, 50, 75, 100, 150 };
    private int armorDelta = 3;
    private int spellArmorDelta = 1;

    public Slider hpSlider;
    public TMP_Text hpCostTxt;
    private int hpMaxUpgrade = 5;
    private List<int> hpCosts = new List<int>() { 25, 50, 75, 100, 150 };
    private int hpDelta = 20;

    public Slider staminaSlider;
    public TMP_Text staminaCostTxt;
    private int staminaMaxUpgrade = 5;
    private List<int> staminaCosts = new List<int>() { 25, 50, 75, 100, 150 };
    private int staminaDelta = 20;
    private float recoverDelta = 0.11f;


    public Slider potionSlider;
    public TMP_Text potionCostTxt;
    private int potMaxUpgrade = 2;
    private List<int> potCosts = new List<int>() { 50, 100 };
    private int potDelta = 5;
    private float regenDelta = 0.05f;


    [HideInInspector]
    public PlayerMovement playerScr;


    public TMP_Text goldText;
    // Start is called before the first frame update
    private void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Back()
    {
        playerScr.inputMap.Enable();
        gameObject.SetActive(false);
    }

    public void init()
    {
        playerScr.inputMap.Disable();
        UpdateMenu();
    }

    private void UpdateMenu()
    {
        dmgSlider.maxValue = dmgMaxUpgrade;
        dmgSlider.value = playerScr.swordLevel;
        if (playerScr.swordLevel != dmgMaxUpgrade)
            dmgCostTxt.text = dmgCosts[playerScr.swordLevel].ToString();
        else dmgCostTxt.text = "MAX";
        armorSlider.maxValue = armorMaxUpgrade;
        armorSlider.value = playerScr.armorLevel;
        if (playerScr.armorLevel != armorMaxUpgrade)
            armorCostTxt.text = armorCosts[playerScr.armorLevel].ToString();
        else armorCostTxt.text = "MAX";

        hpSlider.maxValue = hpMaxUpgrade;
        hpSlider.value = playerScr.survivalLevel;
        if (playerScr.survivalLevel != hpMaxUpgrade)
            hpCostTxt.text = hpCosts[playerScr.survivalLevel].ToString();
        else hpCostTxt.text = "MAX";

        staminaSlider.maxValue = staminaMaxUpgrade;
        staminaSlider.value = playerScr.enduranceLevel;
        if (playerScr.enduranceLevel != staminaMaxUpgrade)
            staminaCostTxt.text = staminaCosts[playerScr.enduranceLevel].ToString();
        else staminaCostTxt.text = "MAX";

        potionSlider.maxValue = potMaxUpgrade;
        potionSlider.value = playerScr.potionLevel;
        if (playerScr.potionLevel != potMaxUpgrade)
            potionCostTxt.text = potCosts[playerScr.potionLevel].ToString();
        else potionCostTxt.text = "MAX";

        goldText.text = playerScr.gold.Value.ToString();
    }

    public void BuyDmgUpgrade()
    {
        if(playerScr.swordLevel != dmgMaxUpgrade)
        {
            if(playerScr.gold.Value >= dmgCosts[playerScr.swordLevel])
            {
                playerScr.UpdateGoldServerRPC(-dmgCosts[playerScr.swordLevel]);
                playerScr.swordLevel++;
                dmgSlider.value++;
                playerScr.UpdateDmgServerRPC(dmgDelta);
                playerScr.atkStats.BaseDamage += dmgDelta;
                UpdateMenu();
            }

        }
    }

    public void BuyArmorUpgrade()
    {
        if (playerScr.armorLevel != armorMaxUpgrade)
        {
            if (playerScr.gold.Value >= armorCosts[playerScr.armorLevel])
            {
                playerScr.UpdateGoldServerRPC(-armorCosts[playerScr.armorLevel]);
                playerScr.armorLevel++;
                armorSlider.value++;
                playerScr.UpdateArmorServerRPC(armorDelta, spellArmorDelta);
                playerScr.defStats.Armor += armorDelta;
                playerScr.defStats.SpellArmor += spellArmorDelta;
                UpdateMenu();
            }

        }
    }

    public void BuyHpUpgrade()
    {
        if (playerScr.survivalLevel != hpMaxUpgrade)
        {
            if (playerScr.gold.Value >= hpCosts[playerScr.survivalLevel])
            {
                playerScr.UpdateGoldServerRPC(-hpCosts[playerScr.survivalLevel]);
                playerScr.survivalLevel++;
                hpSlider.value++;
                playerScr.UpdateMaxHpServerRPC(hpDelta);
                UpdateMenu();
            }

        }
    }

    public void BuyStaminaUpgrade()
    {
        if (playerScr.enduranceLevel != staminaMaxUpgrade)
        {
            if (playerScr.gold.Value >= staminaCosts[playerScr.enduranceLevel])
            {
                playerScr.UpdateGoldServerRPC(-staminaCosts[playerScr.enduranceLevel]);
                playerScr.enduranceLevel++;
                staminaSlider.value++;
                playerScr.UpdateMaxStaminaServerRPC(staminaDelta);
                playerScr.recoveryRate += recoverDelta;
                UpdateMenu();
            }

        }
    }

    public void BuyPotUpgrade()
    {
        if (playerScr.potionLevel != potMaxUpgrade)
        {
            if (playerScr.gold.Value >= potCosts[playerScr.potionLevel])
            {
                playerScr.UpdateGoldServerRPC(-potCosts[playerScr.potionLevel]);
                playerScr.potionLevel++;
                potionSlider.value++;
                playerScr.UpdatePotServerRPC(potDelta, regenDelta);
                playerScr.regenTime += potDelta;
                playerScr.regenRate += regenDelta;
                UpdateMenu();
            }

        }
    }
}
