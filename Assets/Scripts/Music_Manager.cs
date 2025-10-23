using Unity.VisualScripting;
using UnityEngine;

public class Music_Manager : MonoBehaviour
{
     [SerializeField]private AudioSource audioSource;
     [SerializeField] private AudioClip clip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
