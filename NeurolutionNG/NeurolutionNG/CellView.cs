using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Neurolution
{
    public class CellView
    {
        private Line _tailLine;
        private Line _bodyLine;

        private Cell _cell;

        private Gdk.Window _window;

        public CellView(Gtk.Widget canvas, Cell cell, Random rnd)
        {
            _cell = cell;
            _window = canvas.GdkWindow;           

            var gc = new Gdk.GC(_window);
            gc.Copy(canvas.Style.ForegroundGC(Gtk.StateType.Normal));
            gc.RgbFgColor = new Gdk.Color((byte) rnd.Next(128), (byte) rnd.Next(128), (byte) rnd.Next(128));


            // Add a Line Element
            _tailLine = new Line
            {
                    Stroke = gc,
                    X1 = 0.0,
                    X2 = 1.0,
                    Y1 = 0.0,
                    Y2 = 1.0
            };

            _bodyLine = new Line
            {
                    Stroke = gc,
                    X1 = 0.0,
                    X2 = 1.0,
                    Y1 = 0.0,
                    Y2 = 1.0
            };

            Update();
        }

        public void Draw()
        {
            if (_cell.LocationX >= 0.0 && _cell.LocationX < AppProperties.WorldWidth
                && _cell.LocationY >= 0.0 && _cell.LocationY < AppProperties.WorldHeight)
            {
                _tailLine.Draw(_window);
                _bodyLine.Draw(_window);           
            }
        }

        public void Update()
        {
            //if (Cell.Alive)
            if (_cell.CurrentEnergy > 0.001 &&
                _cell.LocationX >= 0.0 && _cell.LocationX < AppProperties.WorldWidth
                && _cell.LocationY >= 0.0 && _cell.LocationY < AppProperties.WorldHeight)
            {
                double adjRotation = _cell.Rotation - Math.PI/2.0;

                double dxBody = Cell.EyeBase*Math.Cos(adjRotation)/2.0;
                double dyBody = Cell.EyeBase*Math.Sin(adjRotation)/2.0;

                double dxTail = Cell.TailLength*Math.Cos(_cell.Rotation);
                double dyTail = Cell.TailLength*Math.Sin(_cell.Rotation);

                _tailLine.X1 = _cell.LocationX;
                _tailLine.X2 = _cell.LocationX - dxTail;
                _tailLine.Y1 = _cell.LocationY;
                _tailLine.Y2 = _cell.LocationY - dyTail;

                _bodyLine.X1 = _cell.LocationX - dxBody;
                _bodyLine.X2 = _cell.LocationX + dxBody;
                _bodyLine.Y1 = _cell.LocationY - dyBody;
                _bodyLine.Y2 = _cell.LocationY + dyBody;
            }
            else
            {
                _tailLine.X1 = 0.0; // ;
                _tailLine.X2 = 1.0; //  - dxTail;
                _tailLine.Y1 = 0.0; // ;
                _tailLine.Y2 = 1.0; //  - dyTail;
                              
                _bodyLine.X1 = 0.0; //  - dxBody;
                _bodyLine.X2 = 1.0; //  + dxBody;
                _bodyLine.Y1 = 0.0; //  - dyBody;
                _bodyLine.Y2 = 1.0; //  + dyBody;
            }
        }
    }
}
