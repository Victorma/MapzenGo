using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MapzenGo.Helpers;
using UniRx;
using UnityEngine;
using MapzenGo.Models.Plugins;
using System;
using MapzenGo.Models.Factories;

namespace MapzenGo.Models
{
    public class VectorDataPlugin : Plugin
    {
        public string RelativeCachePath = "../CachedTileData/{0}/";
        protected string CacheFolderPath;

        protected readonly string _mapzenUrl = "http://tile.mapzen.com/mapzen/vector/v1/{0}/{1}/{2}/{3}.{4}?api_key={5}";
        [SerializeField]
        protected string _key = "vector-tiles-5sBcqh6"; //try getting your own key if this doesn't work
        protected string _mapzenLayers;
        [SerializeField]
        protected Material MapMaterial;
        protected readonly string _mapzenFormat = "json";

        void Start()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            CacheFolderPath = Path.Combine(Application.persistentDataPath, RelativeCachePath);
#else
            CacheFolderPath = Path.Combine(Application.dataPath, RelativeCachePath);
#endif
            CacheFolderPath = CacheFolderPath.Format(Zoom);
            if (!Directory.Exists(CacheFolderPath))
                Directory.CreateDirectory(CacheFolderPath);

            if (MapMaterial == null)
                MapMaterial = Resources.Load<Material>("Ground");

            InitFactories();
            InitLayers();

            var v2 = GM.LatLonToMeters(Latitude, Longitude);
            var tile = GM.MetersToTile(v2, Zoom);

            TileHost = new GameObject("Tiles").transform;
            TileHost.SetParent(transform, false);

            Tiles = new Dictionary<Vector2d, Tile>();
            CenterTms = tile;
            CenterInMercator = GM.TileBounds(CenterTms, Zoom).Center;

            LoadTiles(CenterTms, CenterInMercator);

            var rect = GM.TileBounds(CenterTms, Zoom);
            transform.localScale = Vector3.one * (float)(TileSize / rect.Width);
        }

        private void InitLayers()
        {
            var layers = new List<string>();
            foreach (var plugin in _plugins.OfType<Factory>())
            {
                if (layers.Contains(plugin.XmlTag)) continue;
                layers.Add(plugin.XmlTag);
            }
            _mapzenLayers = string.Join(",", layers.ToArray());
        }

        private void InitFactories()
        {
            _plugins = new List<Plugin>();
            foreach (var plugin in GetComponentsInChildren<Plugin>())
            {
                _plugins.Add(plugin);
            }
        }

        protected override IEnumerator CreateRoutine(Tile tile, Action<bool> finished)
        {
            var url = string.Format(_mapzenUrl, _mapzenLayers, Zoom, tile.TileTms.x, tile.TileTms.y, _mapzenFormat, _key);
            //this is temporary (hopefully), cant just keep adding stuff to filenames
            var tilePath = Path.Combine(CacheFolderPath, _mapzenLayers.Replace(',', '_') + "_" + tile.TileTms.x + "_" + tile.TileTms.y);
            if (File.Exists(tilePath))
            {
                using (var r = new StreamReader(tilePath, Encoding.Default))
                {
                    var mapData = r.ReadToEnd();
                    ConstructTile(mapData, tile);
                }
            }
            else
            {
                ObservableWWW.Get(url).Subscribe(
                    success =>
                    {
                        var sr = File.CreateText(tilePath);
                        sr.Write(success);
                        sr.Close();
                        ConstructTile(success, tile, finished);
                    },
                    error =>
                    {
                        Debug.Log(error);
                        finished(false);
                    });
            }

            yield return null;
        }

        protected void ConstructTile(string text, Tile tile, Action<bool> finished)
        {
            var heavyMethod = Observable.Start(() => new JSONObject(text));

            heavyMethod.ObserveOnMainThread().Subscribe(mapData =>
            {
                if (tile) // checks if tile still exists and haven't destroyed yet
                    tile.Data = mapData;

                finished(true);
            });
        }
    }
}
