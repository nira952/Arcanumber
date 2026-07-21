using UnityEngine;

public class Turret : MonoBehaviour
{
    private int haveNo;
    [SerializeField] GameObject bullet; //弾
    [SerializeField] GameObject spwnObj;    //スポーン用
    float createSpeed = 1f;
    float time = 0;
    void Start()
    {
        TurretDirection();
    }

    // Update is called once per frame
    void Update()
    {
        TimeCount();
    }

    /// <summary>
    /// タレットの向きを決めるメソッド
    /// </summary>
    void TurretDirection()
    {
        //０より大きいなら左、小さいなら右
        if (transform.position.x >= 0)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else
            transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    
    /// <summary>
    /// カウントダウン
    /// </summary>
    void TimeCount()
    {
        time += Time.deltaTime;
        if(time > createSpeed)
        {
            InstanceBullet();
            time = 0;
        }
    }

    /// <summary>
    /// バレットの生成
    /// </summary>
    void InstanceBullet()
    {
        //弾を生成
        GameObject b = Instantiate(bullet);
        //場所と向きを決定
        b.transform.position = spwnObj.transform.position;
        b.transform.rotation = transform.rotation;
        //クラスを取得して初期化する
        Bullet bulletScript = b.GetComponent<Bullet>();
        if (bulletScript != null)
            //初期化する
            bulletScript.InitializeBullet(haveNo, b.transform.position);
    }

   public void SetHaveNo(int no) { haveNo = no; }
}
