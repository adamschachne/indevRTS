using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputActions;

public class Selection : MonoBehaviour {

    public GameObject SelectionCircle;
    HashSet<GameObject> selectedUnits;
    HashSet<GameObject>[] controlGroups;
    List<ActionType> controlGroupTypes;
    bool leftClickHeld;
    bool attacking;
    enum Mode { None, Remove, Add };
    Mode selectionMode;
    Vector3 mousePositionInitial;
    Vector3[] selFrustumCorners;
    public LayerMask movementLayerMask;
    const float MIN_LENGTH = 0.0005f;
    private StateManager state;

    // Use this for initialization
    void Start() {
        state = StateManager.state;
        mousePositionInitial = new Vector3(0.0f, 0.0f, 0.0f);
        movementLayerMask = 1 << LayerMask.NameToLayer("Ground");
        state.input.Subscribe(SelectDown, RTS.SELECT_DOWN);
        state.input.Subscribe(SelectUp, RTS.SELECT_UP);
        state.input.Subscribe(Move, RTS.MOVE);
        state.input.Subscribe(Stop, RTS.STOP);
        state.input.Subscribe(ToggleAttackOn, RTS.ATTACK);
        state.input.Subscribe(ControlGroup, RTS.CONTROL_GROUP_1);

        controlGroupTypes = state.input.getTaggedActions("Control Group", typeof(RTS));
        attacking = false;

        controlGroups = new HashSet<GameObject>[controlGroupTypes.Count];
        for(int i = 0; i < controlGroups.Length; ++i) {
            controlGroups[i] = new HashSet<GameObject>();
            state.input.Subscribe(ControlGroup, controlGroupTypes[i]);
        }
    }

    private void OnEnable() {
        leftClickHeld = false;
        selectionMode = Mode.None;
        selectedUnits = new HashSet<GameObject>();
        attacking = false;
    }

    private void OnDisable() {
        leftClickHeld = false;
        selectionMode = Mode.None;
        selectedUnits.Clear();
        attacking = false;
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

        /* DEBUG RAY */
        //if (selFrustumCorners == null) {
        //    return;
        //}
        //foreach (Vector3 ray in selFrustumCorners) {
        //    Debug.DrawRay(Camera.main.transform.position, ray, Color.yellow);
        //}
        /* END DEBUG RAY */
    }

    void OnGUI() {
        if (leftClickHeld) {
            // Create a rect from both mouse positions
            var rect = Utils.GetScreenRect(mousePositionInitial, Input.mousePosition);
            Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Utils.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));

            /* DEBUG RAY */
            //float x = rect.x / Screen.width;
            //float y = 1.0f - rect.y / Screen.height;
            //float xMax = rect.xMax / Screen.width;
            //float yMax = 1.0f - rect.yMax / Screen.height;

