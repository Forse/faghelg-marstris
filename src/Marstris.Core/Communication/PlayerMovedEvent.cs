using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marstris.Core.Communication
{
    public class PlayerMovedEvent
    {
        public int PlayerId { get; set; }
        
        public Point Position { get; set; }
    }
}
