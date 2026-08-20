using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif


public class TransferFunctionCreator : MonoBehaviour
{
    [Header("密度の色設定 (左端:密度0 / 右端:密度1)")]
    public Gradient colorGradient;

#if UNITY_EDITOR
    [ContextMenu("Generate Transfer Texture")]
    public void GenerateTexture()
    {
        int width = 256;
        int height = 4; // 細長い画像にする
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // グラデーションから色をサンプリングして画像に書き込む
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

        // PNGとしてAssetsフォルダに保存
        string path = Application.dataPath + "/TransferFunction.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        
        AssetDatabase.Refresh();
        Debug.Log("Assetsフォルダに TransferFunction.png を作成しました！");
    }
#endif
}