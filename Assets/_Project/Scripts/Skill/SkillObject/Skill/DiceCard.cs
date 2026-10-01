using R3;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DiceCard : MonoBehaviour, IRpcObjectInterface
{
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private Sprite[] cardSprites;

    public int syncedDmg = 0;  //ダメージ

    private Subject<Unit> onDestroyed = new Subject<Unit>();
    public Observable<Unit> OnDestroyed => onDestroyed;

    public void RpcInitialize(int playerIndex)
    {
        if (renderer == null)
        {
            renderer = GetComponent<SpriteRenderer>();
        }

        if (renderer != null && syncedDmg > 0 && syncedDmg < cardSprites.Length + 1)
            renderer.sprite = cardSprites[syncedDmg - 1];

    }

}
