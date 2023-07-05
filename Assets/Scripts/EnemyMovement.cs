using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyMovement : NetworkBehaviour
{

    public float Speed;
    protected float calcSpeed;
    public Rigidbody2D RigBod;
    protected Animator anim;
    protected float AtkCdTime = 0;
    public float AtkCd;
    protected float AttackingTime = 0;
    public float AtkDuration;
    protected int prevAtkId = 0;
    [HideInInspector]
    public int atkId = 0;
    protected List<int> hitId = new List<int>();
    protected int enemyAtkId;
    public LayerMask whatIsPlayer;
    public Transform feet;
    protected Collider2D col;
    protected Vector2 dir;
    protected Vector3 enemyPos;
    protected PlayerMovement enemyScr;
    protected PlayerMovement enemyAtkScr;
    protected AtkInfo enemyAtkInfo;
    protected AtkStats enemyAtkStats;
    protected float distance;
    protected float horizontal;
    protected float vertical;
    public float range;
    public bool inRange;
    public Transform atkCollider;

    [HideInInspector]
    public NetworkVariable<float> netCurHealth = new NetworkVariable<float>();
    [Header("Char Stats")]
    public float curHealth;
    [HideInInspector]
    public NetworkVariable<float> netMaxHealth = new NetworkVariable<float>();
    public float maxHealth;
    public DefenseStats defStats;
    public AtkStats atkStats;

    public Slider healthSlider;

    public List<Collider2D> colliders;

    protected bool dead;

    [SerializeField]
    public List<BoxScript.Loot> loot;
    // Start is called before the first frame update
    void Start()
    {
        anim = this.GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        healthSlider.value = netCurHealth.Value / maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsHost && !dead)
        {
            
            col = Physics2D.OverlapCircle(feet.position, 5, whatIsPlayer);
            if (col)
            {
                if (col.GetComponentInParent<PlayerMovement>())
                {
                    enemyScr = col.GetComponentInParent<PlayerMovement>();
                }
            }
            if (enemyScr)
            {
                enemyPos = enemyScr.feet.position;
                distance = Mathf.Sqrt(Mathf.Pow(enemyPos.x - feet.position.x, 2) + Mathf.Pow(enemyPos.y - feet.position.y, 2));
                //Debug.Log(distance);
                dir = (enemyPos - feet.position).normalized;
                if (distance <= range)
                {
                    inRange = true;
                }
                else
                {
                    inRange = false;
                }
            }

            if (!enemyScr)
                dir = Vector2.zero;

            horizontal = dir.x;
            vertical = dir.y;

            if ((horizontal > -0.1f && horizontal < 0.1f) && (vertical > -0.1f && vertical < 0.1f))
                calcSpeed = 0.75f * Speed;
            else calcSpeed = Speed;
            if (AttackingTime <= Time.time && !inRange)
            {
                SetVelocityServerRpc(new Vector2(horizontal * calcSpeed, vertical * calcSpeed));
                AnimateEnemyMovementServerRpc(dir.x, dir.y);
            }
            if (inRange)
            {
                SetVelocityServerRpc(Vector2.zero);
                SetMovingAnimServerRpc(false);
                if (AtkCdTime <= Time.time)
                {
                    AnimateEnemyAtkServerRpc(dir.x, dir.y);
                }
            }
            else if (AttackingTime >= Time.time)
            {
                SetVelocityServerRpc(Vector2.zero);
                SetMovingAnimServerRpc(false);
            }
        }

    }

    [ServerRpc]
    public void TakeDmgServerRpc(float incDmg, DamageType type)
    {
        incDmg = Damage.CalculateDamage(incDmg, type, defStats);
        UpdateHealthServerRpc(netCurHealth.Value - incDmg);  
    }

    protected void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag == "PlayerAtk")
        {
            enemyAtkInfo = collision.GetComponent<AtkInfo>();
            if(enemyAtkInfo.playerInfo)
            {
                enemyAtkId = enemyAtkInfo.playerInfo.atkId;
                enemyAtkStats = enemyAtkInfo.playerInfo.atkStats;
            }
            else
            {
                enemyAtkId = enemyAtkInfo.atkId;
                enemyAtkStats = enemyAtkInfo.projectileInfo;
            }
            if (!hitId.Contains(enemyAtkId))
            {
                TakeDmgServerRpc(enemyAtkStats.BaseDamage, enemyAtkStats.Type);
                hitId.Add(enemyAtkId);
                ClearID(0.4f);
            }
        }
    }

    protected IEnumerator ClearID(float timer)
    {
        int selectedID = enemyAtkId;
        yield return new WaitForSeconds(timer);
        hitId.Remove(enemyAtkId);
    }

    [ServerRpc]
    public void SetVelocityServerRpc(Vector2 direction)
    {
        RigBod.velocity = direction;
    }

    [ServerRpc]
    public void SetMovingAnimServerRpc(bool value)
    {
        anim.SetBool("Moving", value);
    }

    [ServerRpc]
    public void AnimateEnemyMovementServerRpc(float horizontal, float vertical)
    {
        switch (RigBod.velocity.x)
        {
            case > 0.2f:
                switch (RigBod.velocity.y)
                {
                    case > 0.2f:
                        anim.SetTrigger("NorthEast");
                        anim.SetBool("Moving", true);
                        break;
                    case < -0.2f:
                        anim.SetTrigger("SouthEast");
                        anim.SetBool("Moving", true);
                        break;
                    default:
                        anim.SetTrigger("East");
                        anim.SetBool("Moving", true);
                        break;
                }
                break;
            case < -0.2f:
                switch (RigBod.velocity.y)
                {
                    case > 0.2f:
                        anim.SetTrigger("NorthWest");
                        anim.SetBool("Moving", true);
                        break;
                    case < -0.2f:
                        anim.SetTrigger("SouthWest");
                        anim.SetBool("Moving", true);
                        break;
                    default:
                        anim.SetTrigger("West");
                        anim.SetBool("Moving", true);
                        break;
                }
                break;
            default:
                switch (RigBod.velocity.y)
                {
                    case > 0.2f:
                        anim.SetTrigger("North");
                        anim.SetBool("Moving", true);
                        break;
                    case < -0.2f:
                        anim.SetTrigger("South");                    
                        anim.SetBool("Moving", true);
                        break;
                    default:
                        break;
                }
                break;
        }

        if ((horizontal > -0.1f && horizontal < 0.1f) && (vertical > -0.1f && vertical < 0.1f))
            anim.SetBool("Moving", false);
    }

    [ServerRpc]
    public virtual void AnimateEnemyAtkServerRpc(float horizontal, float vertical)
    {
        atkCollider.right = new Vector2(horizontal, vertical);
        while (atkId == prevAtkId)
        {
            atkId = Random.Range(1, 10000);
        }
        prevAtkId = atkId;
        switch (horizontal)
        {
            case > 0.2f:
                switch (vertical)
                {
                    case > 0.2f:
                        anim.SetTrigger("AtkNorthEast");
                        break;
                    case < -0.2f:
                        anim.SetTrigger("AtkSouthEast");
                        break;
                    default:
                        anim.SetTrigger("AtkEast");
                        break;
                }
                break;
            case < -0.2f:
                switch (vertical)
                {
                    case > 0.2f:
                        anim.SetTrigger("AtkNorthWest");
                        break;
                    case < -0.2f:
                        anim.SetTrigger("AtkSouthWest");
                        break;
                    default:
                        anim.SetTrigger("AtkWest");
                        break;
                }
                break;
            default:
                switch (vertical)
                {
                    case > 0.2f:
                        anim.SetTrigger("AtkNorth");
                        break;
                    case < -0.2f:
                        anim.SetTrigger("AtkSouth");
                        break;
                    default:
                        break;
                }
                break;
        }
        AttackingTime = Time.time + AtkDuration;
        AtkCdTime = Time.time + AtkCd;
    }


    public override void OnNetworkSpawn()
    {
        netCurHealth.OnValueChanged += OnStateChanged;
        netCurHealth.Value = curHealth;
        netMaxHealth.Value = maxHealth;
    }

    public override void OnNetworkDespawn()
    {
        netCurHealth.OnValueChanged -= OnStateChanged;
    }

    public virtual void OnStateChanged(float previous, float current)
    {
        if (netCurHealth.Value <= 0)
        {
            if (IsHost)
            {
                DropLootServerRPC();
                Destroy(gameObject, 15);
                SetVelocityServerRpc(Vector2.zero);
                SetMovingAnimServerRpc(false);
            }
            dead = true;
            anim.StopPlayback();
            anim.SetTrigger("Death");  
            healthSlider.gameObject.SetActive(false);
            foreach(Collider2D col in colliders)
            {
                col.enabled = false;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateHealthServerRpc(float newHealth)
    {
        netCurHealth.Value = newHealth;      
    }

    [ServerRpc]
    protected void DropLootServerRPC()
    {
        foreach (BoxScript.Loot drop in loot)
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
    }
}