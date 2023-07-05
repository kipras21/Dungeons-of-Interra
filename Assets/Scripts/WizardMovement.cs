using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WizardMovement : EnemyMovement
{

    public float aggroRange;
    public Transform firingPoint;
    public float projectileCd = 2.5f;
    protected float projectileCdTime;
    protected Vector2 newPosition;
    public GameObject ShootParticles;
    public GameObject Projectile;
    public LayerMask rayCastFilter;
    protected float tpTimer;


    // Start is called before the first frame update
    void Start()
    {
       anim = this.GetComponent<Animator>();
        AtkCdTime = 0;
        AttackingTime = 0;
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
            col = Physics2D.OverlapCircle(feet.position, aggroRange, whatIsPlayer);
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
                dir = (enemyPos - feet.position).normalized;
                if (AttackingTime <= Time.time)
                {
                    if (distance <= range)
                    {
                        
                        if (!CheckRaycast())
                        {
                            AttackingTime = 0;
                            inRange = false;
                        }
                        else inRange = true;
                    }
                    else
                    {
                        inRange = false;
                        AttackingTime = 0;
                    }
                }
                else
                {
                    if (distance <= range * 1.5f)
                    {

                        if (!CheckRaycast())
                        {
                            AttackingTime = 0;
                            inRange = false;
                        }
                        else inRange = true;
                        if (projectileCdTime < Time.time)
                        {
                            StartCoroutine(ShootProjectile());
                        }
                    }
                    else
                    {
                        inRange = false;
                        AttackingTime = 0;

                    }
                }
            }

            if (!enemyScr)
                dir = Vector2.zero;

            horizontal = dir.x;
            vertical = dir.y;

            if ((horizontal > -0.1f && horizontal < 0.1f) && (vertical > -0.1f && vertical < 0.1f))
                calcSpeed = 0.75f * Speed;
            else calcSpeed = Speed;
            if (AttackingTime <= Time.time && !inRange && tpTimer <= Time.time)
            {
                SetVelocityServerRpc(new Vector2(horizontal * calcSpeed, vertical * calcSpeed));
                AnimateEnemyMovementServerRpc(dir.x, dir.y);
            }
            if (inRange || CheckRaycast())
            {
                SetVelocityServerRpc(Vector2.zero);
                SetMovingAnimServerRpc(false);
                if (AtkCdTime <= Time.time && AttackingTime <= Time.time && tpTimer <= Time.time)
                {
                    AnimateEnemyAtkServerRpc(dir.x, dir.y);
                }
            }
            else if (AttackingTime >= Time.time || tpTimer >= Time.time)
            {
                SetVelocityServerRpc(Vector2.zero);
                SetMovingAnimServerRpc(false);
            }
        }
    }

    public override void OnStateChanged(float previous, float current)
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
            foreach (Collider2D col in colliders)
            {
                col.enabled = false;
            }
        }
        else if (current != netMaxHealth.Value && current < previous)
        {
            if (IsHost)
            {
                int kk = 0;
                newPosition = new Vector2(feet.position.x + Random.Range(-6, 6f), feet.position.y + Random.Range(-6, 6f));
                while (kk < 1000)
                {
                    kk++;
                    newPosition = new Vector2(feet.position.x + Random.Range(-6, 6f), feet.position.y + Random.Range(-6, 6f));
                    if (Physics2D.OverlapCircleAll(newPosition, 0.1f, LayerMask.GetMask("Floor")).Length > 0 && Physics2D.OverlapCircleAll(newPosition, 0.75f, LayerMask.GetMask("Wall")).Length == 0)
                            break;
                    
                }

                if (Physics2D.OverlapCircleAll(newPosition, 0.01f, LayerMask.GetMask("Floor")).Length == 0 )
                {
                    newPosition = feet.position;
                }
                    
                
                
                AttackingTime = 0;
                AnimateTpOutServerRPC();
                tpTimer = Time.time + 1.2f;
            }
        }
    }


    private bool CheckRaycast()
    {    
        try
        {
            if (Physics2D.Raycast(firingPoint.position + new Vector3(0, 0.2f, 0), (enemyPos - firingPoint.position).normalized, range*1.5f, rayCastFilter).collider.tag != "Player")
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
        return true;
    }


    [ServerRpc]
    public void AnimateTpOutServerRPC()
    {
        anim.SetTrigger("TpOut");
        UpdateCollidersOff();
    }

    [ServerRpc]
    public void AnimateTpInServerRPC()
    {
        anim.SetTrigger("TpIn");

    }

    public void UpdatePosition()
    {
        if (IsHost)
        {
            UpdatePositionServerRPC();
            AnimateTpInServerRPC();
        }
    }

    public void UpdateCollidersOn()
    {

        UpdateColliders(true);

    }

    public void UpdateCollidersOff()
    {

        UpdateColliders(false);

    }

    public void UpdateColliders(bool value)
    {
        foreach(Collider2D col in colliders)
        {
            col.enabled = value;
        }
    }

    [ServerRpc]
    public void UpdatePositionServerRPC()
    {
        transform.position = newPosition - new Vector2(0, Mathf.Abs(transform.position.y) - Mathf.Abs(feet.position.y));
    }

    [ServerRpc]
    public override void AnimateEnemyAtkServerRpc(float horizontal, float vertical)
    {
        while (atkId == prevAtkId)
        {
            atkId = Random.Range(1, 10000);
        }
        prevAtkId = atkId;
        
        anim.SetTrigger("Atk");
        projectileCdTime = Time.time + 0.8f;             
        AttackingTime = Time.time + 9999;
        AtkCdTime = Time.time + AtkCd;
    }


    public IEnumerator ShootProjectile()
    {
        if (IsHost)
        {
            projectileCdTime = Time.time + projectileCd;
            ShootParticlesClientRPC();
            yield return new WaitForSeconds(0.3f);
            dir = (enemyPos + new Vector3(0, 0.3f, 0) - firingPoint.position).normalized;
            ShootProjectileServerRpc(dir.x, dir.y);
        }
    }

    [ClientRpc]
    public void ShootParticlesClientRPC()
    {
        Instantiate(ShootParticles, firingPoint.position, Quaternion.identity);
    }

    [ServerRpc]
    public void ShootProjectileServerRpc(float horizontal, float vertical)
    {
        while (atkId == prevAtkId)
        {
            atkId = Random.Range(1, 10000);
        }
        prevAtkId = atkId;
        ProjectileScript proj = Instantiate(Projectile, firingPoint.position, Quaternion.identity).GetComponent<ProjectileScript>();
        proj.GetComponent<NetworkObject>().Spawn();
        proj.direction = new Vector2(horizontal, vertical);
        proj.stats = atkStats;
        proj.enemyInfo = this;
        AtkCdTime = Time.time + AtkCd;
    }
}
