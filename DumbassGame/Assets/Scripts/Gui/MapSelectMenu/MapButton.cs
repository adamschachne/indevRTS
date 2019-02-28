using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapButton : Button {
    private MapData map;
    private Image mapPreview;
    private EventTrigger eventTrigger;

    public void init (MapData map, Image mapPreview) {
        this.map = map;
        this.mapPreview = mapPreview;

        Image i = GetComponent<Image> ();
        i.sprite = map.mapIcon;

        RectTransform rTrans = GetComponent<RectTransform> ();
        rTrans.sizeDelta = new Vector2 (i.sprite.rect.width / 4, i.sprite.rect.height / 4);

        this.transform.GetChild (1).GetComponent<Text> ().text = map.name;
        this.transform.GetChild (0).GetComponent<ShowMapPreview> ().init (map.mapIcon, mapPreview, this.transform);
        Voteable v = GetComponent<Voteable> ();
        v.decisionCallback.AddListener (StateManager.state.AssignStartMap);

    }

    public MapData GetMap () {
        return map;
    }

    private void ShowPreview () {
        mapPreview.sprite = map.mapIcon;
        mapPreview.rectTransform.sizeDelta = new Vector2 (map.mapIcon.rect.width, map.mapIcon.rect.height);
        mapPreview.gameObject.SetActive (true);
    }

    private void HidePreview () {
        mapPreview.gameObject.SetActive (false);
    }

}