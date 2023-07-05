using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PotionPickupScript : NetworkBehaviour
{
    private PlayerMovement playerScr;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            playerScr = collision.gameObject.GetComponent<PlayerMovement>();
            PickUpPotServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PickUpPotServerRPC()
    {
        if (playerScr.netPotUses.Value < 2)
        {
            playerScr.UpdatePotUsesServerRPC(1);
            gameObject.GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
}
