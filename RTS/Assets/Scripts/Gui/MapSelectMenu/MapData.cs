using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapData : MonoBehaviour {
    public Sprite mapIcon;
    private int lastNumberSupportedPlayers;

    [Serializable]
    public struct MapInfo {
        public int numberSupportedPlayers;
        [ReadOnly]
        public Vector3[] flagPositions;
        [ReadOnly]
        public Vector3[] unitSpawnPositions;
        public bool canBuildSoldiers;
        public bool canBuildIronfoe;
        public bool canBuildDog;
        public bool canBuildMortars;
        public GameMode mapGameMode;
        public Vector3 cameraPosition;
        public Vector3 cameraAngle;
        public string size;
    }

    public MapInfo mapInfo;

    public enum GameMode {
        CaptureTheFlag,
        DestroyTheBuilding
    }

    public void Start () {
        Transform gameAnchors = transform.Find ("GameAnchors");
        for (int i = 1; i <= mapInfo.numberSupportedPlayers; ++i) {
            mapInfo.flagPositions[i - 1] = gameAnchors.Find ("Player" + i + "FlagSpot").position;
            if (mapInfo.flagPositions[i - 1] == Vector3.zero) {
                Debug.Log ("Could not find flag spot for player: " + i);
            }

            mapInfo.unitSpawnPositions[i - 1] = gameAnchors.Find ("Player" + i + "UnitSpawn").position;
            if (mapInfo.unitSpawnPositions[i - 1] == Vector3.zero) {
                Debug.Log ("Could not find unit spawn position for player: " + i);
            }
        }
    }

    public MapData () {
        mapInfo.numberSupportedPlayers = 2;
        mapInfo.canBuildSoldiers = true;
        mapInfo.canBuildIronfoe = true;
        mapInfo.canBuildDog = true;
        mapInfo.canBuildMortars = true;
        mapInfo.mapGameMode = GameMode.CaptureTheFlag;
        mapInfo.flagPositions = new Vector3[mapInfo.numberSupportedPlayers];
        mapInfo.unitSpawnPositions = new Vector3[mapInfo.numberSupportedPlayers];
        mapInfo.size = "Medium";
    }

    public void OnValidate () {
        if (mapInfo.numberSupportedPlayers != lastNumberSupportedPlayers) {
            lastNumberSupportedPlayers = mapInfo.numberSupportedPlayers;
            mapInfo.flagPositions = new Vector3[mapInfo.numberSupportedPlayers];
            mapInfo.unitSpawnPositions = new Vector3[mapInfo.numberSupportedPlayers];
        }
    }

}