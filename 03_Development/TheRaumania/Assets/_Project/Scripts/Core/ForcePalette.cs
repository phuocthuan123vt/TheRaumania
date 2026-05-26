using UnityEngine;
using UnityEditor;

public class ForcePalette
{
    [MenuItem("Raumania/Force Create Palette")]
    public static void Create()
    {
        GameObject paletteObj = new GameObject("pal_Fixed_System");
        paletteObj.AddComponent<Grid>();

        string path = "Assets/_Project/Art/GroundTiles/groundpal_Fixed_System.prefab";

        PrefabUtility.SaveAsPrefabAsset(paletteObj, path);

        Object.DestroyImmediate(paletteObj);
        AssetDatabase.Refresh();

        Debug.Log("<color=green>Đã tạo thành công Palette tại: </color>" + path);
    }
}