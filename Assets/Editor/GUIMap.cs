using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using MapzenGo.Helpers;
using MapzenGo.Helpers.VectorD;

public class GUIMap {

    private struct Tile
    {
        public int zoom;
        public Vector2d tileTms;
    }
    private int Range = 3;
    public int Zoom { get; set; }
    public Vector2 LatLon { get; set; }

    public Texture2D loadingTexture;

    private Dictionary<Tile, Texture2D> tilecache;

    public Vector2 Center { get; set; }

    public void DrawMap(Rect area)
    {
        var v2 = GM.LatLonToMeters(Center.y, Center.x);
        var tile = GM.MetersToTile(v2, Zoom);

        if(Event.current.type == EventType.ScrollWheel)
        {
            // Changezoom
        }

        if(Event.current.type == EventType.mouseDrag)
        {
            // MoveLatLon or center
        }
        

        var CenterTms = tile;
        var CenterInMercator = GM.TileBounds(CenterTms, Zoom).Center;

        LoadTiles(CenterTms, CenterInMercator);

        UpdateTiles();
        DrawTiles();
    }

    protected void LoadTiles(Vector2d tms, Vector2d center)
    {
        for (int i = -Range; i <= Range; i++)
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
        }
    }

    protected virtual void CreateTile(Vector2d tileTms, Vector2d centerInMercator)
    {
        var rect = GM.TileBounds(tileTms, Zoom);

        var tile = new Tile();
        tile.zoom = Zoom;
        tile.tileTms = tileTms;

        tilecache.Add(tile, loadingTexture);
        
    }

}
