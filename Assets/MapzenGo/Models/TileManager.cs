using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapzenGo.Helpers;
using MapzenGo.Models.Factories;
using MapzenGo.Models.Plugins;
using UniRx;
using UnityEngine;

namespace MapzenGo.Models
{
    public class TileManager : MonoBehaviour
    {

        [SerializeField]
        public float Latitude = 39.921864f;
        [SerializeField]
        public float Longitude = 32.818442f;
        [SerializeField]
        public int Range = 3;
        [SerializeField]
        public int Zoom = 16;
        [SerializeField]
        public float TileSize = 100;

        
        protected Transform TileHost;

        protected Dictionary<Vector2d, Tile> Tiles; //will use this later on
        protected Vector2d CenterTms; //tms tile coordinate
        protected Vector2d CenterInMercator; //this is like distance (meters) in mercator 

        private Queue<Tile> _readyToProcess;

        private List<Plugin> _plugins;

        public virtual void Start()
        {
            _readyToProcess = new Queue<Tile>();
        }

        public virtual void Update()
        {
            
        }



        protected void LoadTiles(Vector2d tms, Vector2d center)
        {
            for (int i = -Range; i <= Range; i++)
            {
                for (int j = -Range; j <= Range; j++)
                {
                    var v = new Vector2d(tms.x + i, tms.y + j);
                    if (Tiles.ContainsKey(v))
                        continue;
                    StartCoroutine(CreateTile(v, center));
                }
            }
        }

        protected virtual IEnumerator CreateTile(Vector2d tileTms, Vector2d centerInMercator)
        {
            var rect = GM.TileBounds(tileTms, Zoom);
            var tile = new GameObject("tile " + tileTms.x + "-" + tileTms.y).AddComponent<Tile>();

            tile.Zoom = Zoom;
            tile.TileTms = tileTms;
            tile.TileCenter = rect.Center;
            tile.Material = MapMaterial;
            tile.Rect = GM.TileBounds(tileTms, Zoom);

            Tiles.Add(tileTms, tile);
            tile.transform.position = (rect.Center - centerInMercator).ToVector3();
            tile.transform.SetParent(TileHost, false);
            ExecutePlugins(tile);
            
            yield return null;
        }

        protected void ExecutePlugins(Tile tile)
        {
            List<Plugin> todo = new List<Plugin>();
            List<Plugin> doing = new List<Plugin>();

            ContinuePlugins(tile, todo, doing);
        }

        private void ContinuePlugins(Tile tile, List<Plugin> todo, List<Plugin> doing)
        {
            if (!tile)
                return;

            foreach (var plugin in todo)
            {
                if (doing.Contains(plugin)) continue;
                // Check dependencies
                if (plugin.Dependencies.All(dependencie => todo.Contains(dependencie)))
                { 
                    ObservableWWW.Get()
                    var pluginLoad = Observable.Start(() => { plugin.Create(tile); return true; });
                    doing.Add(plugin);
                    pluginLoad.ObserveOnMainThread().Subscribe(success =>
                    {
                        if (success)
                        {
                            todo.Remove(plugin);
                            doing.Remove(plugin);

                            ContinuePlugins(tile, todo, doing);
                        }
                    });
                }
            }
        }
    }
}
