using System;
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
        Destroy(this.gameObject);
    }
}