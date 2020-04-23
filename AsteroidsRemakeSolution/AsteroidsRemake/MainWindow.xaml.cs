﻿using AsteroidsRemake.Common;
using AsteroidsRemake.MathLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AsteroidsRemake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer mainTimer;
        private DispatcherTimer hyperDriveTimer;
        private DispatcherTimer gunLoadedTimer;
        private DispatcherTimer enemySpawnTimer;
        private DispatcherTimer AIShootTimer;
        private Storyboard storyboard;

        private List<GameObject> gameObjects = new List<GameObject>();
        private PlayerShip player;
        private Dictionary<GameObject, Shape> gameObjectDictionary = new Dictionary<GameObject, Shape>();
        private bool IsAccelerating { get; set; }

        double screenWidth;
        double screenHeight;

        public MainWindow()
        {
            InitializeComponent();
            screenWidth = BackgroundCanvas.Width;
            screenHeight = BackgroundCanvas.Height;
            InitializeTimers();
            DrawScene();
            CreateEnemyShip();
        }

        private void InitializeTimers()
        {
            mainTimer = new DispatcherTimer(DispatcherPriority.Render);
            mainTimer.Tick += new EventHandler(MainTimer_Tick);
            mainTimer.Interval = TimeSpan.FromSeconds(0.02);
            mainTimer.Start();

            hyperDriveTimer = new DispatcherTimer();
            hyperDriveTimer.Tick += new EventHandler(ActivateHyperDrive_Tick);
            hyperDriveTimer.Interval = TimeSpan.FromSeconds(1);
            hyperDriveTimer.Start();

            gunLoadedTimer = new DispatcherTimer();
            gunLoadedTimer.Tick += new EventHandler(LoadGun_Tick);
            gunLoadedTimer.Interval = TimeSpan.FromSeconds(0.15);
            gunLoadedTimer.Start();

            enemySpawnTimer = new DispatcherTimer();
            enemySpawnTimer.Tick += new EventHandler(EnemySpawn_Tick);
            enemySpawnTimer.Interval = TimeSpan.FromSeconds(20);
            enemySpawnTimer.Start();

            AIShootTimer = new DispatcherTimer();
            AIShootTimer.Tick += new EventHandler(AIShoot_Tick);
            AIShootTimer.Interval = TimeSpan.FromSeconds(2);
            AIShootTimer.Start();
        }
        private void MainTimer_Tick(object sender, EventArgs e)
        {
            ManageVelocity(); // increase or decrease the ship velocity through the time
            AccelerateShip();
            MoveAsteroids();
            MoveEnemyShips();

            CreateNoEdgeScreen();
            Shoot();
            ResolveCollisions();
            RemoveCompletedObjects();
        }

        private bool HyperDriveActive { get; set; }
        private void ActivateHyperDrive_Tick(object sender, EventArgs e)
        {
            HyperDriveActive = true;
        }

        private bool GunLoaded { get; set; }
        private void LoadGun_Tick(object sender, EventArgs e)
        {
            GunLoaded = true;
        }

        private void EnemySpawn_Tick(object sender, EventArgs e)
        {
            CreateEnemyShip();
        }

        private void AIShoot_Tick(object sender, EventArgs e)
        {
            if (gameObjects.Count > 0)
                foreach (var item in gameObjects)
                {
                    if (item is EnemyShip enemy)
                    {
                        PrepareShot(enemy, enemy.Size, MathClass.GetRandomDouble(0, 359));

                        if (counter % 4 == 0) // Get called every 4 seconds
                            // Enemy motion direction changed by value from interval <-60;60>
                            enemy.MotionDirection += MathClass.GetRandomDouble(-60, 60);
                    }
                }
            counter++;
        }

        private int counter;

        private void DrawScene()
        {
            storyboard = new Storyboard();
            CreatePlayerShip();
            MakePlayerInvulnerable();
            CreateNewSetOfAsteroids();
        }

        private void CreatePlayerShip()
        {
            player = new PlayerShip(new Point(637, 325.5), 40, 3);
            gameObjects.Add(player);
            gameObjectDictionary.Add(player, playerPolygon);

            playerPolygon.Fill = CreateNewColorBrush(138, 148, 255);
            playerPolygon.Stroke = CreateNewColorBrush(70, 69, 80);
            playerPolygon.StrokeThickness = 1;
        }

        private int asteroidCount = 4;
        private readonly int defaultAsteroidSize = 250;
        private void CreateNewSetOfAsteroids()
        {
            for (int i = 0; i < asteroidCount; i++)
            {
                bool hasCollision;
                Asteroid asteroid = new Asteroid(defaultAsteroidSize, 0.75, MathClass.GetRandomDouble(0, 359));
                gameObjects.Add(asteroid);
                do
                {
                    Point position = GenerateObjectPosition(asteroid.Size);
                    asteroid.Position = position;

                    hasCollision = FindCollisionWithOtherObjects(asteroid, out GameObject g);

                } while (hasCollision);

                RenderAsteroid(asteroid);
            }
            asteroidCount++;
        }

        private void CreateAsteroidFragments(Asteroid parent, double speedIncrease)
        {
            for (int i = 0; i < 2; i++)
            {
                Asteroid asteroid = new Asteroid(parent.Position, parent.Size / 2, parent.VelocityMultiplier * speedIncrease, MathClass.GetRandomDouble(0, 359));
                gameObjects.Add(asteroid);
                RenderAsteroid(asteroid);
            }
        }

        private void RenderAsteroid(Asteroid asteroid)
        {
            Ellipse el = new Ellipse
            {
                Width = asteroid.Size,
                Height = asteroid.Size,
                Fill = CreateNewColorBrush(191, 165, 164),
                Stroke = CreateNewColorBrush(70, 69, 80),
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            // The substraction by the (asteroid.Size/2) is used so the circle is drawn from its center 
            Canvas.SetLeft(el, asteroid.Position.X - asteroid.Size / 2);
            Canvas.SetBottom(el, asteroid.Position.Y - asteroid.Size / 2);
            BackgroundCanvas.Children.Add(el);
            gameObjectDictionary.Add(asteroid, el);
        }

        private void MoveAsteroids()
        {
            if (gameObjectDictionary.Count > 0)
            {
                foreach (var item in gameObjectDictionary)
                {
                    if (item.Key is Asteroid asteroid)
                    {
                        // The substraction by the (asteroid.Size/2) is used so the circle is drawn from its center 
                        Canvas.SetLeft(item.Value, asteroid.Position.X - asteroid.Size / 2);
                        Canvas.SetBottom(item.Value, asteroid.Position.Y - asteroid.Size / 2);

                        asteroid.Position = MathClass.MovePointByGivenDistanceAndAngle(asteroid.Position, asteroid.VelocityMultiplier, asteroid.MotionDirection);
                    }
                }
            }
        }

        private void CreateEnemyShip()
        {
            double rndMovementDir = MathClass.GetRandomDouble(0, 359);
            EnemyShip enemy = new EnemyShip(40, 1, rndMovementDir);

            Point position = GenerateObjectPosition(enemy.Size);
            // will choose side (based on enemy movement direction) from which the enemy will occur
            if (enemy.MotionDirection > 315 || enemy.MotionDirection <= 45)
                position.Y = screenHeight + enemy.Size / 2;
            else if (enemy.MotionDirection > 45 || enemy.MotionDirection <= 135)
                position.X = 0 - enemy.Size / 2;
            else if (enemy.MotionDirection > 135 || enemy.MotionDirection <= 225)
                position.Y = 0 - enemy.Size / 2;
            else if (enemy.MotionDirection > 225 || enemy.MotionDirection <= 315)
                position.X = screenWidth + enemy.Size / 2;

            enemy.Position = position;

            Rectangle rec = new Rectangle
            {
                Width = enemy.Size,
                Height = enemy.Size,
                Fill = CreateNewColorBrush(253, 112, 118),
                Stroke = CreateNewColorBrush(70, 69, 80),
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            #region Add rotation
            DoubleAnimation da = new DoubleAnimation();
            da.From = 0;
            da.To = 360;
            da.Duration = new Duration(TimeSpan.FromSeconds(1.5));
            da.RepeatBehavior = RepeatBehavior.Forever;
            RotateTransform rt = new RotateTransform(0, enemy.Size / 2, enemy.Size / 2);
            rec.RenderTransform = rt;
            rt.BeginAnimation(RotateTransform.AngleProperty, da);
            #endregion

            // The substraction by the (asteroid.Size/2) is used so the circle is drawn from its center 
            Canvas.SetLeft(rec, enemy.Position.X - enemy.Size / 2);
            Canvas.SetBottom(rec, enemy.Position.Y - enemy.Size / 2);
            BackgroundCanvas.Children.Add(rec);
            gameObjectDictionary.Add(enemy, rec);
            gameObjects.Add(enemy);
        }

        private void MoveEnemyShips()
        {
            if (gameObjectDictionary.Count > 0)
            {
                foreach (var item in gameObjectDictionary)
                {
                    if (item.Key is EnemyShip enemy)
                    {
                        // The substraction by the (asteroid.Size/2) is used so the circle is drawn from its center 
                        Canvas.SetLeft(item.Value, enemy.Position.X - enemy.Size / 2);
                        Canvas.SetBottom(item.Value, enemy.Position.Y - enemy.Size / 2);

                        enemy.Position = MathClass.MovePointByGivenDistanceAndAngle(enemy.Position, enemy.VelocityMultiplier, enemy.MotionDirection);
                        // will make sure that the enemy ship will vanish after it will leave the screen for the second time (and not the first)
                        if (CheckIfPositionIsInsideScreen(enemy.Position))
                            enemy.CanVanish = true;
                    }
                }
            }
        }

        private void CreateNoEdgeScreen()
        {
            double minScreenW, maxScreenW, minScreenH, maxScreenH;
            foreach (var item in gameObjectDictionary)
            {
                GameObject gameObject = item.Key;
                minScreenW = 0 - gameObject.Size;
                maxScreenW = screenWidth + gameObject.Size;
                minScreenH = 0 - gameObject.Size;
                maxScreenH = screenHeight + gameObject.Size;

                // Check a collision with a window edge
                if (gameObject.Position.X > maxScreenW || gameObject.Position.X < minScreenW
                    || gameObject.Position.Y > maxScreenH || gameObject.Position.Y < minScreenH)
                {
                    if (gameObject is EnemyShip enemy && enemy.CanVanish)
                    {
                        enemy.HadCollision = true;
                    }

                    if (gameObject.Position.X > maxScreenW)
                    {
                        gameObject.Position = new Point(minScreenW, gameObject.Position.Y);
                    }
                    else if (gameObject.Position.X < minScreenW)
                    {
                        gameObject.Position = new Point(maxScreenW, gameObject.Position.Y);
                    }
                    else if (gameObject.Position.Y > maxScreenH)
                    {
                        gameObject.Position = new Point(gameObject.Position.X, minScreenH);
                    }
                    else if (gameObject.Position.Y < minScreenH)
                    {
                        gameObject.Position = new Point(gameObject.Position.X, maxScreenH);
                    }
                    Canvas.SetLeft(item.Value, gameObject.Position.X);
                    Canvas.SetBottom(item.Value, gameObject.Position.Y);
                }
            }
        }

        #region ship controlling methods
        private void PrepareShot(GameObject gameObject, double distFromCentroid, double shotAngle)
        {
            if (GunLoaded || gameObject is EnemyShip)
            {
                if (!(gameObject is EnemyShip))
                    GunLoaded = false;
                double maximumDistance = screenWidth / 3;
                // Get the starting point of the shot
                Point shotStart = MathClass.MovePointByGivenDistanceAndAngle(gameObject.Position, distFromCentroid, shotAngle);
                // Create shot with target set in front of the ship nose
                Shot shot = new Shot(gameObject, shotStart, 8, 1, maximumDistance, shotAngle);

                Ellipse el = new Ellipse
                {
                    Height = shot.Size,
                    Width = shot.Size,
                    Fill = CreateNewColorBrush(249, 248, 113),
                    Stroke = CreateNewColorBrush(70, 69, 80),
                    StrokeThickness = 1,
                };

                #region Set shot position

                Canvas.SetLeft(el, shot.Position.X - shot.Size / 2.0);
                Canvas.SetBottom(el, shot.Position.Y - shot.Size / 2.0);

                #endregion

                gameObjectDictionary.Add(shot, el);
                BackgroundCanvas.Children.Add(el);
            }
        }
        private void Shoot()
        {
            if (gameObjectDictionary.Count > 0)
            {
                foreach (var item in gameObjectDictionary)
                {
                    if (item.Key is Shot shot)
                    {
                        shot.Position = MathClass.MovePointByGivenDistanceAndAngle(shot.Position, 8.0 * shot.VelocityMultiplier, shot.MotionDirection);

                        #region Set shot position

                        Canvas.SetLeft(item.Value, shot.Position.X - shot.Size / 2.0);
                        Canvas.SetBottom(item.Value, shot.Position.Y - shot.Size / 2.0);

                        #endregion

                        shot.TraveledDistance += 8.0;

                        if (shot.TraveledDistance >= shot.MaximumDistance)
                        {
                            item.Key.HadCollision = true;
                        }
                    }
                }
            }
        }
        private void AccelerateShip()
        {
            double movementStep = 5.0;

            // Get the current player position
            if (IsAccelerating) // Key W is pressed
            {
                double angleDifference = MathClass.FindDifferenceOfTwoAngles(player.MotionDirection, polygonRotation.Angle);
                if ((angleDifference >= 20 && angleDifference < 160) || (angleDifference > 200 && angleDifference <= 340))
                    player.VelocityMultiplier = 0.0;
                // Check if the ship is moving backwards
                else if (angleDifference >= 160 && angleDifference <= 200)
                    player.VelocityMultiplier *= -1.0; // set negative value to move slowly backwards (inertia)
                player.MotionDirection = polygonRotation.Angle;
            }
            // Move the ship in the forward direction (ship nose) by the specified step
            player.Position = MathClass.MovePointByGivenDistanceAndAngle(player.Position, movementStep
                * player.VelocityMultiplier, player.MotionDirection);
            // Display the new position on the canvas
            Canvas.SetLeft(playerPolygon, player.Position.X - player.Size / 2);
            Canvas.SetTop(playerPolygon, screenHeight - player.Position.Y - player.Size / 2); // substraction from screenHeight used to invert the y axis
        }

        private void ManageVelocity()
        {
            if (IsAccelerating && player.VelocityMultiplier < 1)
                player.VelocityMultiplier += 0.03; // accelerating
            else if (!IsAccelerating && player.VelocityMultiplier > 0)
                player.VelocityMultiplier -= 0.0001; // slowing down
        }

        private double goalRotation;
        private void RotateShip(string direction)
        {
            if (goalRotation == polygonRotation.Angle)
            {
                Point p = GetPlayerShipCenter();
                polygonRotation.CenterX = p.X;
                polygonRotation.CenterY = p.Y;
                // Will set the goal rotation depending on the current direction
                goalRotation = ((direction == "to left") ? polygonRotation.Angle - 3600 : polygonRotation.Angle + 3600);

                storyboard.Duration = new Duration(TimeSpan.FromSeconds(10));
                DoubleAnimation rotateAnimation = new DoubleAnimation()
                {
                    From = polygonRotation.Angle,
                    To = goalRotation,
                    Duration = storyboard.Duration,
                };
                Storyboard.SetTarget(rotateAnimation, playerPolygon);
                Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("(Polygon.RenderTransform).(RotateTransform.Angle)"));
                storyboard.Children.Clear();
                storyboard.Children.Add(rotateAnimation);
                storyboard.Begin();
            }
        }
        /// <summary>
        /// This method will basically make the player teleport to another location.
        /// </summary>
        private void TravelThroughHyperspace()
        {
            if (HyperDriveActive)
            {
                HyperDriveActive = false;
                Point pos = GenerateObjectPosition(player.Size);
                player.Position = new Point(pos.X - screenWidth / 2, pos.Y - screenHeight / 2); //position relative to the polygon
            }
        }

        private void MainWindow1_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if a key is held down
            if (!e.IsRepeat)
            {
                if (e.Key == Key.A || e.Key == Key.Left || e.Key == Key.D || e.Key == Key.Right)
                {
                    // Resume animation
                    storyboard.Resume();
                    goalRotation = polygonRotation.Angle;
                }
            }
            if (e.Key == Key.W || e.Key == Key.Up)
                IsAccelerating = true;
            else if (e.Key == Key.A || e.Key == Key.Left)
                RotateShip("to left");
            else if (e.Key == Key.D || e.Key == Key.Right)
                RotateShip("to right");
            else if (e.Key == Key.Space)
            {
                PrepareShot(player, player.Size, polygonRotation.Angle);
            }
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                TravelThroughHyperspace();
        }

        private void MainWindow1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A || e.Key == Key.Left || e.Key == Key.D || e.Key == Key.Right)
                storyboard.Pause(); // Pause the animation of rotation
            if (e.Key == Key.W || e.Key == Key.Up)
                IsAccelerating = false;
        }
        #endregion

        private void ResolveCollisions()
        {
            for (int i = 0; i < gameObjectDictionary.Count; i++)
            {
                var item = gameObjectDictionary.ElementAt(i);
                if (FindCollisionWithOtherObjects(item.Key, out GameObject collidedObj))
                {
                    item.Key.collidedWith = collidedObj;
                    collidedObj.collidedWith = item.Key;
                    if ((item.Key is PlayerShip || collidedObj is PlayerShip) && player.IsInvulnerable)
                        continue;
                    item.Key.HadCollision = true;
                    collidedObj.HadCollision = true;
                }
            }
        }

        private void RemoveCompletedObjects()
        {
            for (int i = 0; i < gameObjectDictionary.Count; i++)
            {
                var item = gameObjectDictionary.ElementAt(i);

                if (item.Key.HadCollision)
                {
                    #region Create asteroid fragments
                    if (item.Key is Asteroid)
                    {
                        // Create child asteroids
                        if (item.Key.Size > defaultAsteroidSize / 4) //Make sure that it won't get smaller infinitely
                            CreateAsteroidFragments((Asteroid)item.Key, 1.1);
                    }
                    #endregion

                    #region Calculate the score
                    if (item.Key is Shot shot)
                    {
                        if (shot.Owner is PlayerShip)
                        {
                            if (item.Key.collidedWith is Asteroid)
                                if (item.Key.collidedWith.Size == defaultAsteroidSize)
                                    player.Score += 20;
                                else if (item.Key.collidedWith.Size == defaultAsteroidSize / 2)
                                    player.Score += 50;
                                else
                                    player.Score += 100;
                            if (item.Key.collidedWith is EnemyShip)
                                player.Score += 250;

                            ScoreTextBlock.Text = "Score: " + player.Score.ToString();
                        }
                    }
                    #endregion

                    #region Hit the player ship
                    if (item.Key is PlayerShip ps)
                    {
                        LivesTextBlock.Text = "Lives: " + (--ps.Lives).ToString();
                        if (ps.Lives == 0)
                            EndGame();
                        else
                        {
                            ps.Position = new Point(637, 325.5);
                            ps.VelocityMultiplier = 0;

                            MakePlayerInvulnerable();
                            item.Key.HadCollision = false;
                            continue;
                        }
                    }
                    #endregion

                    if (!(item.Key is Shot))
                        gameObjects.Remove(item.Key);

                    BackgroundCanvas.Children.Remove(item.Value);
                    gameObjectDictionary.Remove(item.Key);
                }
            }
        }

        private void EndGame()
        {
            GameOverTextBlock.Text = "Game over";
        }

        private void MakePlayerInvulnerable()
        {
            player.IsInvulnerable = true;

            int counter = 0;
            DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(0.2) };
            timer.Tick += delegate (object sender, EventArgs e)
            {
                if (++counter % 2 == 0)
                {
                    playerPolygon.Fill = CreateNewColorBrush(138, 148, 255);
                    playerPolygon.Stroke = CreateNewColorBrush(70, 69, 80);
                }
                else if (counter == 11)
                {
                    timer.Stop();
                    player.IsInvulnerable = false;
                }
                else
                {
                    playerPolygon.Fill = null;
                    playerPolygon.Stroke = null;
                }
            };
            timer.Start();
        }

        #region Auxiliary methods
        private SolidColorBrush CreateNewColorBrush(byte r, byte g, byte b)
        {
            SolidColorBrush colorBrush = new SolidColorBrush
            {
                Color = Color.FromRgb(r, g, b)
            };
            return colorBrush;
        }

        private Point GetPlayerShipCenter()
        {
            Point a = playerPolygon.Points[0];
            Point b = playerPolygon.Points[1];
            Point c = playerPolygon.Points[2];
            return MathClass.FindCenterOfTriangle(a, b, c);
        }

        private Point GenerateObjectPosition(double size)
        {
            double minWidth = 0 + size / 2.0;
            double maxWidth = screenWidth - size / 2.0 - 20;
            double minHeight = 0 + size / 2.0;
            double maxHeight = screenHeight - size / 2.0 - 20;

            double x = MathClass.GetRandomDouble(minWidth, maxWidth);
            double y = MathClass.GetRandomDouble(minHeight, maxHeight);
            return new Point(x, y);
        }

        private bool FindCollisionWithOtherObjects(GameObject objectToCheck, out GameObject foundObject)
        {
            foreach (var item in gameObjects)
            {
                if (objectToCheck.Equals(item) || (objectToCheck is Asteroid && item is Asteroid))
                    continue;

                double dist;
                dist = MathClass.GetDistance(objectToCheck.Position.X, item.Position.X, objectToCheck.Position.Y, item.Position.Y);
                double radSum = objectToCheck.Size / 2.0 + item.Size / 2.0;
                if (dist < radSum)
                {
                    foundObject = item;
                    return true;
                }
            }
            foundObject = null;
            return false;
        }

        private bool CheckIfPositionIsInsideScreen(Point position)
        {
            return (position.X > 0 && position.X < screenWidth && position.Y > 0 && position.Y < screenHeight);
        }
        #endregion
    }
}
