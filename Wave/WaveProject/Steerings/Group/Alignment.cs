﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveEngine.Common.Math;

namespace WaveProject.Steerings.Group
{
    public class Alignment : Steering
    {
        public float Threshold { get; set; }

        public Alignment()
        {
            Threshold = 100;
        }

        public override SteeringOutput GetSteering()
        {
            int count = 0;
            Vector2 heading = new Vector2(0, 0);
            // Enemigos en el Threshold
            var targets = Kinematic.Kinematics.Where(w => (w.Position - Character.Position).Length() <= Threshold && w != Character);
            foreach (var target in targets)
            {
                heading += target.Position + target.Velocity;
                count++;
            }

            if (count == 0)
                return new SteeringOutput();

            heading /= count;

            Align align = new Align();
            VelocityMatching velocityMatching = new VelocityMatching();
            align.Character = velocityMatching.Character = Character;
            align.Target = velocityMatching.Target = new Kinematic() { Position = heading };

            return velocityMatching.GetSteering() + align.GetSteering();
        }
    }
}
