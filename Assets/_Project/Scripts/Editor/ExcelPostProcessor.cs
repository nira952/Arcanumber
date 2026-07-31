using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

public class ExcelPostProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    //ビルドが始まる直前に自動で呼ばれる
    public void OnPreprocessBuild(BuildReport report)
    {
        string sourceDir = Path.Combine(Application.dataPath, "_Project/Excel");
        string destDir = Path.Combine(Application.streamingAssetsPath, "Excel");

        if (!Directory.Exists(sourceDir)) return;

        //StreamingAssets/Excelフォルダがなければ作る
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        //_Project/Excel内のファイルをStreamingAssets/Excelへコピー
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            // .meta ファイルなどは除外
            if (Path.GetExtension(file) == ".meta") continue;

            string destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("【Excel自動コピー】ビルド用にExcelファイルをStreamingAssetsへ同期しました。");
    }
}