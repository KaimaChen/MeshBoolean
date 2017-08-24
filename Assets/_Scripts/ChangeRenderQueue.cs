using UnityEngine;
using System.Collections;

public class ChangeRenderQueue : MonoBehaviour {

	void Start () {
        GetComponent<MeshRenderer>().material.renderQueue = 2002;
    }
	
	void Update () {
	
	}
}
