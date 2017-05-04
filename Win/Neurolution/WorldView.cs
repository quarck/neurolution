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
    class WorldView
    {
        private World _world;

        public CellView[] CellViews;

        public Line[] FoodLocations;

        public Line[] PredatorLocations;

        public WorldView(Grid grid, World world)
        {
            _world = world;

            Random rnd = new Random();

            var brush = new SolidColorBrush(new Color { R = 192, G = 64, B = 64, A = 200 });
            var predatorBrush = new SolidColorBrush(new Color { R = 64, G = 64, B = 255, A = 220 });

            FoodLocations = new Line[_world.Foods.Length];

            for (int i = 0; i < FoodLocations.Length; ++i)
            {
                FoodLocations[i] =
                    new Line
                    {
                        Stroke = brush,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        StrokeThickness =  2.0,
                        X1 = 0,
                        X2 = 0,
                        Y1 = 0,
                        Y2 = 0
                    };

                grid.Children.Add(FoodLocations[i]);
            }

            PredatorLocations = new Line[_world.Predators.Length];

            for (int i = 0; i < PredatorLocations.Length; ++i)
            {
                PredatorLocations[i] =
                    new Line
                    {
                        Stroke = predatorBrush,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        StrokeThickness = 2.0,
                        X1 = 0,
                        X2 = 0,
                        Y1 = 0,
                        Y2 = 0
                    };

                grid.Children.Add(PredatorLocations[i]);
            }

            CellViews = new CellView[_world.Cells.Length];

            for (int i = 0; i < _world.Cells.Length; ++i)
            {
                CellViews[i] = new CellView(grid, _world.Cells[i], rnd);
            }
        }

        public void UpdateFrom(World world)
        {
            foreach (var cellView in CellViews)
            {
                cellView.Update();
            }

            for (int i = 0; i < FoodLocations.Length; ++i)
            {
                var food = world.Foods[i];

                var diameter = Math.Sqrt(food.Value) * 5;

                FoodLocations[i].X1 = food.LocationX;
                FoodLocations[i].Y1 = food.LocationY - diameter / 2.0;
                FoodLocations[i].X2 = food.LocationX;
                FoodLocations[i].Y2 = food.LocationY + diameter / 2.0;
                FoodLocations[i].StrokeThickness = diameter;
            }

            for (int i = 0; i < PredatorLocations.Length; ++i)
            {
                var predator = world.Predators[i];

                var diameter = Math.Sqrt(predator.Value) * 5;

                PredatorLocations[i].X1 = predator.LocationX;
                PredatorLocations[i].Y1 = predator.LocationY - diameter / 2.0;
                PredatorLocations[i].X2 = predator.LocationX;
                PredatorLocations[i].Y2 = predator.LocationY + diameter / 2.0;
                PredatorLocations[i].StrokeThickness = diameter;
            }

        }
    }
}
