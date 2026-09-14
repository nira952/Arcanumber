using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "ScriptableObjects/SoundDatabase", order = 2)]
public class SoundDatabase : ScriptableObject
{
    public List<SoundData> bgmList;
    public List<SoundData> seList;
}

[System.Serializable]
public class SoundData
{
    public string name;
    public AudioClip clip;
    [Range(0, 1)] public float volume = 1.0f;
}