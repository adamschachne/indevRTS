using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ApplyMaterialToUIOnStart : MonoBehaviour {
    public Material materialToSet;
    // Start is called before the first frame update
    void Start () {
        GetComponent<Image> ().material = materialToSet;
    }

}