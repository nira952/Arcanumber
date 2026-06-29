using UnityEngine;

/// <summary>
/// 基礎的なプレイヤークラス
/// </summary>
public class PlayerStatus
{
    float maxHp = 100;    //HP最大値
    float atk = 1f; //攻撃値
    float def = 1f; //防御値
    float speed = 5f;   //速度
    int maxJump = 1;  //最大ジャンプ回数

    //プレイヤーの色
    Color[] charaColors = new Color[4] { Color.blue, Color.red, Color.green, Color.yellow };

    /**
     * --------- ゲッター ---------
     */
    public float GetMaxHp() {  return maxHp; }
    public float GetAtk() { return atk; }
    public float GetDef() { return def; }
    public float GetSpeed() { return speed; }
    public int GetMaxJump() {  return maxJump; }
    public Color GetCharaColor(int charaNo) { return charaColors[charaNo]; }

    /**
    * --------- セッター ---------
    */
    public void SetMaxHp(float hp) { maxHp = hp; }
    public void SetAtk(float atk) { this.atk = atk; }
    public void SetDef(float def) { this.def = def; }
    public void SetSpeed(float speed) { this.speed = speed; }
    public void SetMaxJump(int jump) { maxJump = jump; }
}
