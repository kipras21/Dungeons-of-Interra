using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShopSceneManager : NetworkBehaviour
{
    public GameObject loadingScreen;


    // Start is called before the first frame update
    void Start()
    {
        
        StartCoroutine(SpawnLogic());
    }

    IEnumerator SpawnLogic()
    {
        yield return new WaitForSeconds(0.1f);
        if (IsHost)
        {
            GameObject.FindGameObjectWithTag("Manager").GetComponent<WorldManager>().maxPlayers = 2;
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            int kk = 0;
            foreach (GameObject player in players)
            {
                player.transform.position = new Vector2(0 + kk, -1 + 0.7f);
                PlayerMovement tempPlScr = player.GetComponent<PlayerMovement>();
                if (tempPlScr.netCurHealth.Value <= 0)
                    tempPlScr.UpdateHealthServerRpc(tempPlScr.netMaxHealth.Value * 0.25f);
                foreach(Collider2D col in tempPlScr.colliders)
                {
                    col.enabled = true;
                }
                tempPlScr.ResetAnimServerRPC();
                tempPlScr.inputMap.Enable();
                tempPlScr.ActivateUI();
                tempPlScr.mainUI.UpdateIcons(tempPlScr.netPotUses.Value);
                tempPlScr.clientUI.UpdateIcons(tempPlScr.netPotUses.Value);
                tempPlScr.UpdateDeadServerRpc(false);
                kk++;
            }
            yield return new WaitForSeconds(0.4f);
            loadingScreen.SetActive(false);
        }

        if (!IsHost)
        {
            yield return new WaitForSeconds(0.5f);
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                PlayerMovement tempPlScr = player.GetComponent<PlayerMovement>();
                foreach (Collider2D col in tempPlScr.colliders)
                {
                    col.enabled = true;
                }
                tempPlScr.AnimatePlayerMovementServerRpc(0, -1);
                tempPlScr.inputMap.Enable();
                tempPlScr.ActivateUI();
                tempPlScr.mainUI.UpdateIcons(tempPlScr.netPotUses.Value);
                tempPlScr.clientUI.UpdateIcons(tempPlScr.netPotUses.Value);
            }
            loadingScreen.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
