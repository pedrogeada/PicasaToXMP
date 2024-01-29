using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicasaToXMP
{
    internal class FaceRegion
    {
        public Rectangle Rect { get; set; }
        public string ContactId { get; set; }
        public int Index { get; set; }

        // Constructor
        public FaceRegion(int index, Rectangle rect, string contactId)
        {
            Index = index;
            Rect = rect;
            ContactId = contactId;
        }
    }
}
