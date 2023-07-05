using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DefenseStats
{
    
    public float DamageResistance;
    public float Armor;
    public float SpellArmor;

    public float LightningResistance;
    public float FireResistance;
}

[Serializable]
public class AtkStats
{
    public float BaseDamage;
    public DamageType Type;
}

public enum DamageType
{
    Physical,
    Lightning,
    Fire
}


public static class Damage
{
    public static float CalculateDamage(float incDmg, DamageType type, DefenseStats defense)
    {
        float calcDmg = 0;
        float DamageMultiplier;

        switch(type){
            case DamageType.Physical:
                DamageMultiplier = incDmg / (incDmg + defense.Armor);
                calcDmg = incDmg * SubtractPercent(ConvertToPercent(defense.DamageResistance));
                calcDmg = calcDmg * DamageMultiplier;
                break;

            case DamageType.Lightning:
                DamageMultiplier = incDmg / (incDmg + defense.SpellArmor);
                calcDmg = incDmg * SubtractPercent(ConvertToPercent(defense.LightningResistance));
                calcDmg = calcDmg * DamageMultiplier;
                break;
            case DamageType.Fire:
                DamageMultiplier = incDmg / (incDmg + defense.SpellArmor);
                calcDmg = incDmg * SubtractPercent(ConvertToPercent(defense.FireResistance));
                calcDmg = calcDmg * DamageMultiplier;
                break;
        }

        return calcDmg;
    }

    public static float ConvertToPercent(float resist)
    {
        if (resist >= 100)
        {
            return 1;
        }
        else if (resist != 0)
        {
            return resist / 100;
        }
        else return 0;
    }

    public static float SubtractPercent (float percent)
    {
        return 1 - percent;
    }
}
