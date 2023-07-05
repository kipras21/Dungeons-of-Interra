using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionBlocker : MonoBehaviour
{

    public Collider2D Col1;
    public Collider2D Col2;



    void Start()
    {
        Physics2D.IgnoreCollision(Col1, Col2);
    }

}
