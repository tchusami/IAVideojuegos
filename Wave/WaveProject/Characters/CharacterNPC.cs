﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services;
using WaveProject.CharacterTypes;
using WaveProject.DecisionManager;
using WaveProject.Steerings;
using WaveProject.Steerings.Combined;
using WaveProject.Steerings.Delegated;
using WaveProject.Steerings.Pathfinding;

namespace WaveProject.Characters
{
    public class CharacterNPC : Behavior, ICharacterInfo
    {
        private bool disposed = false;
        [RequiredComponent]
        public Transform2D Transform { get; private set; }
        [RequiredComponent]
        public Sprite Texture { get; private set; }
        public Kinematic Kinematic { get; private set; }
        // Color de la textura
        public Color Color { get; set; }
        // Steering actual
        public Steering Steering { get; set; }
        // PathFollowing actual, si lo hay
        public FollowPath PathFollowing { get; set; }
        // Tipo del personaje
        public CharacterType Type { get; private set; }
        // Manejador de acciones del personaje
        public ActionManager ActionManager { get; set; }
        // Texto que muestra la acción que quiere realizar el personaje
        public TextBlock Text { get; set; }
        // Equipo del personaje
        public int Team { get; set; }
        // Indica si la IA está activada
        private bool IsActiveIA = false;

        public CharacterNPC(Kinematic kinematic, EnumeratedCharacterType type, int team/*, Color color*/)
        {
            Kinematic = kinematic;
            Kinematic.MaxVelocity = 30;
            Team = team;
            ActionManager = new ActionManager();
            // En base al tipo construimos la instancia de TypeCharacter
            switch (type)
            {
                case EnumeratedCharacterType.EXPLORER:
                    Type = new ExplorerCharacter(this, EntityManager);
                    break;
                case EnumeratedCharacterType.MELEE:
                    Type = new MeleeCharacter(this, EntityManager);
                    break;
                case EnumeratedCharacterType.RANGED:
                    Type = new RangedCharacter(this, EntityManager);
                    break;
                case EnumeratedCharacterType.NONE:
                    Type = new MeleeCharacter(this, EntityManager);
                    break;
            }

            // En base al equipo se establece un color de la textura
            if (team == 1)
                Color = Color.Cyan;
            if (team == 2)
                Color = Color.Red;
        }

        protected override void Initialize()
        {
            base.Initialize();
            Transform.Origin = Vector2.Center;
            Texture.TintColor = Color;
            Type.EntityManager = EntityManager;
            Kinematic.BRadius = Math.Max(Texture.Texture.Width, Texture.Texture.Height) / 1.5f;
        }

        protected override void Update(TimeSpan gameTime)
        {
            float dt = (float)gameTime.TotalSeconds;
            if (IsActiveIA) // Si la IA está activa pedimos una acción nueva al TypeCharacter
            {
                GenericAction newAction = Type.Update();
                ActionManager.ScheduleAction(newAction);
                string action = ActionManager.Execute(dt);
                if (Text != null) // Si existe el componente texto se escribe la acción
                {
                    var transform = Text.Entity.FindComponent<Transform2D>();
                    transform.Position = Transform.Position.PositionUnproject(CameraController.CurrentCamera);
                    Text.Text = (!string.IsNullOrEmpty(action) ? action : Text.Text);
                }
            }
            else if (Text != null) // Si no hay IA borramos el texto
            {
                Text.Text = "";
            }

            if (Steering == null) // Si no tenemos Steering salimos del método
                return;
            // Actualizamos en base al Steering
            Kinematic.Position = Transform.Position;
            Kinematic.Orientation = Transform.Rotation;

            Terrain terrain = Map.CurrentMap.TerrainOnWorldPosition(Kinematic.Position);
            Kinematic.MaxVelocity = Type.MaxVelocity(terrain);

            SteeringOutput output = Steering.GetSteering();
            Kinematic.Update(dt, output);

            Transform.Position = Kinematic.Position;
            Transform.Rotation = Kinematic.Orientation;

            #region Escenario circular
            if (Transform.Position.X > Map.CurrentMap.TotalWidth)
            {
                Transform.Position -= new Vector2(Map.CurrentMap.TotalWidth, 0);
            }
            else if (Transform.Position.X < 0)
            {
                Transform.Position += new Vector2(Map.CurrentMap.TotalWidth, 0);
            }
            if (Transform.Position.Y > Map.CurrentMap.TotalHeight)
            {
                Transform.Position -= new Vector2(0, Map.CurrentMap.TotalHeight);
            }
            else if (Transform.Position.Y < 0)
            {
                Transform.Position += new Vector2(0, Map.CurrentMap.TotalHeight);
            }
            #endregion
        }

        public Vector2 GetPosition()
        {
            return Kinematic.Position;
        }

        public int GetTeam()
        {
            return Team;
        }

        public EnumeratedCharacterType GetCharacterType()
        {
            return Type.GetCharacterType();
        }

        public Vector2 GetVelocity()
        {
            return Kinematic.Velocity;
        }

        public void SetTarget(Kinematic target)
        {
            Steering.SetTarget(target);
        }
        
        public void SetPathFinding(Vector2 target)
        {
            if (Steering != null)
                Steering.Dispose();
            // Generamos un BlendedSteering para seguir el camino
            BehaviorAndWeight[] behaviors = SteeringsFactory.PathFollowing(Kinematic);
            Steering = new BlendedSteering(behaviors);
            PathFollowing = (FollowPath)behaviors.Select(s => s.Behavior).FirstOrDefault(f => f is FollowPath);

            // Generamos el camino y lo asignamos
            LRTA lrta = new LRTA(Kinematic.Position, target, Type, DistanceAlgorith.CHEVYCHEV) { UseInfluence = true };
            var path = lrta.Execute();
            PathFollowing.SetPath(path);
        }

        public void ReceiveHeal(int hp)
        {
            Type.HP = Math.Min(Type.HP + hp, Type.MaxHP);
        }

        public void ReceiveAttack(int atk)
        {
            float damage = (atk / (float)Type.Def) * 10;
            Type.HP = Math.Max(Type.HP - (int)damage, 0);
        }

        public bool IsDead()
        {
            return Type.HP <= 0;
        }

        public bool IsDisposed()
        {
            return disposed;
        }

        public void SetIA(bool isActiveIA)
        {
            IsActiveIA = isActiveIA;
        }
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (Steering != null)
                    Steering.Dispose();
                if (Text != null)
                {
                    EntityManager.Remove(Text);
                    Text = null;
                }
                Kinematic.Dispose();
                Kinematic = null;
                Steering = null;
                Type = null;
            }

            disposed = true;
        }
    }
}
