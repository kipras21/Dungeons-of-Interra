using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ExitDoorScript : NetworkBehaviour
{

    public string SceneToLoad;
    public GameObject doorWarning;
    public GameObject loadingUI;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public int CountDead()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        int kk = 0;
        foreach(GameObject player in players)
        {
            if(player.GetComponent<PlayerMovement>().dead.Value)
            {
                kk++;
            }
        }
        return kk;
    }

    public void Interact(GameObject player)
    {
        CheckIfCanExitServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckIfCanExitServerRPC()
    {

        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, 3);
        int foundCount = 0;
        foreach (Collider2D coll in colls)
        {
            if (coll.gameObject.tag == "Player")
            {
                foundCount++;
            }
        }
        if (foundCount == NetworkManager.ConnectedClients.Count - CountDead())
        {
            Instantiate(loadingUI, Vector2.zero, Quaternion.identity);
            NetworkManager.SceneManager.LoadScene(SceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            SpawnWarningClientRPC();
        }

    }

    [ClientRpc]
    public void SpawnWarningClientRPC()
    {
        Instantiate(doorWarning, transform.position, Quaternion.identity);
    }

}
