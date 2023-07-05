using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoxScript : NetworkBehaviour
{

    public GameObject Particles;
    [SerializeField]
    public List<Loot> loot;

    [Serializable]
    public struct Loot
    {
        public GameObject item;
        public float dropChance;
        public int minDrop;
        public int maxDrop;
    }

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
        if (collision.tag == "PlayerAtk")
        {
            //Box go boom
            if (IsHost)
            {
                CrackBoxClientRPC();
                DropLootServerRPC();
            }
            
        }
    }


    [ServerRpc]
    void DropLootServerRPC()
    {
        foreach (Loot drop in loot)
        {
            if (Random.Range(0, 100f) <= drop.dropChance)
            {
                int toDrop = Random.Range(drop.minDrop, drop.maxDrop + 1);
                for (int i = 0; i < toDrop; i++)
                {
                  GameObject item = Instantiate(drop.item, new Vector3(this.transform.position.x + Random.Range(-0.5f, 0.5f), this.transform.position.y + Random.Range(-0.5f, 0.5f), this.transform.position.z), Quaternion.identity);
                    item.GetComponent<NetworkObject>().Spawn(true);
                    
                }
            }
        }
        RemoveObjectClientRPC();
    }

    [ClientRpc]
    void CrackBoxClientRPC()
    {
        Instantiate(Particles, this.transform.position, Quaternion.identity);
    }

    [ClientRpc]
    void RemoveObjectClientRPC()
    {
        Destroy(gameObject);
    }
 
}



