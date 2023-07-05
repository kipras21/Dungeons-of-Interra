using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    public GameObject mainMenu;
    public GameObject settingsMenu;
    public GameObject controlsMenu;
    [HideInInspector]
    public InputMap playerInput;

    public TMP_Text healKey;
    public TMP_Text atkKey;
    public TMP_Text blockKey;
    public TMP_Text interactKey;

    public AudioSliderScript MusicSlider;
    public AudioSliderScript SfxSlider;

    [HideInInspector]
    public PlayerMovement playerScr;

    string device;
    string key;


    InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private void Awake()
    {
        playerInput = new InputMap();
        LoadControls();
        
    }

    private void Start()
    {
        MusicSlider.LoadSlider();
        SfxSlider.LoadSlider();
    }

    public void Update()
    {
        if (rebindOperation != null)
        {
            if (rebindOperation.completed)
            {
                playerInput.Player.ConsumePotion.GetBindingDisplayString(0, out device, out key);              
                PlayerPrefs.SetString("HealButton", "<" + device + ">/" + key);
                playerInput.Player.Attack.GetBindingDisplayString(0, out device, out key);
                PlayerPrefs.SetString("AttackButton", "<" + device + ">/" + key);
                playerInput.Player.Block.GetBindingDisplayString(0, out device, out key);
                PlayerPrefs.SetString("BlockButton", "<" + device + ">/" + key);
                playerInput.Player.Interact.GetBindingDisplayString(0, out device, out key);
                PlayerPrefs.SetString("InteractButton", "<" + device + ">/" + key);
                PlayerPrefs.Save();
                UpdateControlLabels();
                rebindOperation.Dispose();
            }
        }
    }


    public void OpenSettings()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
        controlsMenu.SetActive(false);
        if (rebindOperation != null)
            rebindOperation.Dispose();
    }

    public void OpenMain()
    {
        if (mainMenu)
            mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
        controlsMenu.SetActive(false);
        if (rebindOperation != null)
            rebindOperation.Dispose();
    }

    public void OpenControls()
    {
        if(mainMenu)
            mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        controlsMenu.SetActive(true);
        UpdateControlLabels();
    }

    public void DestroySettings()
    {
        playerScr.inputMap.Enable();
        Destroy(this.gameObject);
    }


    public void RemapHealButtonClicked()
    {
        rebindOperation = playerInput.Player.ConsumePotion.PerformInteractiveRebinding().Start();      
    }

    public void RemapAttackButtonClicked()
    {
        rebindOperation = playerInput.Player.Attack.PerformInteractiveRebinding().Start();
    }

    public void RemapBlockButtonClicked()
    {
        rebindOperation = playerInput.Player.Block.PerformInteractiveRebinding().Start();
    }

    public void RemapInteractButtonClicked()
    {
        rebindOperation = playerInput.Player.Interact.PerformInteractiveRebinding().Start();
    }

    private void UpdateControlLabels()
    {
        healKey.text = playerInput.Player.ConsumePotion.GetBindingDisplayString();
        atkKey.text = playerInput.Player.Attack.GetBindingDisplayString();
        blockKey.text = playerInput.Player.Block.GetBindingDisplayString();
        interactKey.text = playerInput.Player.Interact.GetBindingDisplayString();
    }

    private void LoadControls()
    {
        if (PlayerPrefs.HasKey("HealButton"))
        {
            playerInput.Player.ConsumePotion.ApplyBindingOverride(PlayerPrefs.GetString("HealButton"));
        }

        if (PlayerPrefs.HasKey("AttackButton"))
        {
            playerInput.Player.Attack.ApplyBindingOverride(PlayerPrefs.GetString("AttackButton"));
        }

        if (PlayerPrefs.HasKey("BlockButton"))
        {
            playerInput.Player.Block.ApplyBindingOverride(PlayerPrefs.GetString("BlockButton"));
        }

        if (PlayerPrefs.HasKey("InteractButton"))
        {
            playerInput.Player.Interact.ApplyBindingOverride(PlayerPrefs.GetString("InteractButton"));
        }

    }

    public void QuitButton()
    {
        Application.Quit();
    }


    public void ReturnToMenu()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("SampleScene");
    }

}
