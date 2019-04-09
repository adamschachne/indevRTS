using System.Collections;
using System.Collections.Generic;
using InputActions;
using UnityEngine;

public class Selection : MonoBehaviour {

    public GameObject SelectionCircle;
    HashSet<GameObject> selectedUnits;
    HashSet<GameObject>[] controlGroups;
    List<ActionType> controlGroupTypes;
    bool leftClickHeld;
    enum Mode {None,Remove,Add};
    Mode selectionMode;
    Vector3 mousePositionInitial;
    Vector3[] selFrustumCorners;
    public LayerMask movementLayerMask;
    const float MIN_LENGTH = 0.0005f;
    private bool[] filter;
    private StateManager state;
    Camera mainCamera;

        // Use this for initialization
    void Start () {
        state = StateManager.state;
        mousePositionInitial = new Vector3 (0.0f, 0.0f, 0.0f);
        movementLayerMask = 1 << LayerMask.NameToLayer ("Ground");
        state.input.Subscribe (SelectDown, RTS.SELECT_DOWN);
        state.input.Subscribe (SelectUp, RTS.SELECT_UP);
        state.input.Subscribe (Move, RTS.MOVE);
        state.input.Subscribe (Stop, RTS.STOP);
        state.input.Subscribe (TeaseAttack, RTS.TEASE_ATTACK);
        state.input.Subscribe (Attack, RTS.ATTACK);
        state.input.Subscribe (ControlGroup, RTS.CONTROL_GROUP_1);
        filter = new bool[4];

        controlGroupTypes = state.input.getTaggedActions ("Control Group", typeof (RTS));

        controlGroups = new HashSet<GameObject>[controlGroupTypes.Count];
        for (int i = 0; i < controlGroups.Length; ++i) {
            controlGroups[i] = new HashSet<GameObject> ();
            state.input.Subscribe (ControlGroup, controlGroupTypes[i]);
            mainCamera = Camera.main;
        }
    }

    private void OnEnable () {
        leftClickHeld = false;
        selectionMode = Mode.None;
        selectedUnits = new HashSet<GameObject> ();
    }

    private void OnDisable () {
        leftClickHeld = false;
        selectionMode = Mode.None;
        selectedUnits.Clear ();
    }

    // Update is called once per frame
    void Update () {
        // Left Shift
        if (Input.GetKey (KeyCode.LeftShift)) {
            selectionMode = Mode.Add;
        } else if (Input.GetKey (KeyCode.LeftCommand) || Input.GetKey (KeyCode.LeftControl)) {
            selectionMode = Mode.Remove;
        } else {
            selectionMode = Mode.None;
        }

        if(leftClickHeld) {
            filter[(int)StateManager.EntityType.Soldier] = Input.GetKey(InputActions.RTS.SPAWN_SHOOTGUY.currentKey.key);
            filter[(int)StateManager.EntityType.Ironfoe] = Input.GetKey(InputActions.RTS.SPAWN_IRONFOE.currentKey.key);
            filter[(int)StateManager.EntityType.Dog] = Input.GetKey(InputActions.RTS.SPAWN_DOG.currentKey.key);
            filter[(int)StateManager.EntityType.Mortar] = Input.GetKey(InputActions.RTS.SPAWN_MORTAR.currentKey.key);
        }

        /* DEBUG RAY */
        //if (selFrustumCorners == null) {
        //    return;
        //}
        //foreach (Vector3 ray in selFrustumCorners) {
        //    Debug.DrawRay(mainCamera.transform.position, ray, Color.yellow);
        //}
        /* END DEBUG RAY */
    }

