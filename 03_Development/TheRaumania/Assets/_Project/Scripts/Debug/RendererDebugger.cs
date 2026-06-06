using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RendererDebugger : MonoBehaviour
{
    // Press the `F9` key in Play mode to print nearby renderers.
    public float scanRadius = 10f;
    public int maxResults = 20;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ScanAndLog();
        }
    }

    void ScanAndLog()
    {
        Vector3 pos = transform.position;

        var mesh = Object.FindObjectsOfType<MeshRenderer>()
            .Select(r => new { renderer = (Renderer)r, dist = Vector3.Distance(r.transform.position, pos) })
            .Where(x => x.dist <= scanRadius)
            .OrderBy(x => x.dist)
            .Take(maxResults)
            .ToList();

        var sprites = Object.FindObjectsOfType<SpriteRenderer>()
            .Select(r => new { renderer = (Renderer)r, dist = Vector3.Distance(r.transform.position, pos) })
            .Where(x => x.dist <= scanRadius)
            .OrderBy(x => x.dist)
            .Take(maxResults)
            .ToList();

        var tiles = Object.FindObjectsOfType<TilemapRenderer>()
            .Select(r => new { renderer = (Renderer)r, dist = Vector3.Distance(r.transform.position, pos) })
            .Where(x => x.dist <= scanRadius)
            .OrderBy(x => x.dist)
            .Take(maxResults)
            .ToList();

        Debug.Log($"RendererDebugger: scanning around {pos} (radius={scanRadius})");

        int idx = 0;
        foreach (var r in mesh.Concat(sprites).Concat(tiles).OrderBy(x => x.dist))
        {
            var ren = r.renderer;
            string type = ren is MeshRenderer ? "Mesh" : ren is SpriteRenderer ? "Sprite" : "Tilemap";
            string sortInfo = "";
            if (ren is SpriteRenderer sr)
                sortInfo = $" layer={sr.sortingLayerName} order={sr.sortingOrder}";
            else if (ren is MeshRenderer mr)
                sortInfo = $" shadowCasting={mr.shadowCastingMode} layer={mr.gameObject.layer} z={mr.transform.position.z}";
            else if (ren is TilemapRenderer tr)
                sortInfo = $" layer={tr.sortingLayerName} order={tr.sortingOrder} z={tr.transform.position.z}";

            Debug.LogFormat("[{0}] {1} ({2}) dist={3:F2}{4} path={5}", idx, ren.name, type, r.dist, sortInfo, GetGameObjectPath(ren.gameObject));
            idx++;
        }

        if (idx == 0)
            Debug.Log("RendererDebugger: no renderers found within radius");
    }

    string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
