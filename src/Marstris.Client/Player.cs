using Microsoft.Xna.Framework;

namespace Marstris.Core
{
    public class Player : Actor
    {
        private new const int Width = 1;
        private new const int Height = 3;

        public Player(Color foreground, Color background) : base(foreground, background, Width, Height)
        {
            SetDefault();
        }

        public void SetDefault()
        {
            Animation.CurrentFrame[0].Glyph = 'o';
            Animation.CurrentFrame[1].Glyph = '|';
            Animation.CurrentFrame[2].Glyph = '^';
        }
        
        public void SetSplatted()
        {
            Animation.CurrentFrame[0].Glyph = '@';
            Animation.CurrentFrame[1].Glyph = '@';
            Animation.CurrentFrame[2].Glyph = '@';        
        }
    }
}