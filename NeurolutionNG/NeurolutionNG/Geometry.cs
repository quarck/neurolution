using Gdk;

namespace Neurolution
{
    public class Line
    {
        public Gdk.GC Stroke {get; set;}
        public double X1 {get; set; }
        public double Y1 {get; set; }
        public double X2 {get; set; }
        public double Y2 {get; set; }

        public void Draw(Gdk.Drawable window)
        {
            window.DrawLine(Stroke, (int)X1, (int)Y1, (int)X2, (int)Y2);
        }
    }

    public class Rectangle
    {
        public Gdk.GC Stroke {get; set;}
        private Gdk.Rectangle _rect;

        public int X {get { return _rect.X;} set{_rect.X = value;} }
        public int Y {get { return _rect.Y;} set{_rect.Y = value;} }
        public int Width {get { return _rect.Width;} set{_rect.Width = value;} }
        public int Height {get { return _rect.Height;} set{_rect.Height = value;} }
        public bool Filled { get; set;}

        public void Draw(Gdk.Drawable window)
        {
            window.DrawRectangle(Stroke, Filled, _rect);
        }
    }
}
