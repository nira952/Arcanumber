using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LoadManager : SingletonMonoBehaviour<LoadManager>
{
    //パスの場所
    private string filePath = Application.dataPath + "/_Project/Excel/ArcanaData.xlsx";
    private int sheetNumber = 1;    //シートの番号

    private IWorkbook workbook; //Excelファイル
    private ISheet sheet;   //シート
    private IFormulaEvaluator evaluator;    //計算機

    //データ保管
    private List<Arcana> arcanaList = new List<Arcana>();

    void Start()
    {
        LoadExcel();
    }

    void LoadExcel()
    {
        //ファイルが存在しない場合中止
        if (!File.Exists(filePath)) return;

        //ファイルを開くモード
        using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //ファイルを読み込んで使う
        workbook = new XSSFWorkbook(fs);
        //シートを選択する
        sheet = workbook.GetSheetAt(sheetNumber);
        //計算器を作成
        evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

        SetDictionary();
    }

    /// <summary>
    /// 辞書登録
    /// </summary>
    void SetDictionary()
    {
        //リストをクリア（リロード時の二重登録を防ぐ）
        arcanaList.Clear();

        for (int i = 1; i <= sheet.LastRowNum; i++)
        {
            IRow row = sheet.GetRow(i);
            if (row == null) continue;

            //ScriptableObjectのインスタンスを作成
            var data = ScriptableObject.CreateInstance<Arcana>();

            //Excelの行データから値を流し込む
            data.LoadFromExcel(row);
            //リストに入れる
            arcanaList.Add(data);
            
            Debug.Log($"登録完了: {data.GetArcanaName} (位置: {(data.GetIsFront() ? "正" : "逆")})");
        }

        //並び変える
        arcanaList = arcanaList
            .OrderBy(e => e.GetArcanaListID())
            .ThenByDescending(e => e.GetIsFront())
            .ToList();
    }

    /// <summary>
    /// IDと位置を指定してデータを取得
    /// </summary>
    public Arcana GetData(int id, bool pos)
    {
        // LinqのFirstOrDefaultを使って検索
        return arcanaList.FirstOrDefault(a => a.GetArcanaListID() == id && a.GetIsFront() == pos);
    }

    /// <summary>
    /// 全データを取得
    /// </summary>
    public List<Arcana> GetAllArcanas()
    {
        return arcanaList;
    }

    /// <summary>
    /// セルの中身をかえすメソッド
    /// </summary>
    public string GetCell(int rowIndex, int cellIndex)
    {
        //行の選択
        IRow row = sheet.GetRow(rowIndex);

        if (row == null) return "";
        //セルの検索
        ICell cell = row.GetCell(cellIndex);

        return GetCellValueCalculated(cell);
    }

    /// <summary>
    /// セルの種類を判定して、数式なら結果を、それ以外なら値を返すメソッド
    /// </summary>
    public string GetCellValueCalculated(ICell cell)
    {
        if (cell == null) return "";

        // 数式なら計算結果を取得
        if (cell.CellType == CellType.Formula)
        {
            CellValue value = evaluator.Evaluate(cell);
            // 値の型に合わせて変換
            return value.CellType switch
            {
                CellType.Numeric => value.NumberValue.ToString(),
                CellType.Boolean => value.BooleanValue.ToString(),
                _ => value.StringValue // 文字列など
            };
        }
        // 数式でなければ通常のToString
        return cell.ToString();
    }

    public List<Arcana> GetArcanas => arcanaList;
}