using System.Collections.Generic;
using UnityEngine;

public class PixelArt : MonoBehaviour
{
    [Tooltip("ステップ1で作った『ドット化専用のマテリアル』をここにアサインします")]
    [SerializeField] private Material pixelMaterialTemplate;

    [Tooltip("ドットの粗さ（スクリプトから一括でコントロールする場合）")]
    [Range(1, 32)]
    [SerializeField] private float pixelSize = 4f;

    [Tooltip("子孫のパーティクルもすべて対象にするか")]
    [SerializeField] private bool includeChildren = true;

    private void Start()
    {
        ApplyPixelMaterial();
    }

    [ContextMenu("Apply Pixelization")]
    public void ApplyPixelMaterial()
    {
        if (pixelMaterialTemplate == null)
        {
            Debug.LogWarning("ドット化用のテンプレートマテリアルが設定されていません。", this);
            return;
        }

        ParticleSystemRenderer[] renderers = includeChildren
            ? GetComponentsInChildren<ParticleSystemRenderer>(true)
            : GetComponents<ParticleSystemRenderer>();

        foreach (var psr in renderers)
        {
            Material originalMat = psr.sharedMaterial;
            if (originalMat == null) continue;

            // 1. 元のマテリアルが持っている「メインテクスチャ」と「色」を安全に取得する
            Texture sourceTex = null;
            if (originalMat.HasProperty("_BaseMap")) sourceTex = originalMat.GetTexture("_BaseMap");
            else if (originalMat.HasProperty("_MainTex")) sourceTex = originalMat.GetTexture("_MainTex");

            Color sourceColor = Color.white;
            if (originalMat.HasProperty("_BaseColor")) sourceColor = originalMat.GetColor("_BaseColor");
            else if (originalMat.HasProperty("_Color")) sourceColor = originalMat.GetColor("_Color");

            // 2. 個別のインスタンスマテリアルを生成（元のアセットを汚さないため）
            Material newMat = new Material(pixelMaterialTemplate);

            // 3. 取得した元テクスチャと色を、ドット化マテリアルに流し込む
            if (sourceTex != null)
            {
                if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", sourceTex);
                if (newMat.HasProperty("_MainTex")) newMat.SetTexture("_MainTex", sourceTex);
            }
            if (newMat.HasProperty("_PixelSize"))
            {
                newMat.SetFloat("_PixelSize", pixelSize);
            }

            // 4. パーティクルに新しいドット化マテリアルを適用
            psr.material = newMat;
        }
    }
}
