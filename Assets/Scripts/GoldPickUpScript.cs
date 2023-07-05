using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GoldPickUpScript : NetworkBehaviour
{

    private PlayerMovement playerScr;
    public int minCoins;
    public int maxCoins;
    private int value;
    // Start is called before the first frame update
    void Start()
    {
        value = Random.Range(minCoins, maxCoins+1);
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
            PickUpGoldServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PickUpGoldServerRPC()
    {
        playerScr.UpdateGoldServerRPC(value);
        gameObject.GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }
}
