using Microsoft.Xna.Framework;

namespace Marstris.Core
{
	public class Bus : Actor
    {
        private new const int Width = 3;
        private new const int Height = 2;

        public Bus(Color foreground, Color background) : base(foreground, background, Width, Height)
        {
            Animation.CurrentFrame[0].Glyph = '-';
            Animation.CurrentFrame[1].Glyph = '-';
            Animation.CurrentFrame[2].Glyph = '-';
            Animation.CurrentFrame[3].Glyph = 'o';
            Animation.CurrentFrame[4].Glyph = 'o';
            Animation.CurrentFrame[5].Glyph = 'o';
        }
    }
}
