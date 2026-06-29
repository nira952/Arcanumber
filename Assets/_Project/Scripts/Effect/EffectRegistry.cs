using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 生成したEffectアセットを一括管理するクラス
/// </summary>
[CreateAssetMenu(fileName = "EffectRegistry", menuName = "Manager/EffectRegistry")]
public class EffectRegistry : ScriptableObject
{
    [ReadOnly]
    [SerializeField] private List<Effect> allEffects = new List<Effect>();

    // 外部取り出し用の静的辞書
    private static Dictionary<(EffectList type, bool isUp), Effect> effectDict = new Dictionary<(EffectList, bool), Effect>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRegistry()
    {
        EffectRegistry registry = Resources.Load<EffectRegistry>("ScriptableObject/Registry/EffectRegistry");

        if (registry == null)
        {
            var registries = Resources.FindObjectsOfTypeAll<EffectRegistry>();
            if (registries.Length > 0) registry = registries[0];
        }

        if (registry != null)
        {
            registry.BuildDictionary();
        }
        else
        {
            Debug.LogError("【EffectRegistry】アセットが見つかりませんでした。アセット名を 'EffectRegistry' にして Resources フォルダに配置することをおすすめします。");
        }
    }

    /// <summary>
    /// 辞書を組み立てる処理
    /// </summary>
    public void BuildDictionary()
    {
        effectDict.Clear();
        foreach (var fx in allEffects)
        {
            if (fx == null) continue;

            var key = (fx.GetEffectList(), fx.GetIsUp());

            if (!effectDict.ContainsKey(key))
                effectDict.Add(key, fx);
            else
                Debug.LogWarning($"【重複注意】{key.Item1} (isUp: {key.Item2}) のEffectアセットが複数登録されようとしました。'{fx.name}' は無視されます。");
        }
        Debug.Log($"【EffectRegistry】自動初期化完了： {effectDict.Count} 個の効果を辞書に登録しました。");
    }

    private void OnValidate()
    {
        BuildDictionary();
    }
    public static Effect Get(EffectList type, bool isUp)
    {
        //辞書が空なら、ここで強制的に初期化を試みる（安全策）
        if (effectDict.Count == 0)
            InitializeRegistry();

        var key = (type, isUp);

        if (effectDict.TryGetValue(key, out var effect))
            return effect;

        string stateStr = isUp ? "バフ" : "デバフ";
        Debug.LogError($"EffectRegistry: {type} の【{stateStr}】が登録されていません。");
        return null;
    }

    // ========================================================
    // ボタンを押したらプロジェクト内から全自動で集める
    // ========================================================
    [Button("すべてのEffectアセットを自動収集する")]
    public void CollectAllEffects()
    {
#if UNITY_EDITOR
        allEffects.Clear();

        // プロジェクト内の「t:Effect」（Effect型のScriptableObject）のGUIDをすべて検索
        string[] guids = AssetDatabase.FindAssets("t:Effect");

        foreach (string guid in guids)
        {
            //GUIDから実際のアセットのパスを取得
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            //アセットをロードしてリストに追加
            Effect fx = AssetDatabase.LoadAssetAtPath<Effect>(assetPath);

            if (fx != null && !allEffects.Contains(fx))
            {
                allEffects.Add(fx);
            }
        }

        //変更を保存してUnityに覚えさせる
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"【完了】 {allEffects.Count} 個のEffectアセットを登録");
#else
        Debug.LogWarning("自動収集はエディタ専用機能です。");
#endif
    }
}
