using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ChestScript : NetworkBehaviour
{

    public Animator openAnim;
    public Collider2D col1;
    public Collider2D col2;
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

    [ServerRpc]
    void DropLootServerRPC()
    {
        StartCoroutine(DelayOpen());
    }

    [ServerRpc(RequireOwnership = false)]
    void OpenChestServerRPC()
    {
        openAnim.SetTrigger("Open");
        DropLootServerRPC();
    }

    [ClientRpc]
    void RemoveColliderClientRPC()
    {
        col1.enabled = false;
        col2.enabled = false;
    }

    public void Interact()
    {
        OpenChestServerRPC();
        openAnim.SetTrigger("Open");
    }

    IEnumerator DelayOpen()
    {
        yield return new WaitForSeconds(0.4f);
        foreach (Loot drop in loot)
        {
            if (Random.Range(0, 100f) <= drop.dropChance)
            {
                int toDrop = Random.Range(drop.minDrop, drop.maxDrop + 1);
                for (int i = 0; i < toDrop; i++)
                {
                    yield return new WaitForSeconds(0.1f);
                    GameObject item = Instantiate(drop.item, new Vector3(this.transform.position.x + Random.Range(-0.5f, 0.5f), this.transform.position.y + Random.Range(-0.5f, 0.5f), this.transform.position.z), Quaternion.identity);
                    item.GetComponent<NetworkObject>().Spawn(true);

                }
            }
        }
        RemoveColliderClientRPC();
    }
}



