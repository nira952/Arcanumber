using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Label("マップリスト")][SerializeField]private List<GameObject> mapList;
    
    /// <summary>
    /// マップ選択
    /// </summary>
    public void RamdomMapSelect()
    {
        for (int i = 0; i < mapList.Count; i++)
            mapList[i].SetActive(false);
        mapList[RandomMapNo].SetActive(true);
    }

    //ランダムな番号を返す
    int RandomMapNo => Random.Range(0, mapList.Count);
}
