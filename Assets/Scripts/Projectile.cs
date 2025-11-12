using System;
using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 30f;
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

            // Start coroutine that handles both wait and projectile destruction
            StartCoroutine(WaitAndDestroy(rend));
        }
    }

    private IEnumerator WaitAndDestroy(Renderer renderer)
    {
        yield return new WaitForSeconds(0.5f);
        if (renderer != null)
            renderer.material.color = Color.white;

        // Now destroy the projectile
        Destroy(gameObject);
    }


}
