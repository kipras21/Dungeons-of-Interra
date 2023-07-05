using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopScript : MonoBehaviour
{

    public GameObject shopUI;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void Interact(GameObject player)
    {
        if (!shopUI.activeInHierarchy)
        {
            shopUI.SetActive(true);
            ShopMenuManager tempScr = shopUI.GetComponent<ShopMenuManager>();
            tempScr.playerScr = player.GetComponent<PlayerMovement>();
            tempScr.init();
        }
    }


 
}
