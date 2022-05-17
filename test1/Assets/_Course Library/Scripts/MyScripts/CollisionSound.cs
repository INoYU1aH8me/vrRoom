using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSound : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hitSound;
    [SerializeField] Rigidbody rigidBody;   
    private void OnCollisionEnter(Collision collision)
    {
        audioSource.PlayOneShot(hitSound);
    }
}
