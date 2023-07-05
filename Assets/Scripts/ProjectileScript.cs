using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileScript : NetworkBehaviour
{
    public float speed;
    public Vector2 direction;
    public AtkStats stats;
    public int atkID;
    public EnemyMovement enemyInfo;
    public GameObject hitParticles;
    private Rigidbody2D rigBody;

    // Start is called before the first frame update
    void Start()
    {
        rigBody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rigBody.velocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (IsHost)
        {
            if (col.gameObject.layer == 10)
            {
                if (hitParticles)
                    SpawnHitParticlesClientRPC();
                gameObject.GetComponent<NetworkObject>().Despawn();
                Destroy(gameObject);
            }
        }
    }

    [ClientRpc]
    void SpawnHitParticlesClientRPC()
    {
        Instantiate(hitParticles, transform.position, Quaternion.identity);
    }
}
