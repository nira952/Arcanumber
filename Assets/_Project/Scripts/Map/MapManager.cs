using NaughtyAttributes;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MapManager : NetworkBehaviour
{
    [Label("マップリスト")][SerializeField] private List<GameObject> mapList;

    // NetworkVariable の宣言
    private readonly NetworkVariable<int> selectedMapIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (mapList.Count == 0)
        {
            Debug.LogError("マップリストが空です。");
        }
    }

    public override void OnNetworkSpawn()
    {
        // 値変更イベントの登録（全端末）
        selectedMapIndex.OnValueChanged += OnMapIndexChanged;

        if (IsServer)
        {
            // ★ サーバー側：すでにマップが決定済みの場合は適用、未設定(-1)なら抽選する
            if (selectedMapIndex.Value == -1)
            {
                SelectRandomMapServer();
            }
            else
            {
                ApplyMapSelection(selectedMapIndex.Value);
            }
        }
        else
        {
            // ★ クライアント側：すでに同期された値があれば即時反映
            if (selectedMapIndex.Value >= 0 && selectedMapIndex.Value < mapList.Count)
            {
                ApplyMapSelection(selectedMapIndex.Value);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        selectedMapIndex.OnValueChanged -= OnMapIndexChanged;
    }

    /// <summary>
    /// サーバー側でランダムにマップ番号を選び NetworkVariable にセットする
    /// </summary>
    private void SelectRandomMapServer()
    {
        if (mapList.Count == 0) return;

        int randomIndex = Random.Range(0, mapList.Count);

        // ★ NetworkVariable への代入（OnNetworkSpawn内で呼ぶことで安全に同期されます）
        selectedMapIndex.Value = randomIndex;

        // サーバー自身の画面も更新
        ApplyMapSelection(randomIndex);
    }

    /// <summary>
    /// NetworkVariable の値が変わった時に全端末で実行されるコールバック
    /// </summary>
    private void OnMapIndexChanged(int previousValue, int newValue)
    {
        ApplyMapSelection(newValue);
    }

    /// <summary>
    /// 指定されたインデックスのマップのみを表示する
    /// </summary>
    private void ApplyMapSelection(int index)
    {
        if (index < 0 || index >= mapList.Count) return;

        for (int i = 0; i < mapList.Count; i++)
        {
            mapList[i].SetActive(i == index);
        }

        Debug.Log($"[MapManager] マップ '{mapList[index].name}' (Index: {index}) が読み込まれました。");
    }
}