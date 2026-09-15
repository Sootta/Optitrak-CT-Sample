using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class TransferFunctionCreator : MonoBehaviour
{
    [Header("現在のグラデーション設定")]
    public Gradient colorGradient;

    public enum MedicalPreset
    {
        Custom,
        CT_Bone,         // 骨のみを抽出
        CT_SoftTissue,   // 筋肉や臓器を中心に描画
        CT_Angio         // 血管（造影）と骨を描画
    }

    [Header("医療用プリセット")]
    public MedicalPreset preset = MedicalPreset.CT_Bone;

#if UNITY_EDITOR
    // インスペクタの値が変更された時にプレビューとしてGradientを更新する
    private void OnValidate()
    {
        ApplyPreset();
    }

    private void ApplyPreset()
    {
        if (preset == MedicalPreset.Custom) return;

        colorGradient = new Gradient();
        colorGradient.mode = GradientMode.Blend;

        GradientColorKey[] colorKeys = null;
        GradientAlphaKey[] alphaKeys = null;

        // 0〜1023 のデータを HU値の -500 〜 +1500 程度の範囲と仮定してマッピング
        switch (preset)
        {
            case MedicalPreset.CT_Bone:
                // 骨（高い密度）のみを抽出。白〜アイボリー
                colorKeys = new[] {
                    new GradientColorKey(new Color(0.89f, 0.85f, 0.78f), 0.4f), // 象牙色
                    new GradientColorKey(Color.white, 1.0f)
                };
                alphaKeys = new[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 0.35f), // 軟部組織までは透明
                    new GradientAlphaKey(0.8f, 0.5f),  // 骨の開始付近から不透明度を上げる
                    new GradientAlphaKey(1.0f, 1.0f)
                };
                break;

            case MedicalPreset.CT_SoftTissue:
                // 筋肉、臓器などの軟部組織。赤〜オレンジ〜黄色系
                colorKeys = new[] {
                    new GradientColorKey(Color.black, 0.0f),
                    new GradientColorKey(new Color(0.7f, 0.2f, 0.1f), 0.25f), // 筋肉/臓器
                    new GradientColorKey(new Color(0.9f, 0.8f, 0.5f), 0.5f),  // 脂肪/結合組織
                    new GradientColorKey(Color.white, 1.0f)
                };
                alphaKeys = new[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 0.15f), 
                    new GradientAlphaKey(0.4f, 0.25f), // 軟部組織を描画
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(1.0f, 1.0f)
                };
                break;

            case MedicalPreset.CT_Angio:
                // 造影剤による血管（赤）と骨（白）
                colorKeys = new[] {
                    new GradientColorKey(Color.black, 0.0f),
                    new GradientColorKey(new Color(0.9f, 0.1f, 0.1f), 0.3f), // 血管
                    new GradientColorKey(new Color(1.0f, 0.9f, 0.8f), 0.6f), // 骨
                    new GradientColorKey(Color.white, 1.0f)
                };
                alphaKeys = new[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 0.25f), // 筋肉などは消す
                    new GradientAlphaKey(0.6f, 0.3f),  // 血管
                    new GradientAlphaKey(0.9f, 0.6f),  // 骨
                    new GradientAlphaKey(1.0f, 1.0f)
                };
                break;
        }

        colorGradient.SetKeys(colorKeys, alphaKeys);
    }

    [ContextMenu("Generate Medical Transfer Texture")]
    public void GenerateTexture()
    {
        int width = 1024;
        int height = 4; 
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int x = 0; x < width; x++)
        {
            float t = (float)x / (width - 1);
            Color col = colorGradient.Evaluate(t);

            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();

        string path = Application.dataPath + $"/MedicalTransfer_{preset}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        
        AssetDatabase.Refresh();
        Debug.Log($"Assetsフォルダに MedicalTransfer_{preset}.png を作成しました！");
    }
#endif
}