    void OnGUI () {
        if (leftClickHeld) {
            // Create a rect from both mouse positions
            Rect rect = Utils.GetScreenRect (mousePositionInitial, Input.mousePosition);
            Utils.DrawScreenRect (rect, new Color (0.8f, 0.8f, 0.95f, 0.25f));
            Utils.DrawScreenRectBorder (rect, 2, new Color (0.8f, 0.8f, 0.95f));
            //Debug.Log(mousePositionInitial + ", " + Input.mousePosition);
            
            //Vector2 centerOfRect = new Vector2(((mousePositionInitial.x + Input.mousePosition.x)/2) - Screen.width/2, ((mousePositionInitial.y + Input.mousePosition.y)/2) - Screen.height/2);
            //Vector2 centerOfRect = mainCamera.ScreenToViewportPoint(centerOfRect);
            //Vector2 centerOfRect = new Vector2(mousePositionInitial.x - Screen.width/2, mousePositionInitial.y - Screen.height/2);

            state.gui.SelectionFilter(filter);

            /* DEBUG RAY */
            //float x = rect.x / Screen.width;
            //float y = 1.0f - rect.y / Screen.height;
            //float xMax = rect.xMax / Screen.width;
            //float yMax = 1.0f - rect.yMax / Screen.height;

            //Rect selRect = new Rect(x, y, Mathf.Abs(xMax - x), yMax - y);
            //Camera camera = mainCamera;
            //Vector3[] selFrustumCorners = new Vector3[4];
            //selFrustumCorners = new Vector3[4];
            //camera.CalculateFrustumCorners(selRect, camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, selFrustumCorners);
            //for (int i = 0; i < 4; i++) {
            //    // transform to world space
            //    selFrustumCorners[i] = camera.transform.TransformVector(selFrustumCorners[i]);
            //    //Debug.DrawRay(camera.transform.position, selFrustumCorners[i], Color.yellow);      
            //}
            /* END DEBUG RAY */
        }
        else {
            state.gui.DisableSelectionFilter();
        }
    }

    private List<GameObject> WithinSelectionBounds (Selectable[] selObjects) {
        var rect = Utils.GetScreenRect (mousePositionInitial, Input.mousePosition);
        //Bounds viewportBounds = Utils.GetViewportBounds(mainCamera, mousePositionInitial, Input.mousePosition);

        float x = rect.x / Screen.width;
        float y = 1.0f - rect.y / Screen.height;
        float xMax = rect.xMax / Screen.width;
        float yMax = 1.0f - rect.yMax / Screen.height;

        float width = Mathf.Abs (xMax - x);
        float height = yMax - y;
        width = width == 0 ? MIN_LENGTH : width;
        height = height == 0 ? MIN_LENGTH : height;
        Rect selRect = new Rect (x, y, width, height);
        Camera camera = mainCamera;

        selFrustumCorners = new Vector3[4];
        camera.CalculateFrustumCorners (selRect, camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, selFrustumCorners);
        for (int i = 0; i < 4; i++) {
            // transform to world space
            selFrustumCorners[i] = camera.transform.TransformVector (selFrustumCorners[i]);
            //Debug.DrawRay(camera.transform.position, selFrustumCorners[i], Color.yellow);      
        }

        Plane[] selectionVolumePlanes = new Plane[5];

        selectionVolumePlanes[0] = new Plane (selFrustumCorners[2], selFrustumCorners[1], selFrustumCorners[0]);
        selectionVolumePlanes[1] = new Plane (camera.transform.position, selFrustumCorners[0], selFrustumCorners[1]);
        selectionVolumePlanes[2] = new Plane (camera.transform.position, selFrustumCorners[1], selFrustumCorners[2]);
        selectionVolumePlanes[3] = new Plane (camera.transform.position, selFrustumCorners[2], selFrustumCorners[3]);
        selectionVolumePlanes[4] = new Plane (camera.transform.position, selFrustumCorners[3], selFrustumCorners[0]);

        List<GameObject> objectsWithinBounds = new List<GameObject> ();

        foreach (Selectable so in selObjects) {
            if (GeometryUtility.TestPlanesAABB (selectionVolumePlanes, so.gameObject.GetComponent<Collider> ().bounds)) {
                objectsWithinBounds.Add (so.gameObject);
            }
        }

        return objectsWithinBounds;
        // return viewportBounds.Contains(mainCamera.WorldToViewportPoint(gameObject.transform.position));
    }

