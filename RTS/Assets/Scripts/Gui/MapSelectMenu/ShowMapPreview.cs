using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowMapPreview : MonoBehaviour {

    Sprite mapIcon;
    Image mapPreview;
    MapData data;

    public void init (MapData data, Sprite mapIcon, Image mapPreview, Transform parent) {
        this.mapIcon = mapIcon;
        this.mapPreview = mapPreview;
        this.data = data;
        this.transform.SetParent (parent.parent);
    }

    public void OnPointerEnter () {
        mapPreview.sprite = mapIcon;
        mapPreview.rectTransform.sizeDelta = new Vector2 (mapIcon.rect.width, mapIcon.rect.height);
        Text t = mapPreview.GetComponentInChildren<Text>();
        if(t != null) {
            t.text = "MAP INFO\n\nMax Players\n" + data.mapInfo.numberSupportedPlayers + "\n\nSize\n" + data.mapInfo.size + "\n\nGame Mode\n" + data.mapInfo.mapGameMode.ToString();
        }
        mapPreview.gameObject.SetActive (true);
    }

    public void OnPointerExit () {
        mapPreview.gameObject.SetActive (false);
    }
}