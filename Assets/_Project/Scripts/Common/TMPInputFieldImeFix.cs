using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_InputField))]
public class TMPInputFieldImeFix : MonoBehaviour
{
    private TMP_InputField _inputField;

    // スクリプトから自動取得
    private TextMeshProUGUI texts;

    void Start()
    {
        _inputField = GetComponent<TMP_InputField>();

        // 1. "Text" という名前の子オブジェクトを名前で検索
        Transform textObject = FindChildTransformByName(transform, "Text");

        if (textObject != null)
        {
            // 2. 見つかったオブジェクトから TextMeshProUGUI を取得
            texts = textObject.GetComponent<TextMeshProUGUI>();
        }

        // 見つからなかった場合
        if (texts == null)
        {
            Debug.LogError($"{gameObject.name} の配下に '[Text]' という名前の TextMeshProUGUI オブジェクトが見つかりません！階層やオブジェクト名を確認してください。", this);
            return;
        }

        // 編集終了時のイベントを追加
        _inputField.onEndEdit.AddListener(OnEndEdit);
    }

    /// <summary>
    /// 子や孫の階層から指定された名前のTransformを再帰的に検索するヘルパーメソッド
    /// </summary>
    private Transform FindChildTransformByName(Transform current, string targetName)
    {
        if (current.name == targetName) return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindChildTransformByName(current.GetChild(i), targetName);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// 編集終了時に呼ばれるコールバック
    /// </summary>
    /// <param name="text">入力されたテキスト</param>
    private void OnEndEdit(string text)
    {
        if (texts == null) return;

        // 実際にTextMeshProUGUIに表示されているテキストを取得
        string cleanText = texts.text;

        // --- <u> と </u> を除去 ---
        if (!string.IsNullOrEmpty(cleanText))
        {
            cleanText = cleanText.Replace("<u>", "").Replace("</u>", "");
        }

        // インプットフィールドに値を再設定して強制同期
        _inputField.text = cleanText;
        _inputField.ForceLabelUpdate();

        // カーソル位置を末尾に移動させる
        _inputField.caretPosition = _inputField.text.Length;

        // 次のフレームでテキストをリセットするコルーチンを開始
        StartCoroutine(ClearTexts());
    }

    private IEnumerator ClearTexts()
    {
        yield return null; // 次のフレームまで待機

        texts.text = "";  // InputFieldに渡したテキストをクリア

    }
}