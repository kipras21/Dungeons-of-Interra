using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UNET;
using UnityEngine.UI;
using TMPro;
using Unity.Networking.Transport;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class WorldManager : MonoBehaviour
{
    string IP;
    string Port;

    private bool simulateUI = true;

    private Vector3 pz;
    private Vector3[] StartPos;

    public GameObject[] Layers;
    public float[] Modifiers;

    public GameObject Cam;
    public GameObject Grid;

    public TMP_InputField IPAddressField;
    public TMP_InputField PortField;

    public GameObject[] players;
    public GameObject loadingUI;

    public int maxPlayers;

    private float timeOutTimer;
    private float restartTimer;
    private bool restart = false;
    int kk = 0;
    PlayerMovement playerScr;

    private void Update()
    {
        if (simulateUI)
        {
            try
            {
                var pz = Camera.main.ScreenToViewportPoint(Input.mousePosition);
                pz.z = 0;
                for (int i = 0; i < Layers.Length; i++)
                {
                    if (Modifiers[i] != 0)
                    {
                        Layers[i].transform.position = pz;
                        Layers[i].transform.position = new Vector3(StartPos[i].x + (pz.x * Modifiers[i]), StartPos[i].y + (pz.y * Modifiers[i]), 0);
                    }
                }
            }
            catch
            {

            }
        }
    }

    private void FixedUpdate()
    {
        if (playerScr)
        {
            if (!restart)
            {
                try
                {


                    foreach (GameObject player in players)
                    {
                        if (player.GetComponent<PlayerMovement>().netCurHealth.Value == 0)
                        {
                            kk++;
                        }
                    }
                    if (kk == playerScr.NetworkManager.ConnectedClients.Count)
                    {
                        restart = true;
                        restartTimer = Time.time + 2;
                    }
                    kk = 0;
                }
                catch
                {
                    players = GameObject.FindGameObjectsWithTag("Player");
                }
            }
            else if (restartTimer < Time.time && restart)
            {
                playerScr.NetworkManager.SceneManager.LoadScene("ShopScene", LoadSceneMode.Single);
                int kk = 0;
                foreach (GameObject player in players)
                {
                    player.transform.position = new Vector2(0 + kk, -1 + 0.7f);
                    PlayerMovement tempPlScr = player.GetComponent<PlayerMovement>();
                    if (tempPlScr.netCurHealth.Value <= 0)
                        tempPlScr.ResetCharacterServerRPC();
                    foreach (Collider2D col in tempPlScr.colliders)
                    {
                        col.enabled = true;
                    }
                    Instantiate(loadingUI, Vector2.zero, Quaternion.identity);
                    tempPlScr.ResetAnimServerRPC();
                    tempPlScr.inputMap.Enable();
                    tempPlScr.ActivateUI();
                    tempPlScr.mainUI.UpdateIcons(tempPlScr.netPotUses.Value);
                    tempPlScr.clientUI.UpdateIcons(tempPlScr.netPotUses.Value);
                    kk++;
                }
                restart = false;
            }
        }
    }

    private void Start()
    {
        StartPos = new Vector3[Layers.Length];
        for (int i = 0; i < Layers.Length; i++)
        {
            StartPos[i] = Layers[i].transform.position;
        }
    }

    public void ClearUI()
    {
        Cam.SetActive(false);
        Grid.SetActive(true);
    }

    public void Host()
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            Layers[i].SetActive(false);
        }
        Cam.SetActive(false);
        Grid.SetActive(true);
        simulateUI = false;
        NetworkManager.Singleton.StartHost();
        playerScr = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        players = GameObject.FindGameObjectsWithTag("Player");
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedServer;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }


    public void Join()
    {
        UNetTransport NetScr = GetComponent<UNetTransport>();
        if (IPAddressField.text != "")
            NetScr.ConnectAddress = IPAddressField.text;
        if (PortField.text != "")
        {
            try
            {
                NetScr.ConnectPort = System.Int32.Parse(PortField.text);
            }
            catch
            {
                PortField.text = "Invalid Port";
            }
        }
        for (int i = 0; i < Layers.Length; i++)
        {
            Layers[i].SetActive(false);
        }
        Cam.SetActive(false);
        Grid.SetActive(true);
        simulateUI = false;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        StartCoroutine(TryConnect());
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientConnected(ulong obj)
    {
        players = GameObject.FindGameObjectsWithTag("Player");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var clientId = request.ClientNetworkId;
        var connectionData = request.Payload;

        if (playerScr.NetworkManager.ConnectedClients.Count < maxPlayers)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
        }
        else
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "Connection refused, server is full or in the wrong phase";
        }
        

        response.PlayerPrefabHash = null;
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;

        response.Pending = false;
    }

    IEnumerator TryConnect()
    {
        yield return new WaitForSeconds(5);
        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            for (int i = 0; i < Layers.Length; i++)
            {
                Layers[i].SetActive(true);
            }
            simulateUI = true;
            Cam.SetActive(true);
            Grid.SetActive(false);
        }
    }

    private void OnClientDisconnected(ulong obj)
    {
        NetworkManager.Singleton.Shutdown();
        players = GameObject.FindGameObjectsWithTag("Player");
        SceneManager.LoadScene("SampleScene");
    }

    private void OnClientDisconnectedServer(ulong obj)
    {
        players = GameObject.FindGameObjectsWithTag("Player");
    }
}

