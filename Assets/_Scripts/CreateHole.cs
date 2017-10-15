using UnityEngine;
using System.Collections;

public class CreateHole : MonoBehaviour {
    public float radius = 1.0f;
    public bool enableCollide = true;

    void OnCollisionEnter(Collision collision)
    {
        if(enableCollide)
        {
            enableCollide = false;
            GroundManager.Instance.CreateHole(transform.position, radius);
        }
    }
}
