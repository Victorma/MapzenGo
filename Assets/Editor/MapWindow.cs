using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using MapzenGo.Helpers.Search;
using MapzenGo.Models.Settings.Editor;
using MapzenGo.Helpers;
using MapzenGo.Helpers.VectorD;

public class MapWindow : EditorWindow {


    const string PATH_SAVE_SCRIPTABLE_OBJECT = "Assets/MapzenGo/Resources/Settings/";



    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Map Window")]
    static void Create()
    {
        // Get existing open window or if none, make a new one:
        MapWindow window = (MapWindow)EditorWindow.GetWindow(typeof(MapWindow));

        window.place = FindObjectOfType<SearchPlace>();
        if(window.place == null)
        {
            SearchPlace search = new GameObject("Searcher").AddComponent<SearchPlace>();
            window.place = search;
        }
        

        window.Init();
        window.Show();
    }

    private SearchPlace place;
    private string address = "";
    private Vector2 location;
    private string lastSearch = "";
    private float timeSinceLastWrite;
    private DropDown addressDropdown;
    private GUIMap map;

    void Init()
    { 
        EditorApplication.update += this.Update;

        place.DataStructure = HelperExtention.GetOrCreateSObjectReturn<StructSearchData>(ref place.DataStructure, PATH_SAVE_SCRIPTABLE_OBJECT);
        place.namePlaceСache = "";
        place.DataStructure.dataChache.Clear();

        addressDropdown = new DropDown("Address");
        map = new GUIMap();
        map.Repaint += Repaint;
        map.Zoom = 19;
    }

    void OnGUI()
    {
        if (addressDropdown == null)
            Init();

        var prevAddress = address;
        address = addressDropdown.LayoutBegin();
        if (address != prevAddress)
        {
            timeSinceLastWrite = 0;
        }

        
        location = EditorGUILayout.Vector2Field("Location", location);
        var lastRect = GUILayoutUtility.GetLastRect();
        if(location != map.Center.ToVector2())
            map.Center = new Vector2d(location.x, location.y);

        map.DrawMap(GUILayoutUtility.GetRect(position.width, position.height - lastRect.y - lastRect.height));
        location = map.Center.ToVector2();

        if (addressDropdown.LayoutEnd())
        {
            lastSearch = address = addressDropdown.Value;
            foreach (var l in place.DataStructure.dataChache)
                if (l.label == address)
                    location = l.coordinates;

            var geometry = new GMLGeometry();
            geometry.Type = GMLGeometry.GeometryType.Polygon;

            var points = 5f;
            var radius = 0.00005;
            for(float i = 0; i<5; i++)
                geometry.Points.Add(new Vector2d(location.x + radius*Mathf.Sin(i*2f*Mathf.PI/points)*1.33333f, location.y + radius * Mathf.Cos(i * 2f * Mathf.PI / points)));


            map.Geometries.Add(geometry);

            place.DataStructure.dataChache.Clear();
            Repaint();
        }
    }

    void Update()
    {
        //Debug.Log(Time.fixedDeltaTime);
        timeSinceLastWrite += Time.fixedDeltaTime;
        if (timeSinceLastWrite > 3f)
        {
            PerformSearch();
        }

        if(place.DataStructure.dataChache.Count > 0)
        {
            var addresses = new List<string>();
            foreach (var r in place.DataStructure.dataChache)
                addresses.Add(r.label);
            addressDropdown.Elements = addresses;
            Repaint();
        }
    }


    private void PerformSearch()
    {
        if (address != null && address.Trim() != "" && lastSearch != address)
        {
            place.namePlace = address;
            place.SearchInMapzen();
            lastSearch = address;
            
        }
    }

    void OnDestroy()
    {
        EditorApplication.update -= this.Update;

    }

}
    

public class GMLGeometry {

    public enum GeometryType { Point, LineString, Polygon }

    public GMLGeometry()
    {
        Points = new List<Vector2d>();
    }

    public GeometryType Type { get; set; }
    public List<Vector2d> Points { get; set; }
}