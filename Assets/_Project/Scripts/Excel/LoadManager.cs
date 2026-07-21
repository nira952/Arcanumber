using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LoadManager : SingletonMonoBehaviour<LoadManager>
{
    //アルカナデータ
    private string arcanaFilePath = Application.dataPath + "/_Project/Excel/ArcanaData.xlsx";
    private int arcanaSheetNumber = 1;    //シートの番号
    private IWorkbook arcanaWorkbook; //Excelファイル
    private ISheet arcanaSheet;   //シート
    private IFormulaEvaluator arcanaEvaluator;    //計算機

    //スキルデータ
    private string skillFilePath = Application.dataPath + "/_Project/Excel/SkillData.xltm";
    private int skillSheetNumber = 0;    //シートの番号
    private IWorkbook skillWorkbook; //Excelファイル
    private ISheet skillSheet;   //シート
    private IFormulaEvaluator skillEvaluator;    //計算機

    //データ保管
    private List<Arcana> arcanaList = new List<Arcana>();
    private List<Skill> skillList = new List<Skill>();

    public void Initialize() 
    {
        ArcanaLoadExcel();
        SkillLoadExcel();
    }

    /// <summary>
    /// アルカナデータをロードする
    /// </summary>
    public void ArcanaLoadExcel()
    {
        //ファイルが存在しない場合中止
        if (!File.Exists(arcanaFilePath)) return;

        //ファイルを開くモード
        using FileStream fs = new FileStream(arcanaFilePath, FileMode.Open, FileAccess.Read);
        //ファイルを読み込んで使う
        arcanaWorkbook = new XSSFWorkbook(fs);
        //シートを選択する
        arcanaSheet = arcanaWorkbook.GetSheetAt(arcanaSheetNumber);
        //計算器を作成
        arcanaEvaluator = arcanaWorkbook.GetCreationHelper().CreateFormulaEvaluator();

        ArcanaSetDictionary();
    }

    void SkillLoadExcel()
    {
        //ファイルが存在しない場合中止
        if (!File.Exists(skillFilePath)) return;

        //ファイルを開くモード
        using FileStream fs = new FileStream(skillFilePath, FileMode.Open, FileAccess.Read);
        //ファイルを読み込んで使う
        skillWorkbook = new XSSFWorkbook(fs);
        //シートを選択する
        skillSheet = skillWorkbook.GetSheetAt(skillSheetNumber);
        //計算器を作成
        skillEvaluator = skillWorkbook.GetCreationHelper().CreateFormulaEvaluator();

        SkillSetDictionary();
    }

    /// <summary>
    /// 辞書登録
    /// </summary>
    void ArcanaSetDictionary()
    {
        //リストをクリア（リロード時の二重登録を防ぐ）
        arcanaList.Clear();

        for (int i = 1; i <= arcanaSheet.LastRowNum; i++)
        {
            IRow row = arcanaSheet.GetRow(i);
            if (row == null) continue;

            //ScriptableObjectのインスタンスを作成
            var data = ScriptableObject.CreateInstance<Arcana>();

            //Excelの行データから値を流し込む
            data.LoadFromExcel(row, arcanaEvaluator);
            //リストに入れる
            arcanaList.Add(data);
            
        }

        //並び変える
        arcanaList = arcanaList
            .OrderBy(e => e.GetArcanaListID())
            .ThenByDescending(e => e.GetIsFront())
            .ToList();

        //全体に入れる
        AssetLoader.Instance.SetArcanaList(arcanaList);
    }

    void SkillSetDictionary()
    {
        skillList.Clear();

        for (int i = 1; i <= skillSheet.LastRowNum; i++)
        {
            IRow row = skillSheet.GetRow(i);
            if (row == null) continue;

            string rawNo = GetCellValueCalculated(row.GetCell(0), skillEvaluator);
            Debug.Log($"{i}行目のスキル番号: [{rawNo}]");

            //ScriptableObjectのインスタンスを作成
            var data = ScriptableObject.CreateInstance<Skill>();

            //Excelの行データから値を流し込む
            data.LoadFromExcel(row, skillEvaluator);
            //リストに入れる
            skillList.Add(data);

        }

        //並び変える
        skillList = skillList
            .OrderBy(e => e.GetSkillNo())
            .ToList();

        //全体に入れる
        AssetLoader.Instance.SetSkillList(skillList);
    }

    /// <summary>
    /// IDと位置を指定してデータを取得
    /// </summary>
    public Arcana GetData(int id, bool pos)
    {
        //LinqのFirstOrDefaultを使って検索
        return arcanaList.FirstOrDefault(a => a.GetArcanaListID() == id && a.GetIsFront() == pos);
    }

    /// <summary>
    /// セルの種類を判定して、数式なら結果を、それ以外なら値を返すメソッド
    /// </summary>
    public string GetCellValueCalculated(ICell cell, IFormulaEvaluator evaluator)
    {
        if (cell == null) return "";

        // 数式なら計算結果を取得
        if (cell.CellType == CellType.Formula)
        {
            // 渡された正しい計算機を使う
            CellValue value = evaluator.Evaluate(cell);
            return value.CellType switch
            {
                CellType.Numeric => value.NumberValue.ToString(),
                CellType.Boolean => value.BooleanValue.ToString(),
                _ => value.StringValue
            };
        }
        return cell.ToString();
    }

    /// <summary>
    /// 文字列を安全に指定した型に変換するメソッド
    /// </summary>
    public T ParseValue<T>(string value, T defaultValue = default)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        try
        {
            if (typeof(T).IsEnum) return (T)System.Enum.Parse(typeof(T), value);
            return (T)System.Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public List<Arcana> GetArcanas => arcanaList;
}