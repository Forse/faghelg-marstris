using Microsoft.Xna.Framework;

namespace Marstris.Core
{
    public class Bullet : Actor
    {
        private new const int Width = 1;
        private new const int Height = 1;

        public Bullet(Color foreground, Color background) : base(foreground, background, Width, Height)
        {
            Animation.CurrentFrame[0].Glyph = 'o';
        }
    }
}