#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundDatabase))]
public class SoundDatabaseEditor : Editor
{
    private static AudioSource previewSource;
    private int selectedBgmIndex = 0;
    private int selectedSeIndex = 0;

    public override void OnInspectorGUI()
    {
        var db = (SoundDatabase)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("サウンドプレビュー", EditorStyles.boldLabel);

        bool isPlaying = previewSource != null && previewSource.isPlaying;

        // BGM 選択＆再生・停止切り替えボタン
        if (db.bgmList != null && db.bgmList.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            selectedBgmIndex = Mathf.Clamp(selectedBgmIndex, 0, db.bgmList.Count - 1);

            string[] bgmOptions = new string[db.bgmList.Count];
            var bgmNames = System.Enum.GetValues(typeof(BgmName));
            for (int i = 0; i < db.bgmList.Count; i++)
            {
                if (i < bgmNames.Length)
                {
                    bgmOptions[i] = bgmNames.GetValue(i).ToString();
                }
                else
                {
                    bgmOptions[i] = string.IsNullOrEmpty(db.bgmList[i].name) ? $"[Index {i}]" : db.bgmList[i].name;
                }
            }

            selectedBgmIndex = EditorGUILayout.Popup("BGM 選択", selectedBgmIndex, bgmOptions);

            var sound = db.bgmList[selectedBgmIndex];
            bool isThisBgmPlaying = isPlaying && previewSource.clip == (sound != null ? sound.clip : null);

            GUI.backgroundColor = isThisBgmPlaying ? new Color(1f, 0.6f, 0.6f) : Color.white;
            string btnText = isThisBgmPlaying ? "停止" : "再生";

            if (GUILayout.Button(btnText, GUILayout.Width(60)))
            {
                if (isThisBgmPlaying)
                {
                    StopPreview();
                }
                else if (sound != null)
                {
                    PlayClip(sound.clip, sound.volume);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        // SE 選択＆再生・停止切り替えボタン
        if (db.seList != null && db.seList.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            selectedSeIndex = Mathf.Clamp(selectedSeIndex, 0, db.seList.Count - 1);

            string[] seOptions = new string[db.seList.Count];
            var seNames = System.Enum.GetValues(typeof(SeName));
            for (int i = 0; i < db.seList.Count; i++)
            {
                if (i < seNames.Length)
                {
                    seOptions[i] = seNames.GetValue(i).ToString();
                }
                else
                {
                    seOptions[i] = string.IsNullOrEmpty(db.seList[i].name) ? $"[Index {i}]" : db.seList[i].name;
                }
            }

            selectedSeIndex = EditorGUILayout.Popup("SE 選択", selectedSeIndex, seOptions);

            var sound = db.seList[selectedSeIndex];
            bool isThisSePlaying = isPlaying && previewSource.clip == (sound != null ? sound.clip : null);

            GUI.backgroundColor = isThisSePlaying ? new Color(1f, 0.6f, 0.6f) : Color.white;
            string btnText = isThisSePlaying ? "停止" : "再生";

            if (GUILayout.Button(btnText, GUILayout.Width(60)))
            {
                if (isThisSePlaying)
                {
                    StopPreview();
                }
                else if (sound != null)
                {
                    PlayClip(sound.clip, sound.volume);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // 更新ボタン
        GUI.backgroundColor = new Color(0.8f, 0.95f, 1f);
        if (GUILayout.Button("BGMとSEの定数クラスを更新", GUILayout.Height(40)))
            UpdateSoundNames();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        //通常のインスペクター表示
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning("再生する AudioClip が設定されていません。");
            return;
        }

        InitPreviewSource();
        previewSource.clip = clip;
        previewSource.volume = volume;
        previewSource.Play();
    }

    private void StopPreview()
    {
        if (previewSource != null)
        {
            previewSource.Stop();
        }
    }

    private void InitPreviewSource()
    {
        if (previewSource == null)
        {
            var go = GameObject.Find("EditorSoundPreviewObject");
            if (go == null)
            {
                go = new GameObject("EditorSoundPreviewObject");
                go.hideFlags = HideFlags.HideAndDontSave;
            }

            previewSource = go.GetComponent<AudioSource>();
            if (previewSource == null)
            {
                previewSource = go.AddComponent<AudioSource>();
            }
        }
    }

    private void OnDisable()
    {
        if (previewSource != null)
        {
            DestroyImmediate(previewSource.gameObject);
        }
    }

    private void UpdateSoundNames()
    {
        var db = (SoundDatabase)target;
        string directoryPath = "Assets/Plugins/AudioManager";

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        string bgmFilePath = directoryPath + "/BgmName.cs";
        using (StreamWriter writer = new StreamWriter(bgmFilePath))
        {
            writer.WriteLine("public enum BgmName {");
            if (db.bgmList != null)
            {
                foreach (var sound in db.bgmList)
                {
                    if (sound != null && !string.IsNullOrEmpty(sound.name) && !sound.name.Contains(" "))
                        writer.WriteLine($"    {sound.name},");
                }
            }
            writer.WriteLine("}");
        }

        string seFilePath = directoryPath + "/SeName.cs";
        using (StreamWriter writer = new StreamWriter(seFilePath))
        {
            writer.WriteLine("public enum SeName {");
            if (db.seList != null)
            {
                foreach (var sound in db.seList)
                {
                    if (sound != null && !string.IsNullOrEmpty(sound.name) && !sound.name.Contains(" "))
                        writer.WriteLine($"    {sound.name},");
                }
            }
            writer.WriteLine("}");
        }

        AssetDatabase.Refresh();
        Debug.Log("BgmName.cs と SeName.cs を更新しました: " + directoryPath);
    }
}
#endif