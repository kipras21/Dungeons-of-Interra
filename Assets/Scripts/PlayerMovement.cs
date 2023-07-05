using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    public float Speed;
    private float calcSpeed;
    public Rigidbody2D RigBod;
    private Animator anim;
    public Transform feet;
    private Camera cam;
    private float AtkCd = 0;
    private float AttackingTime = 0;
    private int prevAtkId = 0;
    [HideInInspector]
    public InputMap inputMap;
    [HideInInspector]
    public NetworkVariable<int> gold = new NetworkVariable<int>();
    private int enemyAtkId;
    AtkInfo enemyAtkInfo;
    AtkStats enemyAtkStats;
    ProjectileScript enemyProjectile;
    private MenuManager menuScr;

    [HideInInspector]
    public int atkId = 0;
    private List<int> hitId = new List<int>();
    public Transform atkCollider;
    [HideInInspector]
    public bool blocking;
    [HideInInspector]
    public bool perfectBlocking;
    private float blockTimer;
    [SerializeField]
    public NetworkVariable<float> netCurHealth = new NetworkVariable<float>();
    [SerializeField]
    public NetworkVariable<float> netCurStamina = new NetworkVariable<float>();
    [Header("Char Stats")]
    public float curHealth;
    public float curStamina;
    [HideInInspector]
    public NetworkVariable<float> netMaxHealth = new NetworkVariable<float>();
    [HideInInspector]
    public NetworkVariable<float> netMaxStamina = new NetworkVariable<float>();
    public float maxHealth;
    public float maxStamina;

    private float baseHealth;
    private float baseStamina;
    private float baseRegenRate;
    private float baseRegenTime;
    private DefenseStats baseDefStats;
    private AtkStats baseAtkStats;

    public DefenseStats defStats;
    public AtkStats atkStats;
    public int swordLevel = 0;
    public int armorLevel = 0;
    public int survivalLevel = 0;
    public int enduranceLevel = 0;
    public int potionLevel = 0;
    public float atkCost;
    public float blockCost;
    public float recoveryRate;
    public float recoveryCd;
    public GameObject blockParticles;
    public GameObject healParticles;
    public GameObject regenParticles;
    [HideInInspector]
    public float regenRate = 0.1f;
    [HideInInspector]
    public float regenTime = 10;
    private float recoveryTimer;
    private float drinkTimer;
    [SerializeField]
    public NetworkVariable<int> netPotUses;

    [HideInInspector]
    public SliderUpdater mainUI;
    [HideInInspector]
    public SliderUpdater clientUI;

    private float potCd;

    private Vector2 moveVector;

    private AudioSource audioS;
    public List<AudioClip> walkSounds;
    public AudioClip swingSound;
    public AudioClip deathSound;
    public AudioClip blockSound;
    public AudioClip parrySound;
    public AudioClip healSound;

    public List<Collider2D> colliders;

    public NetworkVariable<bool> dead;
    public GameObject settingsUI;


    // Start is called before the first frame update
    void Start()
    {
        anim = this.GetComponent<Animator>();
        audioS = GetComponent<AudioSource>();

        ActivateUI();
        if (!IsLocalPlayer)
        {
            Destroy(gameObject.GetComponentInChildren<Camera>().gameObject);//.SetActive(false);
        }
        else
        {
            cam = gameObject.GetComponentInChildren<Camera>();

        }

        baseHealth = netMaxHealth.Value;
        baseStamina = netMaxStamina.Value;
        baseRegenRate = regenRate;
        baseRegenTime = regenTime;
        baseDefStats = defStats;
        baseAtkStats = atkStats;
    }

    private void Awake()
    {
        menuScr = GameObject.FindGameObjectWithTag("Canvas").GetComponent<MenuManager>();
        inputMap = menuScr.playerInput;
    }

    private void OnEnable()
    {

        inputMap.Enable();
        inputMap.Player.Movement.performed += OnMovementPerformed;
        inputMap.Player.Movement.canceled += OnMovementCancelled;
        inputMap.Player.ConsumePotion.performed += OnConsumePotionPerformed;
        inputMap.Player.Block.performed += OnBlockPerformed;
        inputMap.Player.Attack.performed += OnAttackPerformed;
        inputMap.Player.Interact.performed += OnInteractPerformed;
        inputMap.Player.Settings.performed += OnMenuPerformed;

    }
    private void OnDisable()
    {
        if (IsLocalPlayer)
        {
            inputMap.Disable();
            inputMap.Player.Movement.performed -= OnMovementPerformed;
            inputMap.Player.Movement.canceled -= OnMovementCancelled;
            inputMap.Player.ConsumePotion.performed -= OnConsumePotionPerformed;
            inputMap.Player.Block.performed -= OnBlockPerformed;
            inputMap.Player.Attack.performed -= OnAttackPerformed;
            inputMap.Player.Interact.performed -= OnInteractPerformed;
            inputMap.Player.Settings.performed -= OnMenuPerformed;
        }
    }


    private void OnMovementPerformed(InputAction.CallbackContext value)
    {
        if (IsLocalPlayer)
        {
            moveVector = value.ReadValue<Vector2>();
        }
    }
    private void OnMovementCancelled(InputAction.CallbackContext value)
    {
        if (IsLocalPlayer)
        {
            moveVector = Vector2.zero;
        }
    }

    private void OnConsumePotionPerformed(InputAction.CallbackContext value)
    {
        if (IsLocalPlayer)
        {
            if (!blocking && !perfectBlocking && AttackingTime <= Time.time && drinkTimer < Time.time && netPotUses.Value > 0 && potCd < Time.time)
            {
                SetMovingAnimServerRpc(false);
                SetVelocityServerRpc(Vector2.zero);
                HealAnimServerRPC();
                drinkTimer = Time.time + 1;
            }
        }
    }

    private void OnBlockPerformed(InputAction.CallbackContext value)
    {
        if (IsLocalPlayer)
        {
            if (!blocking && !perfectBlocking && netCurStamina.Value > blockCost && drinkTimer < Time.time)
            {
                UpdateStaminaServerRpc(netCurStamina.Value - blockCost);
                recoveryTimer = Time.time + recoveryCd;
                blockTimer = Time.time + 0.5f;
                SetVelocityServerRpc(Vector2.zero);
                SetMovingAnimServerRpc(false);
                AnimatePlayerBlockServerRpc((cam.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10) - this.transform.position).normalized);
            }
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext value)
    {
        if (IsLocalPlayer)
        {
            if (AtkCd <= Time.time && !blocking && !perfectBlocking && !Input.GetKey(KeyCode.LeftShift) && netCurStamina.Value > atkCost && drinkTimer < Time.time)
            {
                UpdateStaminaServerRpc(netCurStamina.Value - atkCost);
                recoveryTimer = Time.time + recoveryCd;
                AnimatePlayerAtkServerRpc((cam.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10) - this.transform.position).normalized);
                AtkCd = Time.time + 0.7f;
                AttackingTime = Time.time + 0.5f;
                SetVelocityServerRpc(Vector2.zero);
                SetMovingAnimServerRpc(false);
            }
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext value)
    {
        if (IsLocalPlayer)
        {
            Collider2D[] collissions = Physics2D.OverlapCircleAll(transform.position, 1);
            foreach (Collider2D coll in collissions)
            {
                if (coll.gameObject.tag == "Interactable")
                {
                    coll.gameObject.SendMessage("Interact", gameObject);
                }
            }
        }
    }

    private void OnMenuPerformed(InputAction.CallbackContext value)
    {
        if(IsLocalPlayer)
        {
            inputMap.Disable();
            Instantiate(settingsUI, Vector2.zero, Quaternion.identity).GetComponent<MenuManager>().playerScr = this;
        }
    }



    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            //if(netCurHealth.Value <= 0 && !dead.Value)
            //{
            //    UpdateDeadServerRpc(true);
            //    SetVelocityServerRpc(Vector2.zero);
            //    SetMovingAnimServerRpc(false);
            //    DisableInputClientRPC();
            //    anim.StopPlayback();
            //    anim.SetTrigger("Death");
            //    UpdateDeadServerRpc(true);
            //    foreach (Collider2D col in colliders)
            //    {
            //        col.enabled = false;
            //    }
            //}
            if (recoveryTimer < Time.time && netCurStamina.Value < netMaxStamina.Value && !blocking)
            {
                if (netCurStamina.Value + recoveryRate < netMaxStamina.Value)
                {
                    UpdateStaminaServerRpc(netCurStamina.Value + recoveryRate);
                }
                else UpdateStaminaServerRpc(netMaxStamina.Value);
            }
            if (mainUI)
            {
                mainUI.staminaValue = netCurStamina.Value / netMaxStamina.Value;
                mainUI.healthValue = netCurHealth.Value / netMaxHealth.Value;
                mainUI.gold = gold.Value;
            }
        }
        else
        {
            if (clientUI)
            {
                clientUI.staminaValue = netCurStamina.Value / netMaxStamina.Value;
                clientUI.healthValue = netCurHealth.Value / netMaxHealth.Value;
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (IsLocalPlayer)
        {
            if (moveVector.x != 0 && moveVector.y != 0)
                calcSpeed = 0.9f * Speed;
            else calcSpeed = Speed;
            if (AttackingTime <= Time.time && !blocking && !perfectBlocking && !Input.GetKey(KeyCode.LeftShift) && blockTimer < Time.time && drinkTimer < Time.time)
            {
                SetAtkAnimServerRpc(false);
                SetVelocityServerRpc(moveVector * calcSpeed);
            }
            AnimatePlayerMovementServerRpc(moveVector.x, moveVector.y);

            if (!inputMap.Player.Block.IsPressed() && (blocking || perfectBlocking) && blockTimer < Time.time)
            {
                SetBlockFalse();
                SetBlockAnimServerRpc(false);
            }

        }      
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDmgServerRpc(float incDmg, DamageType type)
    {
        incDmg = Damage.CalculateDamage(incDmg, type, defStats);
        if (perfectBlocking)
        {
            anim.SetTrigger("BHit");
            ParrySoundClientRPC();
            incDmg = 0;
            if (netCurStamina.Value + recoveryRate * 250 < netMaxStamina.Value)
            {
                UpdateStaminaServerRpc(netCurStamina.Value + recoveryRate * 250);
            }
            else UpdateStaminaServerRpc(netMaxStamina.Value);
            BlockParticlesClientRPC();
        }
        else if (blocking && !perfectBlocking)
        {
            anim.SetTrigger("BHit");
            BlockSoundClientRPC();
            incDmg = incDmg * 0.25f;
        }
        if (netCurHealth.Value - incDmg <= 0)
        {
            UpdateHealthServerRpc(0);
        }
        else
        {
            UpdateHealthServerRpc(netCurHealth.Value - incDmg);
        }
    }

    [ClientRpc]
    void DisableInputClientRPC()
    {
        if (IsLocalPlayer)
            inputMap.Disable();
    }

    [ClientRpc]
    void BlockParticlesClientRPC()
    {
        Instantiate(blockParticles, this.transform.position, Quaternion.identity);
    }

    [ClientRpc]
    void BlockSoundClientRPC()
    {
        audioS.PlayOneShot(blockSound);
    }

    [ClientRpc]
    void ParrySoundClientRPC()
    {
        audioS.PlayOneShot(parrySound);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePotUsesServerRPC(int delta)
    {
        netPotUses.Value += delta;
    }

    [ServerRpc]
    void HealParticlesServerRPC()
    {
        Instantiate(healParticles, feet.position - new Vector3(0, 0.3f, 0), Quaternion.identity);
    }

    [ServerRpc]
    void HealAnimServerRPC()
    {
        netPotUses.Value--;
        anim.SetTrigger("DrinkPotion");
        RegenerationScript regenScr = gameObject.AddComponent<RegenerationScript>();
        regenScr.timer = Time.time + regenTime;
        regenScr.RegenParticles = Instantiate(regenParticles, feet.position, Quaternion.identity, transform);
        regenScr.regenRate = regenRate;
        RegenPartClientRPC();
    }

    [ClientRpc]
    void RegenPartClientRPC()
    {
        audioS.PlayOneShot(healSound, 2);
        if (!IsHost)
        {
            GameObject tempPart = Instantiate(regenParticles, feet.position, Quaternion.identity, transform);
            tempPart.AddComponent<SelfDestruct>().delay = regenTime;
        }
    }


    public void HealParticles()
    {
        if (IsLocalPlayer)
        {
            potCd = Time.time + regenTime;
            HealParticlesServerRPC();
        }
        if(!IsHost)
            Instantiate(healParticles, feet.position - new Vector3(0, 0.3f, 0), Quaternion.identity);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "EnemyAtk")
        {
            enemyAtkInfo = collision.GetComponent<AtkInfo>();
            if (enemyAtkInfo.enemyInfo)
            {
                enemyAtkId = enemyAtkInfo.enemyInfo.atkId;
                enemyAtkStats = enemyAtkInfo.enemyInfo.atkStats;
            }
            if (!hitId.Contains(enemyAtkId))
            {
                TakeDmgServerRpc(enemyAtkStats.BaseDamage, enemyAtkStats.Type);
                hitId.Add(enemyAtkId);
                StartCoroutine(ClearID(enemyAtkInfo.enemyInfo.AtkCd * 0.8f));
            }
        }
        else if(collision.tag == "EnemyProjectile")
        {
            enemyProjectile = collision.GetComponent<ProjectileScript>();
            if (enemyProjectile)
            {
                enemyAtkId = enemyProjectile.atkID;
                enemyAtkStats = enemyProjectile.stats;
                
            }
            if (!hitId.Contains(enemyAtkId))
            {
                TakeDmgServerRpc(enemyAtkStats.BaseDamage, enemyAtkStats.Type);
                hitId.Add(enemyAtkId);
                StartCoroutine(ClearID(0.3f));
                Destroy(collision.gameObject);
            }

        }
    }

    IEnumerator ClearID(float timer)
    {
        int selectedID = enemyAtkId;
        yield return new WaitForSeconds(timer);
        hitId.Remove(enemyAtkId);
    }

    IEnumerator clearUI()
    {
        yield return new WaitForSeconds(0.1f);
        NetworkManager.gameObject.GetComponent<WorldManager>().ClearUI();
    }

    public void ActivateUI()
    {
        if (!IsLocalPlayer)
        {
            mainUI = GameObject.FindGameObjectWithTag("MainUI").transform.GetChild(0).GetComponent<SliderUpdater>();
            clientUI = GameObject.FindGameObjectWithTag("ClientUI").transform.GetChild(0).GetComponent<SliderUpdater>();
            clientUI.gameObject.SetActive(true);
        }
        else
        {
            mainUI = GameObject.FindGameObjectWithTag("MainUI").transform.GetChild(0).GetComponent<SliderUpdater>();
            clientUI = GameObject.FindGameObjectWithTag("ClientUI").transform.GetChild(0).GetComponent<SliderUpdater>();
            mainUI.gameObject.SetActive(true);
        }
    }

    [ServerRpc]
    public void SetVelocityServerRpc(Vector2 direction)
    {
        RigBod.velocity = direction;
    }

    [ServerRpc]
    public void SetAtkAnimServerRpc(bool value)
    {
        anim.SetBool("Attacking", value);
    }
    [ServerRpc]
    public void SetMovingAnimServerRpc(bool value)
    {
        anim.SetBool("Moving", value);
    }


    [ServerRpc(RequireOwnership = false)]
    public void ResetAnimServerRPC()
    {
        anim.SetTrigger("South");
        anim.SetBool("Moving", true);
        SetVelocityServerRpc(Vector2.zero);
    }

    [ServerRpc]
    public void AnimatePlayerMovementServerRpc(float horizontal, float vertical)
    {
        switch (RigBod.velocity.x)
        {
            case > 0.1f:
                switch (RigBod.velocity.y)
                {
                    case > 0.1f:
                        anim.SetTrigger("NorthEast");
                        anim.SetBool("Moving", true);
                        break;
                    case < -0.1f:
                        anim.SetTrigger("SouthEast");
                        anim.SetBool("Moving", true);
                        break;
                    case 0:
                        anim.SetTrigger("East");
                        anim.SetBool("Moving", true);
                        break;
                }
                break;
            case < -0.1f:
                switch (RigBod.velocity.y)
                {
                    case > 0.1f:
                        anim.SetTrigger("NorthWest");
                        anim.SetBool("Moving", true);
                        break;
                    case < -0.1f:
                        anim.SetTrigger("SouthWest");
                        anim.SetBool("Moving", true);
                        break;
                    case 0:
                        anim.SetTrigger("West");
                        anim.SetBool("Moving", true);
                        break;
                }
                break;
            case 0:
                switch (RigBod.velocity.y)
                {
                    case > 0.1f:
                        anim.SetTrigger("North");
                        anim.SetBool("Moving", true);
                        break;
                    case < -0.1f:
                        anim.SetTrigger("South");
                        anim.SetBool("Moving", true);
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }

        if ((horizontal > -0.1f && horizontal < 0.1f) && (vertical > -0.1f && vertical < 0.1f))
            anim.SetBool("Moving", false);
    }


    [ServerRpc]
    public void AnimatePlayerAtkServerRpc(Vector2 dir)
    {
        atkCollider.right = dir;
        while(atkId == prevAtkId)
        {
            atkId = Random.Range(1, 10000);
        }
        prevAtkId = atkId;
        float angle = (180 / Mathf.PI) * Mathf.Atan(dir.y / dir.x);
        anim.SetBool("Attacking", true);
        //Debug.Log(angle);
        switch (dir.x)
        {
            case > 0f:
                switch (dir.y)
                {
                    case > 0f:
                        if (angle > 22.5f && angle < 67.5f)
                            anim.SetTrigger("AtkNorthEast");
                        else if (angle < 22.5f)
                            anim.SetTrigger("AtkEast");
                        else if (angle > 67.5f)
                            anim.SetTrigger("AtkNorth");
                        break;
                    case < -0f:
                        if (angle + 360 < 337.5f && angle + 360 > 292.5f)
                            anim.SetTrigger("AtkSouthEast");
                        else if (angle + 360 > 337.5f)
                            anim.SetTrigger("AtkEast");
                        else if (angle + 360 < 292.5f)
                            anim.SetTrigger("AtkSouth");
                        break;
                    default:
                        anim.SetTrigger("AtkEast");
                        break;
                }
                break;
            case < -0f:
                switch (dir.y)
                {
                    case > 0f:
                        if (angle + 180 > 112.5f && angle + 180 < 157.5f)
                            anim.SetTrigger("AtkNorthWest");
                        else if (angle + 180 > 157.5f)
                            anim.SetTrigger("AtkWest");
                        else if (angle + 180 < 112.5f)
                            anim.SetTrigger("AtkNorth");
                        break;
                    case < -0f:
                        if (angle + 180 > 202.5f && angle + 180 < 247.5f)
                            anim.SetTrigger("AtkSouthWest");
                        else if (angle + 180 > 247.5f)
                            anim.SetTrigger("AtkSouth");
                        else if (angle + 180 < 202.5f)
                            anim.SetTrigger("AtkWest");
                        break;
                    default:
                        anim.SetTrigger("AtkWest");
                        break;
                }
                break;
            default:
                switch (dir.y)
                {
                    case > 0f:
                        anim.SetTrigger("AtkNorth");
                        break;
                    case < -0f:
                        anim.SetTrigger("AtkSouth");
                        break;
                    default:
                        anim.SetTrigger("AtkSouth");
                        break;
                }
                break;
        }

    }

    [ServerRpc]
    public void SetBlockFalseServerRpc()
    {
        perfectBlocking = false;
        blocking = false;
    }    
    public void SetBlockFalse()
    {
        SetBlockFalseServerRpc();
        if (IsLocalPlayer)
        {
            perfectBlocking = false;
            blocking = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetBlockServerRpc()
    {
        perfectBlocking = false;
        blocking = true;
    }

    public void SetBlock()
    {
        SetBlockServerRpc();
        
        if (IsLocalPlayer)
        {
            perfectBlocking = false;
            blocking = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPerfectBlockServerRpc()
    {
        perfectBlocking = true;
    }

    public void SetPerfectBlock()
    {
        SetBlockServerRpc();

        if (IsLocalPlayer)
        {
            perfectBlocking = true;
        }
    }

    [ServerRpc]
    public void SetBlockAnimServerRpc(bool value)
    {
        anim.SetBool("BlockNorth", value);
        anim.SetBool("BlockNorthEast", value);
        anim.SetBool("BlockEast", value);
        anim.SetBool("BlockSouthEast", value);
        anim.SetBool("BlockSouth", value);
        anim.SetBool("BlockSouthWest", value);
        anim.SetBool("BlockWest", value);
        anim.SetBool("BlockNorthWest", value);
    }

    [ServerRpc]
    public void AnimatePlayerBlockServerRpc(Vector2 dir)
    {
        float angle = (180 / Mathf.PI) * Mathf.Atan(dir.y / dir.x);
        //Debug.Log(angle);
        switch (dir.x)
        {
            case > 0f:
                switch (dir.y)
                {
                    case > 0f:
                        if (angle > 22.5f && angle < 67.5f)
                            anim.SetBool("BlockNorthEast", true);
                        else if (angle < 22.5f)
                            anim.SetBool("BlockEast", true);
                        else if (angle > 67.5f)
                            anim.SetBool("BlockNorth", true);
                        break;
                    case < -0f:
                        if (angle + 360 < 337.5f && angle + 360 > 292.5f)
                            anim.SetBool("BlockSouthEast", true);
                        else if (angle + 360 > 337.5f)
                            anim.SetBool("BlockEast", true);
                        else if (angle + 360 < 292.5f)
                            anim.SetBool("BlockSouth", true);
                        break;
                    default:
                        anim.SetBool("BlockEast", true);
                        break;
                }
                break;
            case < -0f:
                switch (dir.y)
                {
                    case > 0f:
                        if (angle + 180 > 112.5f && angle + 180 < 157.5f)
                            anim.SetBool("BlockNorthWest", true);
                        else if (angle + 180 > 157.5f)
                            anim.SetBool("BlockWest", true);
                        else if (angle + 180 < 112.5f)
                            anim.SetBool("BlockNorth", true);
                        break;
                    case < -0f:
                        if (angle + 180 > 202.5f && angle + 180 < 247.5f)
                            anim.SetBool("BlockSouthWest", true);
                        else if (angle + 180 > 247.5f)
                            anim.SetBool("BlockSouth", true);
                        else if (angle + 180 < 202.5f)
                            anim.SetBool("BlockWest", true);
                        break;
                    default:
                        anim.SetBool("BlockWest", true);
                        break;
                }
                break;
            default:
                switch (dir.y)
                {
                    case > 0f:
                        anim.SetBool("BlockNorth", true);
                        break;
                    case < -0f:
                        anim.SetBool("BlockSouth", true);
                        break;
                    default:
                        anim.SetBool("BlockSouth", true);
                        break;
                }
                break;
        }

    }
    public override void OnNetworkSpawn()
    {
        netCurHealth.OnValueChanged += OnStateChanged;
        netPotUses.OnValueChanged += OnPotChanged;
        netCurHealth.Value = curHealth;
        netCurStamina.Value = curStamina;
        netMaxHealth.Value = maxHealth;
        netMaxStamina.Value = maxStamina;
        dead.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        netCurHealth.OnValueChanged -= OnStateChanged;
        netPotUses.OnValueChanged -= OnPotChanged;
    }

    public void OnStateChanged(float previous, float current)
    {
        if(float.IsNaN(current))
        {
            UpdateHealthServerRpc(previous);
        }
        if (current == 0)
        {
            UpdateDeadServerRpc(true);
            SetVelocityServerRpc(Vector2.zero);
            SetMovingAnimServerRpc(false);
            DisableInputClientRPC();
            anim.StopPlayback();
            anim.SetTrigger("Death");
            UpdateDeadServerRpc(true);
            foreach (Collider2D col in colliders)
            {
                col.enabled = false;
            }
        }
    }   
    public void OnPotChanged(int previous, int current)
    {
        if (IsLocalPlayer)
            mainUI.UpdateIcons(current);
        else clientUI.UpdateIcons(current);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateHealthServerRpc(float newHealth)
    {
        netCurHealth.Value = newHealth;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateDeadServerRpc(bool value)
    {
        dead.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateStaminaServerRpc(float newStamina)
    {
        netCurStamina.Value = newStamina;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGoldServerRPC(int delta)
    {
        gold.Value += delta;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateDmgServerRPC(int delta)
    {
        if (!IsLocalPlayer)
        {
            atkStats.BaseDamage += delta;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateMaxHpServerRPC(int delta)
    {
        netMaxHealth.Value += delta;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateMaxStaminaServerRPC(int delta)
    {
        netMaxStamina.Value += delta;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateArmorServerRPC(int delta, int delta2)
    {
        if (!IsLocalPlayer)
        {
            defStats.Armor += delta;
            defStats.SpellArmor += delta2;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePotServerRPC(int delta, float delta2)
    {
        if (!IsLocalPlayer)
        {
            regenTime += delta;
            regenRate += delta2;
        }
    }


    public void PlayWalkSound()
    {
        audioS.PlayOneShot(walkSounds[Random.Range(0, walkSounds.Count)], 0.25f);
    }

    public void PlaySwingSound()
    {
        audioS.PlayOneShot(swingSound);
    }

    public void PlayDeathSound()
    {
        audioS.PlayOneShot(deathSound);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetCharacterServerRPC()
    {
        netMaxHealth.Value = baseHealth;
        netMaxStamina.Value = baseStamina;
        netCurHealth.Value = baseHealth;
        netCurStamina.Value = baseStamina;
        netPotUses.Value = 2;
        gold.Value = 0;
        ResetCharacterClientRPC();
    }


    [ClientRpc]
    public void ResetCharacterClientRPC()
    {
        atkStats = baseAtkStats;
        defStats = baseDefStats;
        regenRate = baseRegenRate;
        regenTime = baseRegenTime;

        armorLevel = 0;
        swordLevel = 0;
        survivalLevel = 0;
        enduranceLevel = 0;
        potionLevel = 0;
            
    }

    [ClientRpc]
    public void SetInputClientRPC(bool value)
    {
        if (value)
            inputMap.Enable();
        else inputMap.Disable();
    }



}
