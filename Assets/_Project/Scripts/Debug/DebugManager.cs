using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugManager : MonoBehaviour
{
    [SerializeField] private GameObject debugUIPanel;

    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI spdText;

    [Header("設定")]
    [SerializeField] private string playerTag = "Player";
    private NetworkPlayer player;

    void Awake()
    {
        //開発環境以外なら、このデバッグUIを完全に消去する
        if (!Debug.isDebugBuild)
        {
            Destroy(debugUIPanel);
            return;
        }

        //開発環境であれば、ゲーム開始時は非表示に
        debugUIPanel.SetActive(false);
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag(playerTag);
        player = playerObj != null ? playerObj.GetComponent<NetworkPlayer>() : null;
    }

    void Update()
    {
        // 開発環境のみ
        if (Debug.isDebugBuild)
        {
            //Tabキーが今押されたか
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                debugUIPanel.SetActive(!debugUIPanel.activeSelf);
            }   
        }

        if (debugUIPanel.activeSelf)
        {
            UpdateStatus();
        }
    }

    void UpdateStatus()
    {
        if (player != null)
        {
            // プレイヤーの現在のHPと、ベースステータスを取得
            float currentHp = player.GetNowHP();
            PlayerStatus status = player.GetPlayerStatus();

            if (status != null)
            {
                //PlayerUtilityの静的メソッドを使って最終的な計算後の値を取得
                float finalAtk = PlayerUtility.GetFinalAtk(player);
                float finalDef = PlayerUtility.GetFinalDef(player);
                float finalSpd = PlayerUtility.GetFinalSpeed(player);

                //各テキストに反映（:F2 で小数点以下2桁に固定）
                hpText.text = $"HP: {currentHp} / {status.GetMaxHp()}";
                atkText.text = $"ATK: {finalAtk:F2}";
                defText.text = $"DEF: {finalDef:F2}";
                spdText.text = $"SPD: {finalSpd:F2}";
            }
        }
        else
        {
            //プレイヤーがシーンにいない場合のフォールバック
            hpText.text = "HP: Player Not Found";
            atkText.text = "ATK: --";
            defText.text = "DEF: --";
            spdText.text = "SPD: --";
        }
        //チェック用
        Debug.Log($"【デバッグ】現在の保持エフェクト数: {player.GetHaveEffect().Count}個");
    }

    public void HPDamage()
    {
        if (player != null)
        {
            //プレイヤーにダメージを与える
            player.TakeDamage(10);
        }
    }
}
