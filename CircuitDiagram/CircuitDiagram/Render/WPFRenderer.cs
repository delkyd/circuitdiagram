﻿// WPFRenderer.cs
//
// Circuit Diagram http://www.circuit-diagram.org/
//
// Copyright (C) 2012  Sam Fisher
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using TextAlignment = CircuitDiagram.Render.TextAlignment;
using System.Windows.Media.Imaging;
using CircuitDiagram.Components.Description.Render;
using System.Windows.Documents;
using System.Windows.Controls;
using CircuitDiagram.Render.Path;

namespace CircuitDiagram.Render
{
    public class WPFRenderer : IRenderContext
    {
        public bool Absolute { get { return true; } }

        DrawingVisual m_visual;
        DrawingContext dc;

        public DrawingVisual DrawingVisual { get { return m_visual; } }

        public WPFRenderer()
        {
            m_visual = new DrawingVisual();
        }

        public void Begin()
        {
            dc = m_visual.RenderOpen();
        }

        public void End()
        {
            dc.Close();
        }

        public void StartSection(object tag)
        {
        }

        public System.IO.MemoryStream GetPNGImage(int width, int height, bool center = false)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Default);
            
            if (center)
                m_visual.Offset = new Vector(width / 2 - m_visual.ContentBounds.Width / 2 - m_visual.ContentBounds.Left, height / 2 - m_visual.ContentBounds.Height / 2 - m_visual.ContentBounds.Top);

