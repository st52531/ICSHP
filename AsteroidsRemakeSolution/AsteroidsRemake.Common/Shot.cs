﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AsteroidsRemake.Common
{
    public class Shot : GameObject
    {
        public Point Target { get; set; }
        public double TraveledDistance { get; set; }
        public double MaximumDistance { get; set; }

        public override void Update()
        {
            throw new NotImplementedException();
        }

        public Shot(Point position, int size, double maximumDistance, double motionDirection)
        {
            Position = position;
            Size = size;
            TraveledDistance = 0;
            MaximumDistance = maximumDistance;
            MotionDirection = motionDirection;
        }
    }
}
