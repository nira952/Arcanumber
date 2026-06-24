using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


// 必要なBGMを入力
public enum BgmName
{
    Title,
    Game,
    num
}

// 必要なSEを入力
public enum SeName
{
    Damege,
    Attack,
    Button,
    num,
}




[System.Serializable]
public class AudioDate
{
    public string name;
    [Header("オーディオクリップ")]
    public AudioClip clip;
    [Header("クリップの音量"), Range(0, 1)]
    public float volume = 1f;
}



[CreateAssetMenu(fileName = "AudioSetting", menuName = "Scriptable Objects/AudioSetting")]
public class AudioSetting : ScriptableObject
{
    [Header("BGMを設定")]
    public List<AudioDate> bgmList = new List<AudioDate>();

    [Header("SEを設定")]
    public List<AudioDate> seList = new List<AudioDate>();

    [Header("BGMを聞く")]
    public bool isBgm = false;
    [Header("SEを聞く")]
    public bool isSe = false;

    public SeName sanpleSe;
    public BgmName sanpleBgm;

}
#if UNITY_EDITOR
[CustomEditor(typeof(AudioSetting))]
public class ExampleScriptEditor : Editor
{
    public void Awake()
    {
        Debug.Log("再設定");

        AudioSetting set = (AudioSetting)target;

        // BGMを設定 & 表示
        set.bgmList = AudioSet(set.bgmList, typeof(BgmName), (int)BgmName.num);

        // SEを設定 & 表示
        set.seList = AudioSet(set.seList, typeof(SeName), (int)SeName.num);

    }

    private List<AudioDate> AudioSet(List<AudioDate> list, Type enumType, int length)
    {
        // リストを初期化
        List<AudioDate> saveList = new List<AudioDate>(list);
        list.Clear();

        // リストの再生成
        for (int i = 0; i < length; i++)
        {
            AudioDate date = new AudioDate();
            date.name = Enum.GetName(enumType, i);
            list.Add(date);

        }
        // 元の配列に同じ名前があるならデータを引き継ぐ
        for (int i = 0; i < list.Count; i++)
        {
            for (int j = 0; j < saveList.Count; j++)
            {
                if (list[i].name == saveList[j].name)
                {
                    list[i].clip = saveList[j].clip;
                    list[i].volume = saveList[j].volume;

                }

            }
        }

        return list;

    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // オブジェクトの状態を更新

        AudioSetting set = (AudioSetting)target;

        CustomOnGUI();             // 初期表示


        if (set.isSe)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sanpleSe"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("SEを再生"))
            {
                PlayClipWithVolume(set.seList[(int)set.sanpleSe].clip, set.seList[(int)set.sanpleSe].volume);
            }
            if (GUILayout.Button("停止"))
            {
                StopClip();
            }
            EditorGUILayout.EndHorizontal();

            set.isBgm = false;
        }

        if (set.isBgm)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sanpleBgm"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("BGMを再生"))
            {
                PlayClipWithVolume(set.bgmList[(int)set.sanpleBgm].clip, set.bgmList[(int)set.sanpleBgm].volume);
            }
            if (GUILayout.Button("停止"))
            {
                StopClip();
            }
            EditorGUILayout.EndHorizontal();


            set.isSe = false;
        }



        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            DestroyImmediate(GameObject.Find("SampleAudioSource"));
        }


        serializedObject.ApplyModifiedProperties(); // 変更を適用

    }


    private static AudioSource audioSource;

    public static void PlayClipWithVolume(AudioClip clip, float volume)
    {
        if (audioSource == null)
        {
            GameObject SampleAudioSource = new GameObject("SampleAudioSource");
            audioSource = SampleAudioSource.AddComponent<AudioSource>();
        }

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();

    }

    public static void StopClip()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            DestroyImmediate(audioSource.gameObject);

            if(GameObject.Find("SampleAudioSource") != null)
            {
                DestroyImmediate(GameObject.Find("SampleAudioSource"));

            }
        }
    }

    /// <summary>
    /// インスペクターの表示をカスタムする
    /// </summary>
    /// <param name="editor"></param>
    public void CustomOnGUI()
    {

        // BGMリストを描画
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bgmList"));
        // SEリストを描画
        EditorGUILayout.PropertyField(serializedObject.FindProperty("seList"));

        EditorGUILayout.Space(10);


        EditorGUILayout.PropertyField(serializedObject.FindProperty("isBgm"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isSe"));


        EditorGUILayout.Space(10);
    }
}
#endif