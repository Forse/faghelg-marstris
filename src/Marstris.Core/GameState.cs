using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Marstris.Core
{
    public enum Sound
    {
        BulletFire,
        BulletHit,
        BusUnload
    }
    
    public enum GameStatus
    {
        Waiting,
        Running,
        Crashed,
        Finished
    }
    
    public class GameState
    {
        public GameStatus Status { get; set; }
        public string FinishText { get; set; }
        public Dictionary<int, int> Scores { get; set; } = new();
        public Dictionary<int, PlayerPosition> Players { get; set; } = new();
        public Dictionary<int, BusPosition> Buses { get; set; } = new();
        public Dictionary<int, BulletPosition> Bullets { get; set; } = new(); 
        public List<PassengerPosition> Passengers { get; set; } = new();
        public List<Sound> Sounds { get; set; } = new();
    }

    public class PassengerPosition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int PlayerId { get; set; }
        public Color Color { get; set; }
        public int X { get; set; } = -50;
        public int Y { get; set; } = -50;
        public const int Height = 3; 

        public bool IsAvailable()
        {
            return X == -50 && Y == -50;
        }

        public void ResetPosition()
        {
            X = -50;
            Y = -50;
        }
    }

    public class BulletPosition
    {
        private int _moved = 0;
        public const int Height = 1;
        public const int Width = 1;
        
        public int Id { get; set; }
        public Color Color { get; set; }
        public int X { get; set; } = -50;
        public int Y { get; set; } = -50;
        public int Direction { get; set; } = 0;

        public void ResetPosition()
        {
            X = -50;
            Y = -50;
            Direction = 0;
            _moved = 0;
        }

        public bool IsFiring()
        {
            return Direction != 0;
        }

        public void MoveUntil(int min, int max)
        {
            X += Direction;
            if (X < min || X > max)
            {
                ResetPosition();
                return;
            }

            _moved++;
            if (_moved % 15 == 0)
            {
                Y++;
            }
        }

        public void Fire(int x, int y, int direction)
        {
            X = x;
            Y = y;
            Direction = direction;
        }
    }
    
    public class BusPosition
    {
        public const int Height = 2;
        public const int Width = 3;
        
        public int Id { get; set; }
        public Color Color { get; set; }
        public int InitialX { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public List<PassengerPosition> Passengers { get; set; } = new();

        public BusPosition()
        {
            
        }

        public BusPosition(int x, int y)
        {
            X = x;
            InitialX = x;
            Y = y;
        }
        
        public bool IsFull() => Passengers.Count >= 3;

        public void MoveLeft()
        {
            X -= 1;
            foreach (var passenger in Passengers.Where(p => p != null))
            {
                passenger.X -= 1;
            }
        }
        
        public void MoveRight()
        {
            X += 1;
            foreach (var passenger in Passengers.Where(p => p != null))
            {
                passenger.X += 1;
            }
        }

        public int KickOutPassengers()
        {
            var count = Passengers.Count;
            foreach (var passenger in Passengers)
            {
                passenger.ResetPosition();
            }
            Passengers.Clear();
            return count;
        }

        public bool IsEmpty()
        {
            return Passengers.All(p => p == null);
        }

        public void MoveToInitialPosition()
        {
            if (X < InitialX)
            {
                MoveRight();
            }
            else if (X > InitialX)
            {
                MoveLeft();
            }
        }

        public void AddPassenger(PassengerPosition passenger)
        {
            var count = Passengers.Count;
            if (count >= 3)
            {
                return;
            }
            
            Passengers.Add(passenger);
            passenger.X = X + count;
            passenger.Y = Y + 1 - PassengerPosition.Height;
        }
    }
    
    public class PlayerPosition
    {
        public PlayerPosition()
        {
            
        }

        public PlayerPosition(int x, int y)
        {
            X = x;
            InitialX = x;
            Y = y;
            InitialY = y;
        }

        public void ResetPosition(int x, int y)
        {
            X = x;
            Y = y;
            Splat = false;
        }
        
        public bool Splat { get; set; }
        public const int Height = 3;
        public int Id { get; set; }
        public Color Color { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int InitialX { get; set; }
        public int InitialY { get; set; }
    }
}