            bitmap.Render(m_visual);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            System.IO.MemoryStream returnStream = new System.IO.MemoryStream();
            encoder.Save(returnStream);
            returnStream.Flush();
            m_visual.Offset = new Vector(0,0);
            return returnStream;
        }

        public System.IO.MemoryStream GetPNGImage2(double width, double height, double actualWidth, double actualHeight, bool whiteBG = false)
        {
            double dpiX = (width / actualWidth) * 96d;
            double dpiY = (height /actualHeight) * 96d;

            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)width, (int)height, dpiX, dpiY, PixelFormats.Default);

            if (whiteBG)
            {
                DrawingVisual backgroundVisual = new System.Windows.Media.DrawingVisual();
                using (var dc = backgroundVisual.RenderOpen())
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, actualWidth, actualHeight));
                bitmap.Render(backgroundVisual);
            }

            bitmap.Render(m_visual);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            System.IO.MemoryStream returnStream = new System.IO.MemoryStream();
            encoder.Save(returnStream);
            returnStream.Flush();
            m_visual.Offset = new Vector(0, 0);
            return returnStream;
        }

        public FixedDocument GetDocument(Size pageSize)
        {
            FixedDocument returnDocument = new FixedDocument();
            PageContent pageContent = new PageContent();
            pageContent.Width = pageSize.Width;
            pageContent.Height = pageSize.Height;
            FixedPage fixedPage = new FixedPage();
            fixedPage.Width = pageSize.Width;
            fixedPage.Height = pageSize.Height;

            Canvas containerCanvas = new Canvas();
            containerCanvas.Width = m_visual.ContentBounds.Width;
            containerCanvas.Height = m_visual.ContentBounds.Height;
            DrawingBrush brush = new DrawingBrush(m_visual.Drawing);
            containerCanvas.Background = brush;

            double scale = 1.0d;
            if (m_visual.ContentBounds.Width > fixedPage.Width)
                scale = fixedPage.Width / m_visual.ContentBounds.Width;

            containerCanvas.LayoutTransform = new ScaleTransform(scale, scale);
            fixedPage.Children.Add(containerCanvas);
            ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
            returnDocument.Pages.Add(pageContent);
            return returnDocument;
        }

        public void DrawLine(Point start, Point end, double thickness)
        {
            Pen newPen = new System.Windows.Media.Pen(Brushes.Black, thickness);
            newPen.StartLineCap = PenLineCap.Square;
            newPen.EndLineCap = PenLineCap.Square;
            dc.DrawLine(newPen, start, end);
        }

        public void DrawRectangle(Point start, Size size, double thickness, bool fill = false)
        {
            dc.DrawRectangle((fill ? Brushes.Black : Brushes.Transparent), new Pen(Brushes.Black, thickness), new Rect(start, size));
        }

        public void DrawEllipse(Point centre, double radiusX, double radiusY, double thickness, bool fill = false)
        {
            dc.DrawEllipse((fill ? Brushes.Black : Brushes.Transparent), new Pen(Brushes.Black, thickness), centre, radiusX, radiusY);
        }

        public void DrawPath(Point start, IList<IPathCommand> commands, double thickness, bool fill = false)
        {
            Pen newPen = new System.Windows.Media.Pen(Brushes.Black, thickness);
            newPen.StartLineCap = PenLineCap.Square;
            newPen.EndLineCap = PenLineCap.Square;
            dc.DrawGeometry((fill ? Brushes.Black : Brushes.Transparent), newPen, RenderHelper.GetGeometry(start, commands, fill));
        }

        public void DrawText(Point anchor, TextAlignment alignment, IEnumerable<TextRun> textRuns)
        {
            double totalWidth = 0d;
            double totalHeight = 0d;

            foreach (TextRun run in textRuns)
            {
                FormattedText ft = new FormattedText(run.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(RenderHelper.CircuitFont()), run.Formatting.Size, Brushes.Black);
                if (run.Formatting.FormattingType == TextRunFormattingType.Subscript)
                    ft = new FormattedText(run.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(RenderHelper.CircuitFont()), run.Formatting.Size / 1.5, Brushes.Black);
                totalWidth += ft.Width;
                if (ft.Height > totalHeight)
                    totalHeight = ft.Height;
            }

            Point renderLocation = anchor;
            if (alignment == TextAlignment.TopCentre || alignment == TextAlignment.CentreCentre || alignment == TextAlignment.BottomCentre)
                renderLocation.X -= totalWidth / 2;
            else if (alignment == TextAlignment.TopRight || alignment == TextAlignment.CentreRight || alignment == TextAlignment.BottomRight)
                renderLocation.X -= totalWidth;
            if (alignment == TextAlignment.CentreLeft || alignment == TextAlignment.CentreCentre || alignment == TextAlignment.CentreRight)
                renderLocation.Y -= totalHeight / 2;
            else if (alignment == TextAlignment.BottomLeft || alignment == TextAlignment.BottomCentre || alignment == TextAlignment.BottomRight)
                renderLocation.Y -= totalHeight;

            double horizontalOffsetCounter = 0;
            foreach (TextRun run in textRuns)
            {
                if (run.Formatting.FormattingType == TextRunFormattingType.Normal)
                {
                    FormattedText formattedText = new FormattedText(run.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(RenderHelper.CircuitFont()), run.Formatting.Size, Brushes.Black);
                    dc.DrawText(formattedText, Point.Add(renderLocation, new Vector(horizontalOffsetCounter, 0d)));
                    horizontalOffsetCounter += formattedText.Width;
                }
                else if (run.Formatting.FormattingType == TextRunFormattingType.Subscript)
                {
                    FormattedText formattedText = new FormattedText(run.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(RenderHelper.CircuitFont()), run.Formatting.Size / 1.5, Brushes.Black);
                    dc.DrawText(formattedText, Point.Add(renderLocation, new Vector(horizontalOffsetCounter, totalHeight - formattedText.Height)));
                    horizontalOffsetCounter += formattedText.Width;
                }
                else if (run.Formatting.FormattingType == TextRunFormattingType.Superscript)
                {
                    FormattedText formattedText = new FormattedText(run.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(RenderHelper.CircuitFont()), run.Formatting.Size / 1.5, Brushes.Black);
                    dc.DrawText(formattedText, Point.Add(renderLocation, new Vector(horizontalOffsetCounter, -3d)));
                    horizontalOffsetCounter += formattedText.Width;
                }
            }
        }
    }
}
