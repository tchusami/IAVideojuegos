﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveProject.Steerings;
using WaveProject.Steerings.Delegated;
using WaveProject.Steerings.Group;

namespace WaveProject.Steerings.Combined
{
    // Estructura que contiene un Steering y su peso en un grupo
    public struct BehaviorAndWeight
    {
        public Steering Behavior { get; set; }
        public float Weight { get; set; }

    };

    // Factoria de grupos de Steerings y Grupos de Prioridad
    public static class SteeringsFactory
    {
        // Devuelve el grupo de Steerings que forma un Flocking
        public static BehaviorAndWeight[] Flocking(Kinematic character)
        {
            BehaviorAndWeight[] behaviors = new BehaviorAndWeight[4];

            behaviors[0] = new BehaviorAndWeight() { Behavior = new Separation() { Character = character }, Weight = 100f };
            behaviors[1] = new BehaviorAndWeight() { Behavior = new Cohesion() { Character = character }, Weight = 0.4f };
            behaviors[2] = new BehaviorAndWeight() { Behavior = new Alignment() { Character = character }, Weight = 1f };
            behaviors[3] = new BehaviorAndWeight() { Behavior = new Wander() { Character = character }, Weight = 3f };

            return behaviors;
        }

        // Devuelve el grupo de Steerings para seguir a un lider
        public static BehaviorAndWeight[] LeaderFollowing(Kinematic character, Kinematic leader)
        {
            BehaviorAndWeight[] behaviors = new BehaviorAndWeight[3];

            behaviors[0] = new BehaviorAndWeight() { Behavior = new Arrive() { Character = character, Target = leader }, Weight = 0.2f };
            behaviors[1] = new BehaviorAndWeight() { Behavior = new Separation() { Character = character, Threshold = 60 }, Weight = 40f };
            behaviors[2] = new BehaviorAndWeight() { Behavior = new Evade() { Character = character, Target = leader }, Weight = 0.05f };

            return behaviors;
        }

        // Grupo de prevención de colisiones
        public static BehaviorAndWeight[] CollisionPrevent(Kinematic character)
        {
            BehaviorAndWeight[] behaviors = new BehaviorAndWeight[3];

            behaviors[0] = new BehaviorAndWeight() { Behavior = new CollisionAvoidance() { Character = character }, Weight = 0.6f };
            behaviors[1] = new BehaviorAndWeight() { Behavior = new WallAvoidance() { Character = character, LookAhead = 30f }, Weight = 1.2f };
            behaviors[2] = new BehaviorAndWeight() { Behavior = new CollisionAvoidanceRT() { Character = character }, Weight = 0.9f };

            return behaviors;
        }

        // Simplemente implementa un Separation
        public static BehaviorAndWeight[] Separation(Kinematic character)
        {
            BehaviorAndWeight[] behaviors = new BehaviorAndWeight[1];

            behaviors[0] = new BehaviorAndWeight() { Behavior = new Separation() { Character = character, Threshold = 80 }, Weight = 30f };

            return behaviors;
        }

        // Simplemente implementa un Pursue
        public static BehaviorAndWeight[] Pursue(Kinematic character, Kinematic target)
        {
            BehaviorAndWeight[] behaviors = new BehaviorAndWeight[1];

            behaviors[0] = new BehaviorAndWeight() { Behavior = new Persue() { Character = character, Target = target }, Weight = 1f };

            return behaviors;
        }

        // Grupo de seguimiento de caminos, evita todo tipo de colisiones en la medida de los posible,
        // y mantiene la distancia con otros personajes
        public static BehaviorAndWeight[] PathFollowing(Kinematic character)
        {
            BehaviorAndWeight[] behaviors = new BehaviorAndWeight[5];

            behaviors[0] = new BehaviorAndWeight() { Behavior = new CollisionAvoidance() { Character = character }, Weight = 0.4f };
            behaviors[1] = new BehaviorAndWeight() { Behavior = new WallAvoidance() { Character = character, LookAhead = 30f }, Weight = 0.7f };
            behaviors[2] = new BehaviorAndWeight() { Behavior = new CollisionAvoidanceRT(true) { Character = character, Radius = 40f }, Weight = 1.7f };
            behaviors[3] = new BehaviorAndWeight() { Behavior = new Separation() { Character = character, Threshold = 20f }, Weight = 40f };
            behaviors[4] = new BehaviorAndWeight() { Behavior = new FollowPath(true) { Character = character }, Weight = 1.0f };

            return behaviors;
        }

        // Grupo de prioridad de prueba, evista colisiones, mantiene la separación y persigue
        public static BlendedSteering[] PriorityGroup(Kinematic character, Kinematic target)
        {
            BlendedSteering[] steerings = new BlendedSteering[3];

            steerings[0] = new BlendedSteering(CollisionPrevent(character));
            steerings[1] = new BlendedSteering(Separation(character));
            steerings[2] = new BlendedSteering(Pursue(character, target));

            return steerings;
        }
    }
}
