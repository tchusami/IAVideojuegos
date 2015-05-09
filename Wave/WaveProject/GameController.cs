﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services;
using WaveProject.Steerings.Pathfinding;

namespace WaveProject
{
    public class GameController : Behavior
    {
        Kinematic Mouse;
        Vector2 LastMousePosition;
        Vector2 LastStartTile;
        Vector2 LastEndTile;

        Button LRTAManhattan;
        Button LRTAChevychev;
        Button LRTAEuclidean;
        DistanceAlgorith CurrentLrtaAlgorithm = DistanceAlgorith.MANHATTAN;
        public DebugLines Debug { get; set; }
        private List<PlayableCharacter> SelectedCharacters;
        private bool MousePressed = false;
        private bool ControlSelect = false;
        private RectangleF MouseRectangle;

        public GameController(Kinematic mouse)
        {
            Mouse = mouse;
            LastMousePosition = mouse.Position;
            LastStartTile = LastEndTile = Vector2.Zero;
            MouseRectangle = RectangleF.Empty;
            SelectedCharacters = new List<PlayableCharacter>();
        }

        protected override void Initialize()
        {
            LRTAManhattan = EntityManager.Find<Button>("LRTA_Manhattan");
            LRTAChevychev = EntityManager.Find<Button>("LRTA_Chevychev");
            LRTAEuclidean = EntityManager.Find<Button>("LRTA_Euclidean");
            
            float width = WaveServices.ViewportManager.ScreenWidth;
            float height = WaveServices.ViewportManager.ScreenHeight;
            LRTAManhattan.Entity.FindComponent<Transform2D>().Position = new Vector2(width - LRTAManhattan.Width - 10, 50);
            LRTAChevychev.Entity.FindComponent<Transform2D>().Position = new Vector2(width - LRTAManhattan.Width - 10, 100);
            LRTAEuclidean.Entity.FindComponent<Transform2D>().Position = new Vector2(width - LRTAManhattan.Width - 10, 150);

            LRTAManhattan.Click += LRTAManhattan_Click;
            LRTAChevychev.Click += LRTAChevychev_Click;
            LRTAEuclidean.Click += LRTAEuclidean_Click;

            LRTAManhattan.IsVisible = false;
            //base.Initialize();
        }

        void LRTAEuclidean_Click(object sender, EventArgs e)
        {
            CurrentLrtaAlgorithm = DistanceAlgorith.EUCLIDEAN;
            LRTAManhattan.IsVisible = true;
            LRTAChevychev.IsVisible = true;
            LRTAEuclidean.IsVisible = false;
        }

        void LRTAChevychev_Click(object sender, EventArgs e)
        {
            CurrentLrtaAlgorithm = DistanceAlgorith.CHEVYCHEV;
            LRTAManhattan.IsVisible = true;
            LRTAChevychev.IsVisible = false;
            LRTAEuclidean.IsVisible = true;
        }

        void LRTAManhattan_Click(object sender, EventArgs e)
        {
            CurrentLrtaAlgorithm = DistanceAlgorith.MANHATTAN;
            LRTAManhattan.IsVisible = false;
            LRTAChevychev.IsVisible = true;
            LRTAEuclidean.IsVisible = true;
        }

        protected override void Update(TimeSpan gameTime)
        {
            Mouse.Update((float)gameTime.TotalMilliseconds, new Steerings.SteeringOutput());

            if ((WaveServices.Input.MouseState.LeftButton == WaveEngine.Common.Input.ButtonState.Release && ControlSelect))
            {
                ControlSelect = false;
            }

            if (WaveServices.Input.MouseState.LeftButton == WaveEngine.Common.Input.ButtonState.Release && MousePressed)
            {
                IEnumerable<PlayableCharacter> characters = EntityManager.AllEntities
                    .Where(w => w.FindComponent<PlayableCharacter>() != null)
                    .Select(s => s.FindComponent<PlayableCharacter>());

                var selectedCharacter = characters
                    .FirstOrDefault(f => Mouse.Position.IsContent(f.Kinematic.Position, new Vector2(f.Texture.Texture.Width, f.Texture.Texture.Height)));

                if (selectedCharacter != null)
                    SelectedCharacters.Add(selectedCharacter);
                MousePressed = false;
                MouseRectangle = RectangleF.Empty;
            }

            if (WaveServices.Input.MouseState.LeftButton == WaveEngine.Common.Input.ButtonState.Pressed && !MousePressed)
            {
                if (WaveServices.Input.KeyboardState.LeftControl == WaveEngine.Common.Input.ButtonState.Pressed && !ControlSelect)
                {
                    ControlSelect = true;
                    IEnumerable<PlayableCharacter> characters = EntityManager.AllEntities
                    .Where(w => w.FindComponent<PlayableCharacter>() != null)
                    .Select(s => s.FindComponent<PlayableCharacter>());

                    var selectedCharacter = characters
                        .FirstOrDefault(f => Mouse.Position.IsContent(f.Kinematic.Position, new Vector2(f.Texture.Texture.Width, f.Texture.Texture.Height)));

                    if (selectedCharacter != null)
                    {
                        if (SelectedCharacters.Contains(selectedCharacter))
                            SelectedCharacters.Remove(selectedCharacter);
                        else 
                            SelectedCharacters.Add(selectedCharacter);
                    }
                }
                else if (!ControlSelect)
                {
                    MousePressed = true;
                    SelectedCharacters.Clear();
                    MouseRectangle.X = Mouse.Position.X;
                    MouseRectangle.Y = Mouse.Position.Y;
                }
            }

            if (WaveServices.Input.MouseState.LeftButton == WaveEngine.Common.Input.ButtonState.Pressed && MousePressed)
            {
                MouseRectangle.Width = Mouse.Position.X - MouseRectangle.X;
                MouseRectangle.Height = Mouse.Position.Y - MouseRectangle.Y;
                IEnumerable<PlayableCharacter> characters = EntityManager.AllEntities
                    .Where(w => w.FindComponent<PlayableCharacter>() != null)
                    .Select(s => s.FindComponent<PlayableCharacter>());

                SelectedCharacters = characters.Where(w => w.Kinematic.Position.IsContent(MouseRectangle.Center, new Vector2(MouseRectangle.Width.Abs(), MouseRectangle.Height.Abs()))).ToList();
            }

            if (WaveServices.Input.MouseState.RightButton == WaveEngine.Common.Input.ButtonState.Pressed && Map.CurrentMap.PositionInMap(Mouse.Position))
            {
                foreach (var selectedCharacter in SelectedCharacters)
                {
                    LRTA lrta = new LRTA(selectedCharacter.Kinematic.Position, Mouse.Position, selectedCharacter.Type, CurrentLrtaAlgorithm);
                    if (LastStartTile != lrta.StartPos || LastEndTile != lrta.EndPos)
                    {
                        LastStartTile = lrta.StartPos;
                        LastEndTile = lrta.EndPos;
                        List<Vector2> path = lrta.Execute();
                        selectedCharacter.SetPath(path);
                        Debug.Path = path;
                    }
                }
            }
            LastMousePosition = Mouse.Position;
            Debug.Controller = this;
        }

        public void Draw(LineBatch2D lb)
        {
            lb.DrawRectangleVM(MouseRectangle, Color.Green, 1f);

            foreach (var selectedCharacter in SelectedCharacters)
            {
                selectedCharacter.Draw(lb);
            }
        }
    }

   
}
