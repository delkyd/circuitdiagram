﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CircuitDiagram
{
    class ToolboxComponent : ContentControl
    {
        internal void SetIcon(System.Windows.Media.ImageSource imageSource)
        {
            var rect = new Rectangle();
            rect.Width = 32;
            rect.Height = 32;
            rect.UseLayoutRounding = true;
            rect.Fill = Brushes.White;
            rect.OpacityMask = new ImageBrush(imageSource) { Stretch = Stretch.None };
            rect.SetValue(Canvas.LeftProperty, 6.5);
            rect.SetValue(Canvas.TopProperty, 6.5);

            Canvas c = new Canvas();
            c.Children.Add(rect);

            this.Content = c;
        }
    }
}
