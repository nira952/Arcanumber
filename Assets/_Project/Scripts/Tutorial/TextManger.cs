using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextManager : MonoBehaviour
{
    // テキストとコマンドを1セットで保持するデータ構造
    [System.Serializable]
    public class DialogueData
    {
        public string text;
        public string command;
    }

    [Header("UI参照")]
    [SerializeField] private GameObject textPanel;
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private Button nextButton;

    [SerializeField] private TextAsset csvFile;

    [Header("読み込む縦列のインデックス (0始まり)")]
    [SerializeField] private int textColumnIndex = 1;    // 2列目: テキスト
    [SerializeField] private int commandColumnIndex = 2; // 3列目: コマンド名

    [Header("文字送り設定")]
    [SerializeField] private float typeSpeed = 0.05f;

    private List<DialogueData> dialogueList = new List<DialogueData>();
    private int currentIndex = 0;
    private Coroutine typingCoroutine;

    private void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnClickNextButton);
            nextButton.gameObject.SetActive(false);
        }

        LoadCsvColumnData();
        ShowCurrentText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnClickNextButton();
        }
    }
    /// <summary>
    /// CSVからテキストとコマンドの両方を読み取る
    /// </summary>
    private void LoadCsvColumnData()
    {
        if (csvFile == null)
        {
            Debug.LogError("CSVファイルがセットされていません。");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');

            // テキスト列が存在するか確認
            if (textColumnIndex < values.Length)
            {
                DialogueData data = new DialogueData();
                data.text = values[textColumnIndex].Trim(' ', '"').Replace("\\n", "\n");

                // 4列目にコマンドが記載されていれば取得
                if (commandColumnIndex < values.Length)
                {
                    data.command = values[commandColumnIndex].Trim(' ', '"');
                }

                dialogueList.Add(data);
            }
        }
    }

    /// <summary>
    /// 現在のテキストとコマンドの処理を実行する
    /// </summary>
    private void ShowCurrentText()
    {
        if (dialogueList.Count == 0)
        {
            textDisplay.text = "表示できるテキストがありません。";
            return;
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // --- コマンド（メソッド）の実行 ---
        DialogueData currentData = dialogueList[currentIndex];
        ExecuteCommand(currentData.command);

        // 文字送りスタート
        typingCoroutine = StartCoroutine(TypeTextCoroutine(currentData.text));
    }

    /// <summary>
    /// CSVに記載されたコマンド名に応じてメソッドを呼び出す
    /// </summary>
    private void ExecuteCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) return;

        switch (command)
        {
            case "Move":
                MoveStart();
                break;

            case "End":
                EndTutorial();
                break;

            default:
                Debug.LogWarning($"未定義のコマンドです: {command}");
                break;
        }
    }

    // ================================================
    //  呼び出したいメソッド（自由に追加・記述してください）
    // ================================================

    private void MoveStart()
    {
        
    }

    private void EndTutorial()
    {

    }

    // ================================================

    private IEnumerator TypeTextCoroutine(string targetText)
    {
        textDisplay.text = "";

        foreach (char c in targetText.ToCharArray())
        {
            textDisplay.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
        }
    }

    private void OnClickNextButton()
    {
        if (dialogueList.Count == 0) return;

        currentIndex++;

        if (currentIndex >= dialogueList.Count)
        {
            currentIndex = 0;
        }

        ShowCurrentText();
    }
}