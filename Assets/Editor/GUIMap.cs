using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using MapzenGo.Helpers;
using MapzenGo.Helpers.VectorD;

public class GUIMap
{
    /* -----------------------------
     * Attributes
     *-----------------------------*/

    // Delegates
    public delegate void RepaintDelegate();

    // Attributes
    public RepaintDelegate Repaint;
    public int Zoom { get { return zoom; } set { zoom = Mathf.Clamp(value, 0, 19); } }
    public List<GMLGeometry> Geometries { get; set; }
    public Vector2d Center { get; set; }

    // private attributes
    protected int zoom = 0;

    /* -----------------------------
     * Constructor
     *-----------------------------*/

    public GUIMap()
    {
        Geometries = new List<GMLGeometry>();
    }

    /* -----------------------------
     *  Main method
     *-----------------------------*/

    public bool DrawMap(Rect area)
    {
        switch (Event.current.type)
        {
            case EventType.Repaint:
                {
                    // Draw the tiles
                    DrawTiles(area);

                    // Draw the GeoShapes
                    DrawGeometries(area);
                }
                break;

            case EventType.ScrollWheel:
                {
                    // Changezoom
                    Zoom += Mathf.FloorToInt(-Event.current.delta.y / 3f);
                    Event.current.Use();
                }
                break;


            case EventType.mouseDrag:
                {
                    // MoveLatLon or center var 
                    var centerPixel = GM.MetersToPixels(GM.LatLonToMeters(Center.y, Center.x), Zoom);
                    Center = GM.MetersToLatLon(GM.PixelsToMeters(centerPixel + new Vector2d(-Event.current.delta.x, -Event.current.delta.y), Zoom));
                    
                    Event.current.Use();
                }
                break;
            case EventType.mouseDown:
                {
                    return true;
                }
                break;
        }

        return false;

    }

    /* -----------------------------
     *  Drawing methods
     *-----------------------------*/

    protected void DrawTiles(Rect area)
    {
        // Download and draw tiles
        var v2 = GM.LatLonToMeters(Center.y, Center.x);
        var tile = GM.MetersToTile(v2, Zoom);
        var centerPixel = GM.MetersToPixels(v2, Zoom);

        Vector2d topLeftCorner = GM.PixelsToTile(centerPixel - new Vector2d(area.width / 2f, area.height / 2f)),
            bottomRightCorner = GM.PixelsToTile(centerPixel + new Vector2d(area.width / 2f, area.height / 2f));

        // pixel absolute to relative adition
        Vector2 patr = -(centerPixel.ToVector2() - (area.size / 2f));

        for (double x = topLeftCorner.x; x <= bottomRightCorner.x; x++)
        {
            for (double y = topLeftCorner.y; y <= bottomRightCorner.y; y++)
            {
                var tp = TileProvider.GetTile(new Vector3d(x, y, Zoom), (texture) => { if (Repaint != null) Repaint(); });
                var tileBounds = GM.TileBounds(new Vector2d(x, y), Zoom);
                var tileRect = ExtensionRect.FromCorners(
                    GM.MetersToPixels(tileBounds.Min, Zoom).ToVector2(),
                    GM.MetersToPixels(tileBounds.Min + tileBounds.Size, Zoom).ToVector2());

                var windowRect = new Rect(tileRect.position + patr, tileRect.size);
                var areaRect = windowRect.Intersection(area);

                GUI.DrawTextureWithTexCoords(areaRect, tp.Texture, windowRect.ToTexCoords(areaRect));
            }
        }
    }

    protected void DrawGeometries(Rect area)
    {        
        // Download and draw tiles
        var v2 = GM.LatLonToMeters(Center.y, Center.x);
        var tile = GM.MetersToTile(v2, Zoom);
        var centerPixel = GM.MetersToPixels(v2, Zoom);

        // pixel absolute to relative adition
        Vector2 patr = -(centerPixel.ToVector2() - (area.size / 2f));

        foreach(var g in Geometries)
        {
            // Convert from lat lon to pixel relative to the rect
            List<Vector2> points = g.Points.ConvertAll(p => GM.MetersToPixels(GM.LatLonToMeters(p.y, p.x), Zoom).ToVector2() + patr);
            
            Handles.BeginGUI();
            switch (g.Type)
            {
                case GMLGeometry.GeometryType.Point:
                    DrawPoint(points[0]);
                    break;
                case GMLGeometry.GeometryType.LineString:
                    DrawPolyLine(points.ToArray());
                    points.ForEach(p => DrawPoint(p));
                    break;
                case GMLGeometry.GeometryType.Polygon:
                   
                    DrawPolygon(points.ToArray());

                    var cicle = new List<Vector2>();
                    cicle.AddRange(points);
                    cicle.Add(points[0]);

                    DrawPolyLine(cicle.ToArray());
                    points.ForEach(p => DrawPoint(p));
                    break;
            }

            Handles.EndGUI();

        }
    }

    private void DrawPoint(Vector2 position)
    {
        Handles.color = Color.black;
        Handles.DrawSolidDisc(position, Vector3.forward, 4f);
        Handles.color = Color.blue;
        Handles.DrawSolidDisc(position, Vector3.forward, 3.5f);
    }

    private void DrawPolyLine(Vector2[] points)
    {
        Handles.color = Color.black;
        Handles.DrawAAPolyLine(2f, V2ToV3(points));
    }

    private void DrawPolygon(Vector2[] points)
    {
        Handles.color = new Color(.2f,.2f,.9f,.5f);
        Handles.DrawAAConvexPolygon(V2ToV3(points));
    }

    private Vector3[] V2ToV3(Vector2[] points)
    {
        var l = new List<Vector2>();
        l.AddRange(points);
        return l.ConvertAll(p => new Vector3(p.x, p.y, 0f)).ToArray();
    }

}


/* --------------------
 * Rect extension class
 * -------------------- */

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

    public static Rect ToTexCoords(this Rect rect, Rect slice)
    {
        return new Rect(
            (slice.x - rect.x) / rect.width,
            1f - (((slice.y + slice.height) - (rect.y + rect.height)) / rect.height),
            slice.width / rect.width,
            slice.height / rect.height
            );
    }

    public static Rect FromCorners(Vector2 o, Vector2 e)
    {
        return new Rect(o, e - o);
    }
}

