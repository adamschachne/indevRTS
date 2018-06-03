using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selection : MonoBehaviour {

    public GameObject SelectionCircle;
    HashSet<GameObject> selectedUnits;
    bool leftClickHeld;
    enum Mode { None, Remove, Add };
    Mode selectionMode;
    Vector3 mousePositionInitial;

    // Use this for initialization
    void Start() {
        leftClickHeld = false;
        mousePositionInitial = new Vector3(0.0f, 0.0f, 0.0f);
        selectionMode = Mode.None;
        selectedUnits = new HashSet<GameObject>();
    }

    // Update is called once per frame
    void Update() {

        // Left Shift
        if (Input.GetKey(KeyCode.LeftShift)) {
            selectionMode = Mode.Add;
        } else if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl)) {
            selectionMode = Mode.Remove;
        } else {
            selectionMode = Mode.None;
        }

        // Left Click
        if (Input.GetMouseButtonDown(0)) {
            leftClickHeld = true;
            mousePositionInitial = Input.mousePosition;
        }

        // Right Click
        if (Input.GetMouseButtonDown(1)) {            
            RaycastHit hitInfo = new RaycastHit();
            if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))) {
                GameObject obj = hitInfo.transform.gameObject;
                if (obj.layer == LayerMask.NameToLayer("Ground")) {
                    foreach (GameObject unit in selectedUnits) {
                        unit.GetComponent<Movement>().moveTo(hitInfo.point);
                    }
                }
            }
        }

        // Releases Left click
        if (Input.GetMouseButtonUp(0)) {            
            if (Vector3.Distance(Input.mousePosition, mousePositionInitial) == 0.0f) {
                RaycastHit hitInfo = new RaycastHit();
                if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))) {
                    GameObject obj = hitInfo.transform.gameObject;
                    //if (obj.tag == "Unit")
                    if (obj.GetComponent<Selectable>() != null) {
                        if (this.selectionMode == Mode.Remove) {
                            RemoveSelection(obj);
                        } else if (this.selectionMode == Mode.Add) {
                            AddSelection(obj);
                        } else if (this.selectionMode == Mode.None) {
                            DeselectAll();
                            AddSelection(obj);
                        }
                    }
                }
            } else {
                if (this.selectionMode == Mode.None) {
                    this.DeselectAll();
                }
                foreach (Selectable selectableObject in FindObjectsOfType<Selectable>()) {
                    GameObject obj = selectableObject.gameObject;
                    if (IsWithinSelectionBounds(obj)) {
                        if (this.selectionMode == Mode.Remove) {
                            RemoveSelection(obj);
                        } else {
                            AddSelection(obj);
                        }
                    }
                }
            }
            leftClickHeld = false;
        }
    }
    
    void OnGUI() {
        if (leftClickHeld) {
            // Create a rect from both mouse positions
            var rect = Utils.GetScreenRect(mousePositionInitial, Input.mousePosition);
            Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Utils.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }
    }

    // Currently only point based TODO
    public bool IsWithinSelectionBounds(GameObject gameObject) {
        if (!leftClickHeld)
            return false;
       
        Bounds viewportBounds = Utils.GetViewportBounds(Camera.main, mousePositionInitial, Input.mousePosition);
        return viewportBounds.Contains(Camera.main.WorldToViewportPoint(gameObject.transform.position));
    }

    void RemoveSelection(GameObject target) {
        bool selectedIndex = this.selectedUnits.Contains(target);
        if (selectedIndex) {
            //target.GetComponent<Outline>().OutlineWidth = 0.0f;
            Selectable selected = target.GetComponent<Selectable>();
            if (selected.selectionCircle != null) {
                Destroy(selected.selectionCircle.gameObject);
                selected.selectionCircle = null;
            }
            this.selectedUnits.Remove(target);
        }
    }

    void DeselectAll() {
        //Debug.Log("Deselect All");
        foreach (GameObject unit in this.selectedUnits) {
            //unit.GetComponent<Outline>().OutlineWidth = 0.0f;
            Selectable selected = unit.GetComponent<Selectable>();
            if (selected.selectionCircle != null) {
                Destroy(selected.selectionCircle.gameObject);
                selected.selectionCircle = null;
            }            
        }

        // clear selected array
        this.selectedUnits.Clear();
    }

    void AddSelection(GameObject target) {
        this.selectedUnits.Add(target);
        //target.GetComponent<Outline>().OutlineWidth = 6.0f;
        Selectable selected = target.GetComponent<Selectable>();
        if (selected.selectionCircle == null) {
            selected.selectionCircle = Instantiate(SelectionCircle);
            selected.selectionCircle.transform.SetParent(selected.transform, false);
        }

        // add to selected list
        this.selectedUnits.Add(target);
    }              
}

