using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using MapzenGo.Helpers.Search;
using MapzenGo.Models.Settings.Editor;

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

        window.tilecache = new Dictionary<Tile, Texture2D>();

        window.Init();
        window.Show();
    }

    private SearchPlace place;
    private string address = "";
    private Vector2 location;
    private string lastSearch = "";
    private float timeSinceLastWrite;
    private DropDown addressDropdown;

    void Init()
    { 
        EditorApplication.update += this.Update;

        place.DataStructure = HelperExtention.GetOrCreateSObjectReturn<StructSearchData>(ref place.DataStructure, PATH_SAVE_SCRIPTABLE_OBJECT);
        place.namePlaceСache = "";
        place.DataStructure.dataChache.Clear();

        addressDropdown = new DropDown("Address");
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

        if (addressDropdown.LayoutEnd())
        {
            lastSearch = address = addressDropdown.Value;
            foreach (var l in place.DataStructure.dataChache)
                if (l.label == address)
                    location = l.coordinates;

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
    