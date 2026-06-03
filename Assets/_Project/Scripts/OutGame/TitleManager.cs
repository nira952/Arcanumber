using System;
using R3;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private PlayerSettingData playerSettingData;

    [SerializeField] private TitleUIManager titleUIManager;

    private IDisposable settingNameSubscription;

    private IDisposable nameResetSubscription;

    private void Awake()
    {
        // PlayerSettingDataがアサインされていない場合は新しいインスタンスを作成する
        if (playerSettingData == null)
        {
            playerSettingData = ScriptableObject.CreateInstance<PlayerSettingData>();
        }

        // データをロードし、必要に応じて初期化する
        playerSettingData.Load();
        playerSettingData.InitializeIfNeeded();


        // プレイヤーの名前が設定されていない場合、名前入力パネルを表示する
        if (string.IsNullOrWhiteSpace(playerSettingData.playerInfo.Name.ToString()))
        {
            titleUIManager.OpenNameInputPanel();
        }

        if (titleUIManager != null)
        {
            // ボタンがクリックされたときのイベントを購読する
            settingNameSubscription
                = titleUIManager.OnSettingNameRequested.Subscribe(_ => HandleSettingName());
            nameResetSubscription 
                = titleUIManager.OnNameResetRequested.Subscribe(_ => HandleNameReset());
        }
    }

    private void OnDestroy()
    {
        settingNameSubscription?.Dispose();
        nameResetSubscription?.Dispose();
    }

    private void OnApplicationQuit()
    {
        playerSettingData.Save();
    }

    /// <summary>
    /// プレイヤーの名前を設定する
    /// </summary>
    private void HandleSettingName()
    {
        if (titleUIManager == null)
        {
            return;
        }

        string inputName = titleUIManager.NameInputText;
        Debug.Log($"入力された名前: {inputName}");

        playerSettingData.playerInfo.Name = inputName;
        Debug.Log($"プレイヤーの名前を設定しました: {playerSettingData.playerInfo.Name}");

        playerSettingData.Save();

        PlayerDataManager.Instance.SetPlayerData(playerSettingData);

        titleUIManager.CloseNameInputPanel();
    }

    /// <summary>
    /// プレイヤーの名前をリセットする
    /// </summary>
    private void HandleNameReset()
    {
        playerSettingData.playerInfo.Name = string.Empty;
        playerSettingData.Save();
        titleUIManager.OpenNameInputPanel();
    }
}
