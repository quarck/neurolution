using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Neurolution
{
    public class CellView
    {
        public Line TailLine;
        public Line BodyLine;

        public Cell Cell;

        public CellView(Grid grid, Cell cell, Random rnd)
        {
            Cell = cell;

            var brush = 
                new SolidColorBrush(new Color
                {
                    R = (byte) rnd.Next(128),
                    G = (byte) rnd.Next(128),
                    B = (byte) rnd.Next(128),
                    A = 255
                });

            // Add a Line Element
            TailLine = new Line
            {
                Stroke = brush,
                X1 = 0.0,
                X2 = 0.0,
                Y1 = 0.0,
                Y2 = 0.0,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeThickness = 1
            };
            grid.Children.Add(TailLine);

            BodyLine = new Line
            {
                Stroke = brush,
                X1 = 0.0,
                X2 = 0.0,
                Y1 = 0.0,
                Y2 = 0.0,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeThickness = 1
            };
            grid.Children.Add(BodyLine);

            Update();
        }

        public void Update()
        {
            //if (Cell.Alive)
            if (Cell.LocationX >= 0.0 && Cell.LocationX < AppProperties.WorldWidth
                && Cell.LocationY >= 0.0 && Cell.LocationY < AppProperties.WorldHeight)
            {
                double adjRotation = Cell.Rotation - Math.PI/2.0;

                double dxBody = Cell.EyeBase*Math.Cos(adjRotation)/2.0;
                double dyBody = Cell.EyeBase*Math.Sin(adjRotation)/2.0;

                double dxTail = Cell.TailLength*Math.Cos(Cell.Rotation);
                double dyTail = Cell.TailLength*Math.Sin(Cell.Rotation);

                TailLine.X1 = Cell.LocationX;
                TailLine.X2 = Cell.LocationX - dxTail;
                TailLine.Y1 = Cell.LocationY;
                TailLine.Y2 = Cell.LocationY - dyTail;

                BodyLine.X1 = Cell.LocationX - dxBody;
                BodyLine.X2 = Cell.LocationX + dxBody;
                BodyLine.Y1 = Cell.LocationY - dyBody;
                BodyLine.Y2 = Cell.LocationY + dyBody;
            }
            else
            {
                TailLine.X1 = 0.0; // ;
                TailLine.X2 = 0.0; //  - dxTail;
                TailLine.Y1 = 0.0; // ;
                TailLine.Y2 = 0.0; //  - dyTail;
                              
                BodyLine.X1 = 0.0; //  - dxBody;
                BodyLine.X2 = 0.0; //  + dxBody;
                BodyLine.Y1 = 0.0; //  - dyBody;
                BodyLine.Y2 = 0.0; //  + dyBody;
            }
        }
    }
}
