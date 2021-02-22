using UnityEngine;
using System.Collections;

public class ChangeRenderQueue : MonoBehaviour {

	void Start () {
        GetComponent<MeshRenderer>().material.renderQueue = 2002; //保证在JustWriteZ着色器之后渲染
    }
}