    public void CleanupSelection (GameObject target) {
        RemoveSelection (target);
        foreach (HashSet<GameObject> group in controlGroups) {
            group.Remove (target);
        }
    }

    private void RemoveSelection (GameObject target, bool removeFromList = true) {
        bool selectedIndex = this.selectedUnits.Contains (target);
        if (selectedIndex) {
            //target.GetComponent<Outline>().OutlineWidth = 0.0f;
            Selectable selected = target.GetComponent<Selectable> ();
            if (selected.selectionCircle.activeSelf) {
                selected.selectionCircle.SetActive (false);
            }
            if (removeFromList) {
                this.selectedUnits.Remove (target);
            }
        }
    }

    private void DeselectAll () {
        //Debug.Log("Deselect All");
        foreach (GameObject unit in this.selectedUnits) {
            RemoveSelection (unit, false);
        }

        // clear selected array
        this.selectedUnits.Clear ();
    }

    private void AddSelection (GameObject target) {
        this.selectedUnits.Add (target);
        //target.GetComponent<Outline>().OutlineWidth = 6.0f;
        Selectable selected = target.GetComponent<Selectable> ();
        selected.selectionCircle.SetActive (true);

        // add to selected list
        this.selectedUnits.Add (target);
    }

    private void Move () {
        RaycastHit hitInfo = new RaycastHit ();

        if (Physics.Raycast (mainCamera.ScreenPointToRay (Input.mousePosition), out hitInfo, Mathf.Infinity, movementLayerMask, QueryTriggerInteraction.Ignore)) {
            //if ((Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hitInfo))) {
            GameObject obj = hitInfo.transform.gameObject;

            if (obj.layer == LayerMask.NameToLayer ("Ground")) {
                string[] names = new string[selectedUnits.Count];
                int i = 0;
                foreach (GameObject g in selectedUnits) {
                    names[i++] = g.name;
                }

                StateManager.state.BlobMove (names, state.network.networkID, hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);
                StateManager.state.network.SendMessage (new MoveMany {
                    ids = names,
                        ownerID = state.network.networkID,
                        x = hitInfo.point.x,
                        y = hitInfo.point.y,
                        z = hitInfo.point.z
                });
            }
        }
    }

    private void TeaseAttack() {
        RaycastHit hitInfo = new RaycastHit ();
        if (Physics.Raycast (mainCamera.ScreenPointToRay (Input.mousePosition), out hitInfo, Mathf.Infinity, movementLayerMask, QueryTriggerInteraction.Ignore)) {
            GameObject obj = hitInfo.transform.gameObject;

            if (obj.layer == LayerMask.NameToLayer ("Ground")) {
                Debug.Log("teasing an attack");
                bool relative = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                string[] ids = new string[selectedUnits.Count];
                int found =0;
                foreach (GameObject unit in selectedUnits) {
                    ids[found++] = unit.name;
                    if(!relative) {
                        unit.GetComponent<UnitController> ().ShowTeaserAttack (hitInfo.point.x, hitInfo.point.z, true);
                    } 
                }

                if(relative) {
                    UnitController[] units = state.GetUnits(ids, state.network.networkID);
                    Vector2[] relativePositions = state.GetRelativePoints(units, hitInfo.point.x, hitInfo.point.z);
                    for(int i = 0; i < units.Length; i++) {
                        units[i].ShowTeaserAttack(relativePositions[i].x, relativePositions[i].y, true);
                    }
                }
            }
        }
    }

