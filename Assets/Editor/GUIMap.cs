using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using MapzenGo.Helpers;
using MapzenGo.Helpers.VectorD;

public class GUIMap
{

    private struct Tile
    {
        public int zoom;
        public Vector2d tile;
    }

    
    private int Range = 3;
    public int Zoom { get; set; }
    public Vector2 LatLon { get; set; }

    public Texture2D loadingTexture;

    private Dictionary<Tile, TilePromise> tilecache;

    public Vector2 Center { get; set; }

    public void DrawMap(Rect area)
    {
        var v2 = GM.LatLonToMeters(Center.y, Center.x);
        var tile = GM.MetersToTile(v2, Zoom);
        var centerPixel = GM.MetersToPixels(v2, Zoom);

        if (Event.current.type == EventType.ScrollWheel)
        {
            // Changezoom
        }

        if (Event.current.type == EventType.mouseDrag)
        {
            // MoveLatLon or center
        }

        Vector2d topLeftCorner = GM.PixelsToTile(centerPixel - new Vector2d(area.width / 2f, area.height / 2f)),
            bottomRightCorner = GM.PixelsToTile(centerPixel + new Vector2d(area.width / 2f, area.height / 2f));

        // pixel absolute to relative adition
        Vector2 patr = - (centerPixel.ToVector2() + (area.size / 2f));

        for (double x = topLeftCorner.x; x < bottomRightCorner.x; x++)
        {
            for (double y = topLeftCorner.y; y < bottomRightCorner.y; y++)
            {
                var tp = TileProvider.GetTile(new Vector3d(x, y, Zoom), (texture) => { });
                var tileBounds = GM.TileBounds(new Vector2d(x, y), Zoom);
                var tileRect = ExtensionRect.FromCorners(
                    GM.MetersToPixels(tileBounds.Min, Zoom).ToVector2(),
                    GM.MetersToPixels(tileBounds.Min + tileBounds.Size, Zoom).ToVector2());
                
                var windowRect = new Rect(tileRect.position + patr, tileRect.size).Intersection(area);
                Debug.Log(tileBounds.Center.ToVector2() + " to  " + tileRect + " to " + windowRect);
                GUI.DrawTexture(windowRect, tp.Texture);
            }
        }
    }

    protected void DrawTiles(Vector2d pixelCenter, Vector2 rect, int zoom)
    {
        /*for (int i = -Range; i <= Range; i++)
        {
            for (int j = -Range; j <= Range; j++)
            {
                var v = new Vector2d(tms.x + i, tms.y + j);
                var t = new Tile();
                t.zoom = Zoom;
                t.tileTms = v;
                if (tilecache.ContainsKey(t))
                    continue;
                CreateTile(v, center);
            }
        }*/
    }

    protected virtual void CreateTile(Vector2d tileTms, Vector2d centerInMercator)
    {
        /*var rect = GM.TileBounds(tileTms, Zoom);

        var tile = new Tile();
        tile.zoom = Zoom;
        tile.tileTms = tileTms;

        tilecache.Add(tile, loadingTexture);*/

    }

}

public static class ExtensionRect
{
    public static Rect Intersection(this Rect rect, Rect other)
    {
        Vector2 r0o = rect.position,
            r0e = rect.position + rect.size,
            r1o = other.position,
            r1e = other.position + other.size;

        return FromCorners(
            new Vector2(Mathf.Max(r0o.x, r1o.x), Mathf.Max(r0o.y, r1o.y)), 
            new Vector2(Mathf.Min(r0e.x, r1e.x), Mathf.Min(r0e.y, r1e.y)));

    }

    public static Rect FromCorners(Vector2 o, Vector2 e)
    {
        return new Rect(o, e - o);
    }
}

