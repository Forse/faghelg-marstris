using Microsoft.Xna.Framework;

namespace Marstris.Core
{
    public abstract class Actor : SadConsole.Entities.Entity
    {
        private int _health; 
        private int _maxHealth;

        public int Health
        {
            get
            {
                return _health;
            }
            set
            {
                _health = value;
            }
        }

        public int MaxHealth
        {
            get
            {
                return _maxHealth;
            }
            set
            {
                _maxHealth = value;
            }
        }

        protected Actor(Color foreground, Color background, int width=1, int height=1) : base(width, height)
        {
            for (var i = 0; i < width*height; i++)
            {
                Animation.CurrentFrame[i].Foreground = foreground;
                Animation.CurrentFrame[i].Background = background;
            }
        }

        public bool MoveTo(Point newPosition)
        {
            Position = newPosition;
            return true;
        }
    }
}
