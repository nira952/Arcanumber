using Unity.Services.Lobbies.Models;
using UnityEngine;

public class AIAttack : MonoBehaviour
{
    [SerializeField] private float coolTime = 5f;   //クールタイム
    [SerializeField] private NetworkPlayer aiPlayer;    //攻撃するキャラクター
    [SerializeField] private Transform pos; //攻撃を出す場所
    [SerializeField] private Skill skill;   //出すスキル

    private float time = 0; //現在のタイム

    // Update is called once per frame
    void Update()
    {
        if (!TimeCount()) return;
        //スキルを生成する
        SkillInstance();
        time = 0;
    }
    
    bool TimeCount()
    {
        time += Time.deltaTime;
        return time >= coolTime;
    }

    /// <summary>
    /// スキルを生成するメソッド
    /// </summary>
    void SkillInstance()
    {
        GameObject s = Instantiate(skill.GetEffectAnimation());
        s.transform.position = pos.position;
        MagicObject magic = s.GetComponent<MagicObject>();
        //magic.Initialize(aiPlayer.GetNetworkId(), skill, pos.position);

        float directionX = transform.localScale.x;

        //magic.SetupPositionAndRotation(pos.position);

        //AIが左向きの場合、回転を180度反転させる
        if (directionX < 0)
        {
            //回転を反転させる
            s.transform.rotation = Quaternion.Euler(0, 0, s.transform.rotation.eulerAngles.z + 180f);

            //ついでに見た目も反転させるならこちら（必要に応じて）
            Vector3 scale = s.transform.localScale;
            scale.y *= -1;
            s.transform.localScale = scale;
        }
    }
}
