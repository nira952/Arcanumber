using Unity.Services.Lobbies.Models;
using UnityEngine;

public class AIAttack : MonoBehaviour
{
    [SerializeField] private float coolTime = 5f;   //クールタイム
    [SerializeField] private NetworkPlayer aiPlayer;    //攻撃するキャラクター
    [SerializeField] private Transform pos; //攻撃を出す場所
    [SerializeField] private GameObject bullet;

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
        GameObject s = Instantiate(bullet);
        s.transform.position = pos.position;
        Bullet magic = s.GetComponent<Bullet>();
        magic.InitializeBullet(aiPlayer.GetNetworkId(), aiPlayer.transform.position);

        float directionX = transform.localScale.x;


        //AIが左向きの場合、回転を180度反転させる
        if (directionX < 0)
        {
            //回転を反転させる
            s.transform.rotation = Quaternion.Euler(0, 0, s.transform.rotation.eulerAngles.z + 180f);

            //ついでに見た目も反転させる
            Vector3 scale = s.transform.localScale;
            scale.y *= -1;
            s.transform.localScale = scale;
        }
    }
}
