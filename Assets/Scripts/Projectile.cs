using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Projectile : MonoBehaviour
{
    
    public float speed = 1000f;
    public float lifeTime = 5f;


    void Start()
    {
        Destroy(gameObject, lifeTime);
        
        
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            EnemyAI ea = other.gameObject.GetComponent<EnemyAI>();
            ea.hp -= 10;
            if (ea.hp <= 0)
            {
                Destroy(ea.gameObject);
            }

            Renderer rend = other.gameObject.GetComponent<Renderer>();
            rend.material.color = Color.red;


        }
        else
        {


            Destroy(gameObject);
        }
    }

 

}
