using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RegenerationScript : NetworkBehaviour
{
    private PlayerMovement playerScr;
    public GameObject RegenParticles;
    public float regenRate;
    public float timer;
    // Start is called before the first frame update
    void Start()
    {
        playerScr = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (timer > Time.time)
        {
            if (playerScr.netCurHealth.Value < playerScr.netMaxHealth.Value)
            {
                if (playerScr.netCurHealth.Value + regenRate < playerScr.netMaxHealth.Value)
                {
                    playerScr.UpdateHealthServerRpc(playerScr.netCurHealth.Value + regenRate);
                }
                else playerScr.UpdateHealthServerRpc(playerScr.netMaxHealth.Value);
            }
        }
        else
        {
            if (IsLocalPlayer)
                RemoveRegenServerRPC();
            if (!IsHost)
            {
                Destroy(RegenParticles);
                Destroy(GetComponent<RegenerationScript>());
            }
        }
    }

    [ServerRpc]
    public void RemoveRegenServerRPC()
    {
        Destroy(RegenParticles);
        Destroy(GetComponent<RegenerationScript>());
    }
}
