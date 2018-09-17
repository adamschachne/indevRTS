using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputActions;

public class Selection : MonoBehaviour {

    public GameObject SelectionCircle;
    HashSet<GameObject> selectedUnits;
    bool leftClickHeld;
    bool attacking;
    enum Mode { None, Remove, Add };
    Mode selectionMode;
    Vector3 mousePositionInitial;
    Vector3[] selFrustumCorners;
    public LayerMask movementLayerMask;

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
        attacking = false;
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

        /* DEBUG */
        //if (selFrustumCorners == null) {
        //    return;
        //}
        //foreach (Vector3 ray in selFrustumCorners) {
            
        //    Debug.DrawRay(Camera.main.transform.position, ray, Color.yellow);
        //}        
    }
    
    void OnGUI() {
        if (leftClickHeld) {
            // Create a rect from both mouse positions
            var rect = Utils.GetScreenRect(mousePositionInitial, Input.mousePosition);
            Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Utils.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }
    }

    public bool IsWithinSelectionBoundsVolume(GameObject gameObject) {
        if (!leftClickHeld)
            return false;

        var rect = Utils.GetScreenRect(mousePositionInitial, Input.mousePosition);
        //Bounds viewportBounds = Utils.GetViewportBounds(Camera.main, mousePositionInitial, Input.mousePosition);

        //Debug.Log("rect: " + rect.y + " " + rect.yMax);
        float x = rect.x / Screen.width;
        float y = 1.0f - rect.y / Screen.height;
        float xMax = rect.xMax / Screen.width;
        float yMax = 1.0f - rect.yMax / Screen.height;

        Rect selRect = new Rect(x, y, Mathf.Abs(xMax - x), yMax - y);
        Camera camera = Camera.main;
        //Vector3[] selFrustumCorners = new Vector3[4];
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

        return GeometryUtility.TestPlanesAABB(selectionVolumePlanes, gameObject.GetComponent<Collider>().bounds);
        // return viewportBounds.Contains(Camera.main.WorldToViewportPoint(gameObject.transform.position));
    }

    // Tries several points for the Collider -- only capsule for now TODO
    public bool IsWithinSelectionBoundsPoints(GameObject gameObject) {
        if (!leftClickHeld)
            return false;

        CapsuleCollider collider = gameObject.GetComponent<CapsuleCollider>();
        Vector3 colliderCenter = collider.center + gameObject.transform.position;
        float radius = collider.radius;
        float height = collider.height;

        
        Vector3 colliderTop = colliderCenter + new Vector3(0.0f, height / 2, 0.0f);
        Vector3 colliderBot = colliderCenter - new Vector3(0.0f, height / 2, 0.0f);
        Vector3 colliderRight = colliderCenter + new Vector3(radius, 0.0f, 0.0f);
        Vector3 colliderLeft = colliderCenter - new Vector3(radius, 0.0f, 0.0f);
        Vector3 colliderFront = colliderCenter + new Vector3(0.0f, 0.0f, radius);
        Vector3 colliderBack = colliderCenter - new Vector3(0.0f, 0.0f, radius);
        

        Bounds viewportBounds = Utils.GetViewportBounds(Camera.main, mousePositionInitial, Input.mousePosition);

        return 
            viewportBounds.Contains(Camera.main.WorldToViewportPoint(colliderCenter)) ||
            viewportBounds.Contains(Camera.main.WorldToViewportPoint(colliderTop)) ||
            viewportBounds.Contains(Camera.main.WorldToViewportPoint(colliderBot)) ||
            viewportBounds.Contains(Camera.main.WorldToViewportPoint(colliderRight)) ||
            viewportBounds.Contains(Camera.main.WorldToViewportPoint(colliderLeft)) ||
            viewportBounds.Contains(Camera.main.WorldToViewportPoint(colliderFront)) ||
            viewportBounds.Contains(Camera.main.WorldToViewportPoint(colliderBack));
    }

    public void RemoveSelection(GameObject target) {
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
            if(unit == null)
                continue;

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

    private void SelectDown() {
        if(!attacking) {
            leftClickHeld = true;
            mousePositionInitial = Input.mousePosition;
        }
        
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

    private void Move(int ownerId, HashSet<int> units) {
        RaycastHit hitInfo = new RaycastHit();

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, movementLayerMask, QueryTriggerInteraction.Ignore)) {
            GameObject obj = hitInfo.transform.gameObject;

            if (obj.layer == LayerMask.NameToLayer("Ground")) {
                foreach (GameObject unit in selectedUnits) {
                    unit.GetComponent<UnitController>().CmdMoveTo(hitInfo.point);
                }
            }
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
            foreach (Selectable selectableObject in FindObjectsOfType<Selectable>()) {

                GameObject obj = selectableObject.gameObject;
                if (this.IsWithinSelectionBoundsVolume(obj)) {
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

