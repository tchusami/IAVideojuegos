﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveEngine.Common.Math;

namespace WaveProject.Steerings.Coordinated
{
    public class DefensiveCirclePattern : Pattern
    {
        public float CharacterRadius { get; set; }

        // Devuelve el numero de Slots del conjunto, tantos como elemento haya
        public override int CalculateNumberOfSlots(List<SlotAssignment> slotAssignments)
        {
            NumberOfSlots = slotAssignments.Count;
            return NumberOfSlots;
        }

        // Calcula el desplazamiento de las unidades
        public override SlotLocation GetDriftOffset(List<SlotAssignment> slotAssignments)
        {
            var center = new SlotLocation();
            foreach (var assignment in slotAssignments)
            {
                var location = GetSlotLocation(assignment.SlotNumber);
                center.Position += location.Position;
                center.Orientation += location.Orientation;
            }

            var numberOfAssignments = slotAssignments.Count;
            center.Position /= numberOfAssignments;
            center.Orientation /= numberOfAssignments;
            
            return center;
        }

        // Calcula la posición de una unidad
        public override SlotLocation GetSlotLocation(int slotNumber)
        {
            var rn = NumberOfSlots * CharacterRadius / (float)Math.PI;
            var o = ((2 * (float)Math.PI) / NumberOfSlots) * slotNumber;

            var location = new SlotLocation();
            location.Position = rn * new Vector2((float)Math.Cos(o), (float)Math.Sin(o));
            location.Orientation = o;

            return location;
        }

        // Se adapta hasta "el infinito"
        public override bool SupportSlots(int slotCounts)
        {
            return true;
        }
    }
}
