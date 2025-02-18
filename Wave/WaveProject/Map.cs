﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Framework.Graphics;
using WaveEngine.TiledMap;
using WaveProject.Characters;
using WaveProject.CharacterTypes;
using WaveProject.Steerings.Pathfinding;

namespace WaveProject
{
    public struct HealPoint
    {
        public Vector2 Position { get; set; }
        public int Team { get; set; }
    }

    public class Map
    {
        // Singleton
        private static Map instance = new Map();
        public static Map CurrentMap { get { return instance; } }

        // Mapa hecho con Tiled
        public TiledMap TiledMap { get; private set; }
        // Mapa de nodos (pathfinding)
        public Node[,] NodeMap { get; private set; }
        // Mapa de nodos de influencia
        public InfluenceNode[,] InfluenceMap { get; set; }

        public const int HealRatio = 10; // Radio de curación

        // Lista de HealPoints (bases)
        public List<HealPoint> HealPoints { get; private set; }
        // Lista de Waypoints
        public List<Vector2> Waypoints { get; private set; }

        public int Width { get { return TiledMap.Width; } }
        public int Height { get { return TiledMap.Height; } }

        public int TotalWidth { get { return TiledMap.Width(); } }
        public int TotalHeight { get { return TiledMap.Height(); } }

        public int TileWidth { get { return TiledMap.TileWidth; } }
        public int TileHeight { get { return TiledMap.TileHeight; } }

        public void Initialize(TiledMap map)
        {
            HealPoints = new List<HealPoint>();
            Waypoints = new List<Vector2>();

            // Establece el mapa de Tiled
            TiledMap = map;

            // Carga los Waypoints
            LoadWaypoints();

            #region Node Map Base
            // Esto es para que los valores true o false recuperados de los tiles,
            // se formateen siempre como True o False.
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;

            NodeMap = new Node[TiledMap.Width, TiledMap.Height];
            InfluenceMap = new InfluenceNode[TiledMap.Width, TiledMap.Height];

            // Obtenemos la posición del tile objetivo para generar los valores heuristicos iniciales
            var layer = TiledMap.TileLayers.First().Value;

            // Obtenemos todos los tiles del mapa
            var tiles = layer.Tiles;
            foreach (var tile in tiles)
            {
                try
                {
                    // Generamos un nodo con las posiciones del Tile
                    Node node = new Node();
                    node.X = tile.X;
                    node.Y = tile.Y;
                    node.Temp = 0f;

                    // Obtenemos el tipo de terreno
                    string terrain = tile.TilesetTile.Properties["terrain"];
                    node.Terrain = (Terrain)System.Enum.Parse(typeof(Terrain), terrain, true);

                    // Obtenemos el valor que indica si un Tile es pasable
                    string passable = tile.TilesetTile.Properties["passable"];
                    node.Passable = bool.Parse(textInfo.ToTitleCase(passable));

                    // Obtenemos el valor que indica si un punto de curación
                    string heal = tile.TilesetTile.Properties["heal"];
                    if (bool.Parse(textInfo.ToTitleCase(heal)))
                    {
                        // Obtenemos el bando del punto de curación
                        string team = tile.TilesetTile.Properties["team"];
                        int t = int.Parse(team);
                        HealPoints.Add(new HealPoint() { Position = new Vector2(tile.X, tile.Y), Team = t });
                    }

                    // Obtenemos el valor que indica si un punto de curación
                    string obstacle = tile.TilesetTile.Properties["obstacle"];
                    if (bool.Parse(textInfo.ToTitleCase(obstacle)))
                    {
                        new Obstacle(WorldPositionByTilePosition(node.Position) + new Vector2(TileWidth / 2f, TileHeight / 2f), Math.Max(TileWidth, TileHeight) / 1.5f, true);
                    }

                    // Guardamos el nodo en la matriz
                    NodeMap[node.X, node.Y] = node;
                    InfluenceMap[node.X, node.Y] = new InfluenceNode() { Team1 = 0, Team2 = 0 };
                }
                catch (Exception)
                {
                    Console.WriteLine("Property or tile could not found.");
                }
            }
            #endregion

            // Carga todas las capas de objeto del mapa como Muros
            LoadWalls();
        }

