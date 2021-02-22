using UnityEngine;
using System.Collections;

public class PassHoleData : MonoBehaviour {
    public Transform Hole;
    public Material ClipHoleByDistanceMat;
    
	void Update () {
	    if(Hole != null && ClipHoleByDistanceMat != null)
        {
            ClipHoleByDistanceMat.SetVector("_HolePos", Hole.position);
        }
	}
}
