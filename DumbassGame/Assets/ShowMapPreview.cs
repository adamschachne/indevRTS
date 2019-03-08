using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowMapPreview : MonoBehaviour {

    Sprite mapIcon;
    Image mapPreview;

    public void init (Sprite mapIcon, Image mapPreview, Transform parent) {
        this.mapIcon = mapIcon;
        this.mapPreview = mapPreview;
        this.transform.SetParent (parent.parent);
    }

    public void OnPointerEnter () {
        mapPreview.sprite = mapIcon;
        mapPreview.rectTransform.sizeDelta = new Vector2 (mapIcon.rect.width, mapIcon.rect.height);
        mapPreview.gameObject.SetActive (true);
    }

    public void OnPointerExit () {
        mapPreview.gameObject.SetActive (false);
    }
}