    private void Attack () {
        RaycastHit hitInfo = new RaycastHit ();
        if (Physics.Raycast (mainCamera.ScreenPointToRay (Input.mousePosition), out hitInfo, Mathf.Infinity, movementLayerMask, QueryTriggerInteraction.Ignore)) {
            GameObject obj = hitInfo.transform.gameObject;

            if (obj.layer == LayerMask.NameToLayer ("Ground")) {
                Stop();
                bool relative = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

                string[] ids = new string[selectedUnits.Count];
                int found =0;
                foreach (GameObject unit in selectedUnits) {
                    ids[found++] = unit.name;
                    if(!relative) {
                        UnitController uc = unit.GetComponent<UnitController>();
                        uc.ShowTeaserAttack(0,0,false);
                        uc.Attack (hitInfo.point.x, hitInfo.point.z);
                    }
                }

                if(relative) {
                    StateManager.state.RelativeAttack(ids, state.network.networkID, hitInfo.point.x, hitInfo.point.z);
                }

                state.network.SendMessage (new AttackMany {
                    ids = ids,
                    ownerID = state.network.networkID,
                    x = hitInfo.point.x,
                    z = hitInfo.point.z,
                    relative = relative
                });
            }
        }

    }

    private void Stop () {
        foreach (GameObject unit in selectedUnits) {
            unit.GetComponent<UnitController> ().CmdMoveTo (new Vector3 (unit.transform.position.x, unit.transform.position.y, unit.transform.position.z));
        }
    }

    private void SelectDown () {
        leftClickHeld = true;
        mousePositionInitial = Input.mousePosition;
        for(int i = 0; i < filter.Length; ++i) {
            filter[i] = false;
        }
    }

    private void SelectUp () {
        // in place
        if (Vector3.Distance (Input.mousePosition, mousePositionInitial) == 0.0f) {
            RaycastHit hitInfo = new RaycastHit ();
            if ((Physics.Raycast (mainCamera.ScreenPointToRay (Input.mousePosition), out hitInfo))) {
                GameObject obj = hitInfo.transform.gameObject;
                if (obj.GetComponent<Selectable> () != null) {
                    if (this.selectionMode == Mode.Remove) {
                        RemoveSelection (obj);
                    } else if (this.selectionMode == Mode.Add) {
                        AddSelection (obj);
                    } else if (this.selectionMode == Mode.None) {
                        DeselectAll ();
                        AddSelection (obj);
                    }
                } else {
                    this.DeselectAll ();
                }
            }
        }
        // box selection 
        else {
            if (this.selectionMode == Mode.None) {
                this.DeselectAll ();
            }

            //if any keys are being held down, we filter to only select those units
            bool useFilter = false;
            for(int i = 0; i < filter.Length; ++i) {
                if(filter[i]) {
                    useFilter = true;
                }
            }

            foreach (GameObject obj in WithinSelectionBounds (FindObjectsOfType<Selectable> ())) {
                UnitController unitType = obj.GetComponent<UnitController>();
                //exclude buildings from box-select
                if (unitType is BuildingController) {
                    continue;
                }

                if(!useFilter || (useFilter && filter[(int)unitType.type])) {
                    if (this.selectionMode == Mode.Remove) {
                        RemoveSelection (obj);
                    } else {
                        AddSelection (obj);
                    }
                }
            }
        }
        leftClickHeld = false;
    }

    private void ControlGroup () {
        for (int i = 0; i < controlGroupTypes.Count; ++i) {
            if (Input.GetKey (controlGroupTypes[i].currentKey.key)) {
                //assigning a control group
                if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift) ||
                    Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl) ||
                    Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) {
                    controlGroups[i].Clear ();
                    controlGroups[i] = new HashSet<GameObject> (selectedUnits);
                }
                //select our control group
                else {
                    if (controlGroups[i].Count != 0) {
                        DeselectAll ();
                        foreach (GameObject unit in controlGroups[i]) {
                            AddSelection (unit);
                        }
                    }
                }
            }
        }
    }
}