            //Rect selRect = new Rect(x, y, Mathf.Abs(xMax - x), yMax - y);
            //Camera camera = Camera.main;
            ////Vector3[] selFrustumCorners = new Vector3[4];
            //selFrustumCorners = new Vector3[4];
            //camera.CalculateFrustumCorners(selRect, camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, selFrustumCorners);
            //for (int i = 0; i < 4; i++) {
            //    // transform to world space
            //    selFrustumCorners[i] = camera.transform.TransformVector(selFrustumCorners[i]);
            //    //Debug.DrawRay(camera.transform.position, selFrustumCorners[i], Color.yellow);      
            //}
            /* END DEBUG RAY */
        }
    }

    public List<GameObject> withinSelectionBounds(Selectable[] selObjects) {
        var rect = Utils.GetScreenRect(mousePositionInitial, Input.mousePosition);
        //Bounds viewportBounds = Utils.GetViewportBounds(Camera.main, mousePositionInitial, Input.mousePosition);

        float x = rect.x / Screen.width;
        float y = 1.0f - rect.y / Screen.height;
        float xMax = rect.xMax / Screen.width;
        float yMax = 1.0f - rect.yMax / Screen.height;

        float width = Mathf.Abs(xMax - x);
        float height = yMax - y;
        width = width == 0 ? MIN_LENGTH : width;
        height = height == 0 ? MIN_LENGTH : height;
        Rect selRect = new Rect(x, y, width, height);
        Camera camera = Camera.main;

        selFrustumCorners = new Vector3[4];
        camera.CalculateFrustumCorners(selRect, camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, selFrustumCorners);
        for (int i = 0; i < 4; i++) {
            // transform to world space
            selFrustumCorners[i] = camera.transform.TransformVector(selFrustumCorners[i]);
            //Debug.DrawRay(camera.transform.position, selFrustumCorners[i], Color.yellow);      
        }

        Plane[] selectionVolumePlanes = new Plane[5];

        selectionVolumePlanes[0] = new Plane(selFrustumCorners[2], selFrustumCorners[1], selFrustumCorners[0]);
        selectionVolumePlanes[1] = new Plane(camera.transform.position, selFrustumCorners[0], selFrustumCorners[1]);
        selectionVolumePlanes[2] = new Plane(camera.transform.position, selFrustumCorners[1], selFrustumCorners[2]);
        selectionVolumePlanes[3] = new Plane(camera.transform.position, selFrustumCorners[2], selFrustumCorners[3]);
        selectionVolumePlanes[4] = new Plane(camera.transform.position, selFrustumCorners[3], selFrustumCorners[0]);

        List<GameObject> objectsWithinBounds = new List<GameObject>();

        foreach (Selectable so in selObjects) {
            if(GeometryUtility.TestPlanesAABB(selectionVolumePlanes, so.gameObject.GetComponent<Collider>().bounds)) {
                objectsWithinBounds.Add(so.gameObject);
            }
        }

        return objectsWithinBounds;
        // return viewportBounds.Contains(Camera.main.WorldToViewportPoint(gameObject.transform.position));
    }

    public void CleanupSelection(GameObject target) {
        RemoveSelection(target);
        foreach(HashSet<GameObject> group in controlGroups) {
            group.Remove(target);
        }
    }

    public void RemoveSelection(GameObject target, bool removeFromList = true) {
        bool selectedIndex = this.selectedUnits.Contains(target);
        if (selectedIndex) {
            //target.GetComponent<Outline>().OutlineWidth = 0.0f;
            Selectable selected = target.GetComponent<Selectable>();
            if (selected.selectionCircle.activeSelf) {
                selected.selectionCircle.SetActive(false);
            }
            if(removeFromList) {
                this.selectedUnits.Remove(target);
            }
        }
    }

    private void DeselectAll() {
        //Debug.Log("Deselect All");
        foreach (GameObject unit in this.selectedUnits) {
            RemoveSelection(unit, false);
        }

        // clear selected array
        this.selectedUnits.Clear();
    }

    private void AddSelection(GameObject target) {
        this.selectedUnits.Add(target);
        //target.GetComponent<Outline>().OutlineWidth = 6.0f;
        Selectable selected = target.GetComponent<Selectable>();
        selected.selectionCircle.SetActive(true);

        // add to selected list
        this.selectedUnits.Add(target);
    }

    private void Move() {
        if(attacking) {
            attacking = false;
        } else {
            RaycastHit hitInfo = new RaycastHit();
                
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, movementLayerMask, QueryTriggerInteraction.Ignore)) {
            //if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))) {
                GameObject obj = hitInfo.transform.gameObject;               

                if (obj.layer == LayerMask.NameToLayer("Ground")) {
                    foreach (GameObject unit in selectedUnits) {
                        unit.GetComponent<UnitController>().CmdMoveTo(hitInfo.point);
                    }
                }
            }
        }
    }

    private void ToggleAttackOn() {
        if(selectedUnits.Count > 0)
            attacking = true;
    }

    private void Attack() {

        RaycastHit hitInfo = new RaycastHit();
                
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, movementLayerMask, QueryTriggerInteraction.Ignore)) {
            //if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))) {
                GameObject obj = hitInfo.transform.gameObject;               

                if (obj.layer == LayerMask.NameToLayer("Ground")) {
                    foreach (GameObject unit in selectedUnits) {
                        unit.GetComponent<UnitController>().CmdAttack(hitInfo.point);
                    }
                }
            }

    }

    private void Stop() {
        foreach(GameObject unit in selectedUnits) {
            unit.GetComponent<UnitController>().CmdStop();
        }
    }

    private void SelectDown() {
        if(!attacking) {
            leftClickHeld = true;
            mousePositionInitial = Input.mousePosition;
        }
        
    }

    private void SelectUp() {
        if(attacking)
        {
            attacking = false;
            Stop();
            Attack();
        }
        // in place
        else if (Vector3.Distance(Input.mousePosition, mousePositionInitial) == 0.0f) {
            RaycastHit hitInfo = new RaycastHit();
            if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))) {
                GameObject obj = hitInfo.transform.gameObject;
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
                else
                {
                    this.DeselectAll();
                }
            }
        }
        // box selection 
        else {
            if (this.selectionMode == Mode.None) {
                this.DeselectAll();
            }
            foreach (GameObject obj in withinSelectionBounds(FindObjectsOfType<Selectable>())) {
                //exclude buildings from box-select
                if(obj.GetComponent<UnitController>() is BuildingController) {
                    continue;
                }

                if (this.selectionMode == Mode.Remove) {
                    RemoveSelection(obj);
                } else {
                    AddSelection(obj);
                }
            }
        }
        leftClickHeld = false;
    }

    private void ControlGroup() {
        for (int i = 0; i < controlGroupTypes.Count; ++i) {
            if(Input.GetKey(controlGroupTypes[i].currentKey.key)) {
                //assigning a control group
                if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                {
                    controlGroups[i].Clear();
                    controlGroups[i] = new HashSet<GameObject>(selectedUnits);
                }
                //select our control group
                else {
                    if(controlGroups[i].Count != 0) {
                        DeselectAll();
                        foreach(GameObject unit in controlGroups[i]) {
                            AddSelection(unit);
                        }
                    } 
                }
            }
        }
    }
}

