using UnityEngine;
using System.Collections;

public class PassClipPlaneData : MonoBehaviour {
    public Transform ClipPlane;
    public Material ClipByPlaneMat;
    
	void Update () {
	    if(ClipPlane != null && ClipByPlaneMat)
        {
            ClipByPlaneMat.SetVector("_PlanePos", ClipPlane.position);
            ClipByPlaneMat.SetVector("_PlaneNormal", ClipPlane.up);
        }
	}
}
