using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Marstris.Core.Communication
{
    public class PlayerData
    {
        public int Id { get; set; }
        public Color Color { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}";
        }
    }

    public class GameData
    {
        public PlayerData PlayerData { get; set; }
        public GameLayout Layout { get; set; }
        public Dictionary<int, InitialBoardMember> Members { get; set; } = new();
    }

    public class InitialBoardMember
    {
        public Color PlayerColor { get; set; }
        public List<Guid> PassengersIds { get; set; }
        public Color PassengersColor { get; set; }
        public int BusId { get; set; }
        public Color BusColor { get; set; }
    }

    public struct Position
    {
        public int X;
        public int Y;
    }
}