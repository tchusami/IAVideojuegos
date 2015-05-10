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

            LRTAManhattan.Click += (s, e) =>
            {
                CurrentLrtaAlgorithm = DistanceAlgorith.MANHATTAN;
                LRTAManhattan.IsVisible = false;
                LRTAChevychev.IsVisible = true;
                LRTAEuclidean.IsVisible = true;
            };

            LRTAChevychev.Click += (s, e) =>
            {
                CurrentLrtaAlgorithm = DistanceAlgorith.CHEVYCHEV;
                LRTAManhattan.IsVisible = true;
                LRTAChevychev.IsVisible = false;
                LRTAEuclidean.IsVisible = true;
            };

            LRTAEuclidean.Click += (s, e) =>
            {
                CurrentLrtaAlgorithm = DistanceAlgorith.EUCLIDEAN;
                LRTAManhattan.IsVisible = true;
                LRTAChevychev.IsVisible = true;
                LRTAEuclidean.IsVisible = false;
            };

            LRTAManhattan.IsVisible = false;
            //base.Initialize();
        }

        protected override void Update(TimeSpan gameTime)
        {
            Mouse.Update((float)gameTime.TotalMilliseconds, new Steerings.SteeringOutput());

            // Si el botón izquierdo del ratón no está pultado y la tecla estaba pulsada
            if ((WaveServices.Input.MouseState.LeftButton == WaveEngine.Common.Input.ButtonState.Release && ControlSelect))
            {
                ControlSelect = false;
            }

            // Si está suelto el botón izquierdo del raton y antes estaba pulsado
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

            // Si está pulsado el botón izquierdo del ratón y antes no estaba pulsado
            if (WaveServices.Input.MouseState.LeftButton == WaveEngine.Common.Input.ButtonState.Pressed && !MousePressed)
            {
                // Si está pulsada la tecla control y antes no lo estaba
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
                // Si no está, ni estaba pulsada la tecla control
                else if (!ControlSelect)
                {
                    MousePressed = true;
                    SelectedCharacters.Clear();
                    MouseRectangle.X = Mouse.Position.X;
                    MouseRectangle.Y = Mouse.Position.Y;
                }
            }

            // Si está pulsado el botón izquierdo del ratón y estaba pulsado antes
            if (WaveServices.Input.MouseState.LeftButton == WaveEngine.Common.Input.ButtonState.Pressed && MousePressed)
            {
                MouseRectangle.Width = Mouse.Position.X - MouseRectangle.X;
                MouseRectangle.Height = Mouse.Position.Y - MouseRectangle.Y;
                IEnumerable<PlayableCharacter> characters = EntityManager.AllEntities
                    .Where(w => w.FindComponent<PlayableCharacter>() != null)
                    .Select(s => s.FindComponent<PlayableCharacter>());

                SelectedCharacters = characters.Where(w => w.Kinematic.Position.IsContent(MouseRectangle.Center, new Vector2(MouseRectangle.Width.Abs(), MouseRectangle.Height.Abs()))).ToList();
            }

            // Si está pulsado el botón derecho del ratón y está en una posición valida del mapa
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

            // R para eliminar personajes
            if (WaveServices.Input.KeyboardState.R == WaveEngine.Common.Input.ButtonState.Pressed && SelectedCharacters.Count > 0)
            {
                foreach (var character in SelectedCharacters.ToArray())
                {
                    var entity = EntityManager.AllEntities.FirstOrDefault(w => w.FindComponent<PlayableCharacter>() != null && w.FindComponent<PlayableCharacter>() == character);
                    if (entity != null)
                        EntityManager.Remove(entity);
                    SelectedCharacters.Remove(character);
                    character.Dispose();
                }
            }
            Console.WriteLine(Kinematic.Kinematics.Count);
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
