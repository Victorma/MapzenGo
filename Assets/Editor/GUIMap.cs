﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
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
    public int Zoom { get { return zoom; }
        set
        {
            zoom = Mathf.Clamp(value, 0, 19);
            centerPixel = GM.MetersToPixels(GM.LatLonToMeters(Center.y, Center.x), zoom);
        }
    }
    public List<GMLGeometry> Geometries { get; set; }
    public GMLGeometry selectedGeometry;
    public double SelectPointDistance { get; set; }
    public Vector2d Center { get { return center; }
        set
        {
            center = value;
            centerPixel = GM.MetersToPixels(GM.LatLonToMeters(center.y, center.x), Zoom);
        }
    }
    public Vector2d GeoMousePosition { get; set; }

    // private attributes
    protected int zoom = 0;
    protected Vector2d center;
    protected Vector2d centerPixel;
    private Vector2d PATR; // Will be calculated in the begining of each iteration

    /* -----------------------------
     * Constructor
     *-----------------------------*/

    public GUIMap()
    {
        Geometries = new List<GMLGeometry>();
        SelectPointDistance = 15.0;
    }

    /* -----------------------------
     *  Main method
     *-----------------------------*/

    public bool DrawMap(Rect area)
    {
        // update the pixel absolute to relative convert variable
        PATR = -(centerPixel - (area.size / 2f).ToVector2d() - area.position.ToVector2d());

        var mousePos = Event.current.mousePosition.ToVector2d();
        var delta = new Vector2d(Event.current.delta.x, Event.current.delta.y);
        GeoMousePosition = GM.MetersToLatLon(GM.PixelsToMeters(RelativeToAbsolute(mousePos), Zoom));

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
                    if (area.Contains(Event.current.mousePosition))
                    {
                        if (selectedGeometry != null)
                        {
                            var pixels = LatLonToPixels(selectedGeometry.Points);

                            // Find the closest point
                            var point = PixelsToRelative(pixels)
                                .FindIndex(p => (p - mousePos).magnitude < SelectPointDistance);
                            // If there's a point, move the point
                            if(point != -1) pixels[point] += delta;
                            // Otherwise, move the pixel
                            else pixels = pixels.ConvertAll(p => p + delta);
                            selectedGeometry.Points = PixelsToLatLon(pixels);
                        }
                        else
                        {
                            Center = GM.MetersToLatLon(GM.PixelsToMeters(centerPixel - delta, Zoom));
                        }
                        Event.current.Use();
                    }
                }
                break;
            case EventType.mouseDown:
                {
                    selectedGeometry = Geometries.Find(g =>
                    {
                        List<Vector2d> points = PixelsToRelative(LatLonToPixels(g.Points))
                            .ConvertAll(p => p - Event.current.mousePosition.ToVector2d());
                        
                        return Inside(mousePos, points) || points.Any(p => p.magnitude < SelectPointDistance); 
                    });

                    if (area.Contains(Event.current.mousePosition))
                    {
                        GUI.FocusControl(null);
                        return true;
                    }

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
        var tile = GM.MetersToTile(GM.LatLonToMeters(Center.y, Center.x), Zoom);

        Vector2d topLeftCorner = GM.PixelsToTile(centerPixel - new Vector2d(area.width / 2f, area.height / 2f)),
            bottomRightCorner = GM.PixelsToTile(centerPixel + new Vector2d(area.width / 2f, area.height / 2f));

        for (double x = topLeftCorner.x; x <= bottomRightCorner.x; x++)
        {
            for (double y = topLeftCorner.y; y <= bottomRightCorner.y; y++)
            {
                var tp = TileProvider.GetTile(new Vector3d(x, y, Zoom), (texture) => { if (Repaint != null) Repaint(); });
                var tileBounds = GM.TileBounds(new Vector2d(x, y), Zoom);
                var tileRect = ExtensionRect.FromCorners(
                    GM.MetersToPixels(tileBounds.Min, Zoom).ToVector2(),
                    GM.MetersToPixels(tileBounds.Min + tileBounds.Size, Zoom).ToVector2());

                var windowRect = new Rect(tileRect.position + PATR.ToVector2(), tileRect.size);
                var areaRect = windowRect.Intersection(area);
                if (areaRect.width < 0 || areaRect.height < 0)
                    continue;

                GUI.DrawTextureWithTexCoords(areaRect, tp.Texture, windowRect.ToTexCoords(areaRect));
            }
        }
    }

    protected void DrawGeometries(Rect area)
    {        
        foreach(var g in Geometries)
        {
            // Convert from lat lon to pixel relative to the rect
            List<Vector2d> points = PixelsToRelative(LatLonToPixels(g.Points));
            if (points.Count == 0)
                continue;


            Handles.BeginGUI();
            switch (g.Type)
            {
                case GMLGeometry.GeometryType.Point:
                    DrawPoint(points[0].ToVector2());
                    break;
                case GMLGeometry.GeometryType.LineString:
                    DrawPolyLine(points.ConvertAll(p => p.ToVector2()).ToArray());
                    points.ForEach(p => DrawPoint(p.ToVector2()));
                    break;
                case GMLGeometry.GeometryType.Polygon:
                   
                    DrawPolygon(points.ConvertAll(p => p.ToVector2()).ToArray());

                    var cicle = new List<Vector2d>();
                    cicle.AddRange(points);
                    cicle.Add(points[0]);

                    DrawPolyLine(cicle.ConvertAll(p => p.ToVector2()).ToArray());
                    points.ForEach(p => DrawPoint(p.ToVector2()));
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

    private List<Vector2d> LatLonToPixels(List<Vector2d> points)
    {        
        return points.ConvertAll(p => GM.MetersToPixels(GM.LatLonToMeters(p.y, p.x), Zoom)); 
    }

    private List<Vector2d> PixelsToLatLon(List<Vector2d> points)
    {
        return points.ConvertAll(p => GM.MetersToLatLon(GM.PixelsToMeters(p, Zoom)));
    }

    private Vector2d PixelToRelative(Vector2d pixel)
    {
        return pixel + PATR;
    }

    private Vector2d RelativeToAbsolute(Vector2d pixel)
    {
        return pixel - PATR;
    }

    private List<Vector2d> PixelsToRelative(List<Vector2d> pixels)
    {
        return pixels.ConvertAll(p => PixelToRelative(p));
    }

    private bool Inside(Vector2d pixel, List<Vector2d> polygon)
    {
        var inside = false;
        for (int i = 0; i < polygon.Count - 1; i++)
        {
            if (((polygon[i].y > 0) != (polygon[i + 1].y > 0))
            && ((polygon[i].y > 0) == (polygon[i].y * polygon[i + 1].x > polygon[i + 1].y * polygon[i].x)))
                inside = !inside;
        }

        return inside;
    }
}




