﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AsteroidsRemake.Common
{
    public class Asteroid : GameObject
    {
        public Asteroid(Point position, int size, double velocityM, double motionDirection)
        {
            Position = position;
            Size = size;
            VelocityMultiplier = velocityM;
            MotionDirection = motionDirection;
        }
        public Asteroid(int size, double velocityM, double motionDirection)
        {
            Size = size;
            VelocityMultiplier = velocityM;
            MotionDirection = motionDirection;
        }
    }
}
