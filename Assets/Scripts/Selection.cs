using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selection : MonoBehaviour {


    List<GameObject> selectedUnits;
    //GameObject selectedUnit;

	// Use this for initialization
	void Start () {
        //selectedUnit = null;
        selectedUnits = new List<GameObject>();
    }
	
	// Update is called once per frame
	void Update () {
        // Left Shift + Left Click
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0))
        {   
            RaycastHit hitInfo = new RaycastHit();
            if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)))
            {
                if (hitInfo.transform.gameObject.tag == "Unit")
                {
                    this.AddSelection(hitInfo.transform.gameObject);
                }
            }
            return;
        }

        // Left Ctrl/Cmd + Left Click
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)) && Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo = new RaycastHit();
            if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)))
            {
                if (hitInfo.transform.gameObject.tag == "Unit")
                {
                    this.Deselect(hitInfo.transform.gameObject);
                }
            }
            return;
        }

        // Only left clicks
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo = new RaycastHit();
            if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)))
            {
                if (hitInfo.transform.gameObject.tag == "Unit")
                {
                    this.Select(hitInfo.transform.gameObject);
                    return;
                } 
            }
            this.DeselectAll();
        }
    }

    void Deselect(GameObject target)
    {
        int selectedIndex = this.selectedUnits.IndexOf(target);
        if (selectedIndex != -1)
        {
            target.GetComponent<Outline>().OutlineWidth = 0.0f;
            this.selectedUnits.RemoveAt(selectedIndex);
        }
    }
    
    void DeselectAll()
    {
        //Debug.Log("Deselect All");
        foreach (GameObject unit in this.selectedUnits)
        {
            unit.GetComponent<Outline>().OutlineWidth = 0.0f;
        }
        this.selectedUnits.Clear();
    }

    void Select(GameObject target)
    {
        this.DeselectAll();
        target.GetComponent<Outline>().OutlineWidth = 6.0f;
        this.selectedUnits.Add(target);
    }

    void AddSelection(GameObject target)
    {
        this.selectedUnits.Add(target);
        target.GetComponent<Outline>().OutlineWidth = 6.0f;
    }
}

