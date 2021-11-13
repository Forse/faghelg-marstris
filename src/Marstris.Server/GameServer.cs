using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Marstris.Core;
using Marstris.Core.Communication;
using Microsoft.Xna.Framework;
using Timer = System.Timers.Timer;

namespace Marstris.Server
{
    public class GameServer : IDisposable
    {
        private readonly Random _random = new();
        private int _gravityCounter;
        public bool IsRunning { get; private set; }
        private readonly int _numberOfPlayers;

        private const int Footer = 2;
        private const int Width = 70;
        private const int Height = 50;

        private readonly GameLayout _layout = new()
        {
            Width = Width,
            Height = Height
        };

        private GameState _state = new GameState
        {
            
        };
        
        private readonly TcpListener _listener;

        private static readonly Dictionary<int, Color> Colors = new()
        {
            [1] = Color.Aquamarine,
            [2] = Color.Red,
            [3] = Color.Yellow
        };

        private readonly Dictionary<int, ClientHandler> _handlers = new();
        private readonly int _port;
        private readonly Timer _timer = new(50);

        public GameServer(int port, int numberOfPlayers)
        {
            Console.WriteLine($"Number of players: {numberOfPlayers}");
            _numberOfPlayers = numberOfPlayers;
            _port = port;
            _listener = new TcpListener(IPAddress.Any, port);
            _timer.Elapsed += UpdateState;
        }

        public void MovePlayerLeft(int id)
        {
            var player = _state.Players[id];
            var otherPlayers = _state.Players.Values.Where(p => p.Id != id);
            
            if (player.X <= 0 || otherPlayers.Any(other => other.Y == player.Y && other.X == player.X - 1))
            {
                return;
            }
            player.X--;
        }

        public void MovePlayerRight(int id)
        {
            var player = _state.Players[id];
            var otherPlayers = _state.Players.Values.Where(p => p.Id != id);
            
            if (player.X >= Width - 1 || otherPlayers.Any(other => other.Y == player.Y && other.X == player.X + 1))
            {
                return;
            }
            player.X++;
        }
        
