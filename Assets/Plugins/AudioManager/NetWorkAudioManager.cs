using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Unity.Netcode;

public class NetWorkAudioManager : NetworkBehaviour
{
    private static NetWorkAudioManager instance;
    public static NetWorkAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NetWorkAudioManager>();
            }
            return instance;
        }
    }

    [SerializeField] private AudioSetting audioData;
    [SerializeField] private AudioMixer audioMixer;

    [Header("Settings")]
    public int maxSeSources = 10;

    private AudioSource bgmSource;
    private List<AudioSource> seSources = new List<AudioSource>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // 単体のDontDestroyOnLoadだけでなく、NGOのネットワーク管理下で生存させる
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (audioData == null || audioMixer == null)
        {
            Debug.LogWarning("オーディオデータ、またはAudioMixerが参照できません");
            return;
        }

        InitAudioSources();
    }

    // ネットワークオブジェクトとして生成（スポーン）された時の処理
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // シーンが切り替わっても、このネットワークオブジェクトの所有権トラブルで消えないように設定
        if (NetworkObject != null)
        {
            NetworkObject.DontDestroyWithOwner = true;
        }
    }

    private void InitAudioSources()
    {
        AudioMixerGroup[] bgmGroups = audioMixer.FindMatchingGroups("Master/BGM");
        AudioMixerGroup[] seGroups = audioMixer.FindMatchingGroups("Master/SE");

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        if (bgmGroups.Length > 0) bgmSource.outputAudioMixerGroup = bgmGroups[0];

        for (int i = 0; i < maxSeSources; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            if (seGroups.Length > 0) source.outputAudioMixerGroup = seGroups[0];
            seSources.Add(source);
        }
    }

    // --- ここからローカル（自分の画面だけ）で鳴らす通常の関数 ---

    public void PlayLocal(BgmName name)
    {
        int kind = (int)name;
        bgmSource.clip = audioData.bgmList[kind].clip;
        bgmSource.volume = audioData.bgmList[kind].volume;
        bgmSource.Play();
    }

    public void PlayLocal(SeName name)
    {
        int kind = (int)name;
        AudioSource freeSource = GetFreeSeSource();

        if (freeSource == null) return;

        freeSource.clip = audioData.seList[kind].clip;
        freeSource.volume = audioData.seList[kind].volume;
        freeSource.Play();
    }


    // --- ここからNGO（ネットワーク経由で全員）に鳴らす関数 ---

    /// <summary>
    /// 【全員共有】誰から呼び出されても、ロビー内全員の画面で同じSEを再生します
    /// </summary>
    public void PlayGlobal(SeName name)
    {
        // もし自分がサーバー（ホスト）なら、直接全員に送る
        if (IsServer)
        {
            PlaySeRpc(name);
        }
        else // 自分がクライアントなら、一回サーバーにお願いする
        {
            RequestPlaySeServerRpc(name);
        }
    }

    /// <summary>
    /// 【全員共有】誰から呼び出されても、ロビー内全員の画面で同じBGMを再生します
    /// </summary>
    public void PlayGlobal(BgmName name)
    {
        if (IsServer)
        {
            PlayBgmRpc(name);
        }
        else
        {
            RequestPlayBgmServerRpc(name);
        }
    }

    // --- NGO用のRPCメソッド群 ---

    // 1. クライアントからサーバーへ「音を鳴らして」とお願いするRPC
    [ServerRpc(RequireOwnership = false)] // 所有権を持たないクライアントからの実行を許可
    private void RequestPlaySeServerRpc(SeName name)
    {
        // サーバーがそれを受け取り、全クライアントに「鳴らせ！」と命令を送る
        PlaySeRpc(name);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayBgmServerRpc(BgmName name)
    {
        PlayBgmRpc(name);
    }

    // 2. サーバーから全クライアントへ「この音を鳴らせ」と命令するRPC
    [Rpc(SendTo.Everyone)] // 参加している全員（自分含む）の画面で実行される
    private void PlaySeRpc(SeName name)
    {
        // 最終的に全員のローカルで実際に再生される
        PlayLocal(name);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayBgmRpc(BgmName name)
    {
        PlayLocal(name);
    }


    // 便利機能：空いているソースを探す処理を共通化
    private AudioSource GetFreeSeSource()
    {
        for (int i = 0; i < seSources.Count; i++)
        {
            if (!seSources[i].isPlaying) return seSources[i];
        }
        Debug.LogWarning("SEの最大同時再生数を超えているため再生をスキップしました。");
        return null;
    }
}