        private void LoadWalls()
        {
            if (TiledMap.ObjectLayers.Count > 0)
            {
                foreach (var layer1 in TiledMap.ObjectLayers)
                {
                    foreach (var wall in layer1.Value.Objects)
                    {
                        new Wall(wall.X, wall.Y, wall.Width, wall.Height, true);
                    }
                }
            }
        }

        private void LoadWaypoints()
        {
            string[] lines = File.ReadAllLines(@"Content\waypoints.txt");
            foreach (var line in lines)
            {
                string[] waypoint = line.Split(',');
                int x, y;
                x = int.Parse(waypoint[0]);
                y = int.Parse(waypoint[1]);
                Waypoints.Add(new Vector2(x, y));
            }
        }

        // Transforma una coordenada del mundo, a coordenada de tile
        public Vector2 TilePositionByWolrdPosition(Vector2 position)
        {
            var layer = TiledMap.TileLayers.First().Value;
            try
            {
                Vector2 endTile = layer.GetLayerTileByWorldPosition(position).Position();
                return endTile;
            }
            catch (Exception)
            {
                return new Vector2(-1, -1);
            }
        }

        // Transforma una coordenada de tile a coordenada del mundo
        public Vector2 WorldPositionByTilePosition(Vector2 position)
        {
            return position * new Vector2(TiledMap.TileWidth, TiledMap.TileHeight);
        }

        // Obtiene un tile a traves de su posición en el mundo
        public LayerTile TileByWolrdPosition(Vector2 position)
        {
            var layer = TiledMap.TileLayers.First().Value;
            try
            {
                return layer.GetLayerTileByWorldPosition(position);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Obtiene un terreno a través de su posición en el mundo
        public Terrain TerrainOnWorldPosition(Vector2 position)
        {
            Vector2 tilePosition = TilePositionByWolrdPosition(position);
            return NodeMap[tilePosition.X(), tilePosition.Y()].Terrain;
        }

        // Indica si una posición existe en el mapa
        public bool PositionInMap(Vector2 position)
        {
            return TiledMap.PositionInMap(position);
        }

        // Obtiene el HealPoint mas prometedor
        public Vector2 GetBestHealPoinPosition(ICharacterInfo character)
        {
            var hp = HealPoints.Where(w => w.Team == character.GetTeam()).Select(s => s.Position)
                .OrderBy(o => (WorldPositionByTilePosition(o) - character.GetPosition()).Length())
                .First();
            return hp;
        }

        // Indica si un personaje está en el área de curación
        public bool IsInHealArea(ICharacterInfo character)
        {
            var hp = HealPoints.Where(w => w.Team == character.GetTeam())
                .Any(a => (TilePositionByWolrdPosition(character.GetPosition()) - a.Position).Length() <= new Vector2(HealRatio, HealRatio).Length());
            return hp;
        }

        // Indica si un personaje está en un waypoint
        public bool IsInWaypoint(Vector2 position)
        {
            var pos = TilePositionByWolrdPosition(position);
            return Waypoints.Any(a => a == pos);
        }

        // Indica si un personaje está en la base de un equipo
        public bool IsInBase(Vector2 position, int team)
        {
            var hp = HealPoints.Where(w => w.Team == team)
                .Any(a => (TilePositionByWolrdPosition(position) - a.Position).Length() <= new Vector2(2f, 2f).Length());
            return hp;
        }

        // Indica si existe una posición en el mapa
        public bool Exists(Vector2 position)
        {
            if (position.X() < 0 || position.Y() < 0 || position.X() >= Width || position.Y() >= Height)
                return false;
            return true;
        }

        // Dibuja información de debug
        public void Draw(LineBatch2D lb)
        {
            foreach (var waypoint in Waypoints)
            {
                lb.DrawCircleVM(WorldPositionByTilePosition(waypoint), 5f, Color.White, 2f);
            }
            foreach (var healpoint in HealPoints)
            {
                Vector2 src = WorldPositionByTilePosition(healpoint.Position - new Vector2(HealRatio, HealRatio));
                Vector2 dst = WorldPositionByTilePosition(new Vector2(HealRatio, HealRatio) * 2);
                if (healpoint.Team == 1)
                    lb.DrawRectangleVM(new RectangleF(src.X, src.Y, dst.X, dst.Y), Color.Cyan, 1f);
                else
                    lb.DrawRectangleVM(new RectangleF(src.X, src.Y, dst.X, dst.Y), Color.Red, 1f);
            }
        }
    }
}