        public void MovePlayerDown(int id)
        {
            const int amount = 1;
            var player = _state.Players[id];
            if (player.Splat)
            {
                return;
            }
            var otherPlayers = _state.Players.Values.Where(p => p.Id != id);
            
            // on top of other player
            if (otherPlayers.Any(other => other.X == player.X && other.Y == player.Y + PlayerPosition.Height))
            {
                return;
            }
            
            // in mid air
            if (player.Y + amount < Height - Footer - BusPosition.Height)
            {
                player.Y += amount;
                return;
            }
            
            foreach (var bus in _state.Buses.Values)
            {
                if (bus.X <= player.X && player.X < bus.X + BusPosition.Width)
                {
                    // landing on bus
                    var passenger = _state.Passengers.FirstOrDefault(p => p.IsAvailable() && p.PlayerId == player.Id);
                    if (passenger == null)
                    {
                        return;
                    }
                    bus.AddPassenger(passenger);

                    player.ResetPosition(_random.Next(0, Width), 0);
                    return;
                }
            }
            
            // landing outside bus
            if (player.Y + amount < Height - Footer)
            {
                player.Y += amount;
                return;
            }

            player.Splat = true;
            
            _state.Scores[player.Id] -= 101;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(StopGame);
            Console.WriteLine($"Listening for connections on port {_port}");
            _listener.Start();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    while (_handlers.Count < _numberOfPlayers)
                    {
                        var socket = await _listener.AcceptSocketAsync(cancellationToken);
                        var id = _handlers.Count + 1;
                        var playerData = new PlayerData
                        {
                            Id = _handlers.Count + 1,
                            Color = Colors[id]
                        };
                        var handler = await ClientHandler.HandshakeAsync(playerData, socket, this, _layout);
                        handler.Start(cancellationToken);
                        _handlers[handler.Id] = handler;    
                    }
                    
                    StartGame();
                    while (IsRunning)
                    {
                        await Task.Delay(500, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                finally
                {
                    foreach (var handler in _handlers.Values)
                    {
                        handler.Dispose();
                    }
                    _handlers.Clear();
                }
            }
        }

        private void StartGame()
        {
            Console.WriteLine("Starting game");

            var slice = Width / (_handlers.Count + 1);

            var players = new Dictionary<int, PlayerPosition>();
            var buses = new Dictionary<int, BusPosition>();
            var score = new Dictionary<int, int>();
            var passengers = new List<PassengerPosition>();
            var bullets = new Dictionary<int, BulletPosition>();
            var index = 1;
            foreach (var key in _handlers.Keys)
            {
                players[key] = new PlayerPosition
                {
                    X = _random.Next(0, Width),
                    Y = 0,
                    Id = key,
                    Color = Colors[index],
                };
                buses[key] = new BusPosition(index * slice, _layout.Height + 1 - Footer - BusPosition.Height)
                {
                    Id = key,
                    Color = Colors[index]
                };
                
                passengers.AddRange(Enumerable.Range(1, 3 * _numberOfPlayers).Select(_ => new PassengerPosition
                {
                    Color = Colors[index],
                    PlayerId = key,
                }));

                bullets[key] = new BulletPosition
                {
                    Id = key,
                    Color = Colors[index]
                };

                score[key] = 0;
                
                index++;
            }
            
            _state = new GameState
            {
                Status = GameStatus.Running,
                Players = players,
                Buses = buses,
                Scores = score,
                Passengers = passengers,
                Bullets = bullets
            };
            
            IsRunning = true;
            _timer.Start();
        }

        private void StopGame()
        {
            _timer.Stop();
        }

        private async void UpdateState(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (_state.Status != GameStatus.Finished)
                {
                    UpdatePlayerYPosition();
                    UpdateBullets();
                    CheckScore();    
                }
                await Task.WhenAll(_handlers.Values.Select(h => h.UpdateStateAsync(_state)));
                _state.Sounds.Clear();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void CheckScore()
        {
            if (_state.Scores.Values.Any(v => v >= 600))
            {
                _state.Status = GameStatus.Finished;
                var winner = _state.Scores.First();
                foreach (var score in _state.Scores)
                {
                    if (score.Value > winner.Value)
                    {
                        winner = score;
                    }
                }
                _state.FinishText = $"Player {winner.Key} conquered Mars.";
            }
        }

        private void UpdateBullets()
        {
            foreach (var bullet in _state.Bullets.Values)
            {
                bullet.MoveUntil(0, Width);
                foreach (var player in _state.Players.Values)
                {
                    if (bullet.X == player.X && player.Y <= bullet.Y && bullet.Y <= player.Y + PlayerPosition.Height)
                    {
                        player.ResetPosition(_random.Next(0, Width), 0);
                        bullet.ResetPosition();
                        _state.Sounds.Add(Sound.BulletHit);
                    }
                }
            }
        }

        private void UpdatePlayerYPosition()
        {
            _gravityCounter += 1;
            if (_gravityCounter < 10)
            {
                return;
            }
            foreach (var player in _state.Players.Values)
            {
                if (player.Splat)
                {
                    player.ResetPosition(_random.Next(0, Width), 0);
                }
                else
                {
                    MovePlayerDown(player.Id);    
                }
            }
            _gravityCounter = 0;
        }

        public void Dispose()
        {
            _timer.Dispose();
            GC.SuppressFinalize(this);
        }

        public void MoveBusLeft(int id)
        {
            var bus = _state.Buses[id];
            if (bus.X <= -BusPosition.Width)
            {
                return;
            }
            bus.MoveLeft();
            if (bus.X is <= -BusPosition.Width or >= Width - 1 + BusPosition.Width)
            {
                foreach (var passenger in bus.Passengers)
                {
                    _state.Scores[passenger.PlayerId] += 100;    
                }
                bus.KickOutPassengers();
            }
        }

        public void MoveBusRight(int id)
        {
            var bus = _state.Buses[id];
            if (bus.X >= Width - 1 + BusPosition.Width)
            {
                return;
            }
            bus.MoveRight();
            if (bus.X is <= -BusPosition.Width or >= Width)
            {
                foreach (var passenger in bus.Passengers)
                {
                    _state.Scores[passenger.PlayerId] += 100;    
                }
                bus.KickOutPassengers();
                _state.Sounds.Add(Sound.BusUnload);
            }
        }

        public void FireLeft(int id)
        {
            var bullet = _state.Bullets[id];
            if (bullet.IsFiring())
            {
                return;
            }
            
            var player = _state.Players[id];
            bullet.Fire(player.X, player.Y, -1);
            _state.Sounds.Add(Sound.BulletFire);
        }

        public void FireRight(int id)
        {
            var bullet = _state.Bullets[id];
            if (bullet.IsFiring())
            {
                return;
            }

            var player = _state.Players[id];
            bullet.Fire(player.X, player.Y, 1);
            _state.Sounds.Add(Sound.BulletFire);
        }
    }
}