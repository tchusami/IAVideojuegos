﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveProject.CharacterTypes;
using WaveProject.Steerings;
using WaveProject.Steerings.Combined;
using WaveProject.Steerings.Delegated;
using WaveProject.Steerings.Pathfinding;

namespace WaveProject
{
    public class PlayableCharacter : Behavior, ICharacterInfo
    {
        private bool disposed = false;
        [RequiredComponent]
        public Transform2D Transform { get; private set; }
        [RequiredComponent]
        public Sprite Texture { get; private set; }
        public Kinematic Kinematic { get; private set; }
        public Color Color { get; set; }
        public Steering Steering { get; set; }
        public FollowPath PathFollowing { get; set; }
        public CharacterType Type { get; private set; }
        public int Team { get; set; }
        private ICharacterInfo Target = null;
        public const float ExecutionTime = 0.3f;
        private float CurrentTime = 0f;

        public PlayableCharacter(Kinematic kinematic, EnumeratedCharacterType type, int team)
        {
            Kinematic = kinematic;
            Kinematic.MaxVelocity = 30;
            Team = team;

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

            //Steering = new PredictivePathFollowing(true) { Character = Kinematic, PredictTime = 0.3f };
            ////Steering = new FollowPath() { Character = Kinematic };
            //PathFollowing = (PredictivePathFollowing)Steering;

            if (Team == 1)
                Color = Color.Cyan;
            else if (Team == 2)
                Color = Color.Red;

            SetPathFollowing();
        }

        protected override void Initialize()
        {
            base.Initialize();
            Transform.Origin = Vector2.Center;
            Texture.TintColor = Color;
            Type.EntityManager = EntityManager;
        }

        public void SetPathFollowing()
        {
            if (Steering != null)
                Steering.Dispose();
            BehaviorAndWeight[] behaviors = SteeringsFactory.PathFollowing(Kinematic);
            Steering = new BlendedSteering(behaviors);
            PathFollowing = (FollowPath)behaviors.Select(s => s.Behavior).FirstOrDefault(f => f is FollowPath);
        }

        public void SetPath(List<Vector2> path)
        {
            PathFollowing.SetPath(path);
        }

        public void Draw(LineBatch2D lb)
        {
            lb.DrawCircleVM(Kinematic.Position, Math.Max(Texture.Texture.Width, Texture.Texture.Height) / 1.8f, Color.Cyan, 1f);
        }

        protected override void Update(TimeSpan gameTime)
        {
            float dt = (float)gameTime.TotalSeconds;
            CurrentTime += dt;
            if (CurrentTime >= ExecutionTime)
            {
                if (Target != null)
                {
                    if (Target.IsDisposed())
                        Target = null;
                    else
                        Type.Attack(Target);
                }
                CurrentTime = 0f;
            }

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
                Kinematic.Dispose();
                Kinematic = null;
                Steering = null;
                PathFollowing = null;
                Type = null;
            }

            disposed = true;
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
            Steering.Dispose();
            BehaviorAndWeight[] behaviors = SteeringsFactory.CollisionPrevent(Kinematic);
            List<BehaviorAndWeight> allBehaviors = new List<BehaviorAndWeight>(behaviors);
            allBehaviors.Add(new BehaviorAndWeight()
            {
                Behavior = Steering = new Arrive() { Character = Kinematic, Target = target },
                Weight = 1.0f
            });
            Steering = new BlendedSteering(allBehaviors.ToArray());
            //Steering.SetTarget(target);
        }


        public void SetPathFinding(Vector2 target)
        {
            if (Steering != null)
                Steering.Dispose();
            BehaviorAndWeight[] behaviors = SteeringsFactory.PathFollowing(Kinematic);
            Steering = new BlendedSteering(behaviors);
            PathFollowing = (FollowPath)behaviors.Select(s => s.Behavior).FirstOrDefault(f => f is FollowPath);

            LRTA lrta = new LRTA(Kinematic.Position, target, Type, DistanceAlgorith.CHEVYCHEV);
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
            Console.WriteLine(Type.HP);
        }

        public void Attack(ICharacterInfo target)
        {
            Target = target;
            //Type.Attack(target);
        }

        public bool IsDead()
        {
            return Type.HP <= 0;
        }

        public bool IsDisposed()
        {
            return disposed;
        }
    }
}
