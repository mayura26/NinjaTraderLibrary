// 
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

#endregion

// Extention of ChartMarker.cs
// This file contains the following drawing tools:
// - SquareFixed
// - ArrowBoxDown
// - ArrowBoxUp
namespace NinjaTrader.NinjaScript.DrawingTools
{
    [TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
    public enum SymbolPosition
    {
        Bottom,
        Top
    }

    public class Fixed
    {
        public static float MinimumSize { get { return 2f; } }

        public SymbolPosition SymbolPosition { get; set; } = SymbolPosition.Bottom;

        public const float Padding = 5f;

        public float GetYPosition(ChartControl chartControl, ChartScale chartScale, float width)
        {
            float yPos = 0;
            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            switch (SymbolPosition)
            {
                case SymbolPosition.Top:
                    yPos = panel.Y + Padding;
                    break;

                case SymbolPosition.Bottom:
                    yPos = panel.Y + panel.H - Padding - width;
                    break;
            }
            return yPos;
        }
    }

    /// <summary>
    /// Represents an interface that exposes information regarding a SquareFixed IDrawingTool.
    /// </summary>
    public class SquareFixed : ChartMarker
    {
        public Fixed Fixed { get; set; } = new Fixed();

        protected void DrawSquare(float width, ChartControl chartControl, ChartScale chartScale)
        {
            areaDeviceBrush.RenderTarget = RenderTarget;
            outlineDeviceBrush.RenderTarget = RenderTarget;

            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point pixelPoint = Anchor.GetPoint(chartControl, panel, chartScale);

            // adjust our x/y to center the rect on our anchor (moving the top left back and up by half)
            float xCentered = (float)(pixelPoint.X - (width / 2f));
            float yCentered = Fixed.GetYPosition(chartControl, chartScale, width);

            SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : areaDeviceBrush.BrushDX;
            if (tmpBrush != null)
                RenderTarget.FillRectangle(new SharpDX.RectangleF(xCentered, yCentered, width, width), tmpBrush);
            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineDeviceBrush.BrushDX;
            if (tmpBrush != null)
                RenderTarget.DrawRectangle(new SharpDX.RectangleF(xCentered, yCentered, width, width), tmpBrush);
        }

        public override object Icon { get { return Gui.Tools.Icons.DrawSquare; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Anchor = new ChartAnchor
                {
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
                    IsEditing = true,
                };
                Name = Custom.Resource.NinjaScriptDrawingToolsChartSquareMarkerName;
                AreaBrush = Brushes.Crimson;
                OutlineBrush = Brushes.DarkGray;
            }
            else if (State == State.Terminated)
                Dispose();
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (Anchor.IsEditing)
                return;
            float barWidth = Math.Max((float)(BarWidth * 1.2), Fixed.MinimumSize * 2); // we draw from center
            DrawSquare(barWidth, chartControl, chartScale);
        }
    }

    /// <summary>
    /// Represents an interface that exposes information regarding a DotFixed IDrawingTool.
    /// </summary>
    public class DotFixed : ChartMarker
    {
        public Fixed Fixed { get; set; } = new Fixed();

        public override object Icon { get { return Gui.Tools.Icons.DrawDot; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Anchor = new ChartAnchor
                {
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
                    IsEditing = true,
                };
                Name = Custom.Resource.NinjaScriptDrawingToolsChartDotMarkerName;
                AreaBrush = Brushes.DodgerBlue;
                OutlineBrush = Brushes.DarkGray;
            }
            else if (State == State.Terminated)
                Dispose();
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (Anchor.IsEditing)
                return;

            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point pixelPoint = Anchor.GetPoint(chartControl, panel, chartScale);

            areaDeviceBrush.RenderTarget = RenderTarget;
            outlineDeviceBrush.RenderTarget = RenderTarget;
            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;

            float width = Math.Max((float)BarWidth, Fixed.MinimumSize);
            float xCentered = (float)(pixelPoint.X - (width / 2f));
            float yCentered = Fixed.GetYPosition(chartControl, chartScale, width);
            // center rendering on anchor is done by width method of drawing here
            SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : areaDeviceBrush.BrushDX;
            if (tmpBrush != null)
                RenderTarget.FillEllipse(new SharpDX.Direct2D1.Ellipse(new SharpDX.Vector2(xCentered, yCentered), width, width), tmpBrush);
            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineDeviceBrush.BrushDX;
            if (tmpBrush != null)
                RenderTarget.DrawEllipse(new SharpDX.Direct2D1.Ellipse(new SharpDX.Vector2(xCentered, yCentered), width, width), tmpBrush);
        }
    }

    /// <summary>
    /// Represents an interface that exposes information regarding a DiamondFixed IDrawingTool.
    /// </summary>
    public class DiamondFixed : SquareFixed
    {
        public override object Icon { get { return Gui.Tools.Icons.DrawDiamond; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Anchor = new ChartAnchor
                {
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
                    IsEditing = true,
                };
                Name = Custom.Resource.NinjaScriptDrawingToolsChartDiamondMarkerName;
                AreaBrush = Brushes.Crimson;
                OutlineBrush = Brushes.DarkGray;
            }
            else if (State == State.Terminated)
                Dispose();
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (Anchor.IsEditing)
                return;

            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point pixelPoint = Anchor.GetPoint(chartControl, panel, chartScale);

            // rotate it 45 degrees and bam, a diamond
            // rotate from anchor since that will be center of rendering in base render
            RenderTarget.Transform = SharpDX.Matrix3x2.Rotation(MathHelper.DegreesToRadians(45), pixelPoint.ToVector2());

            areaDeviceBrush.RenderTarget = RenderTarget;
            outlineDeviceBrush.RenderTarget = RenderTarget;

            float barWidth = Math.Max((float)BarWidth * 2, MinimumSize * 2); // we draw from center

            // We are rotating this square to make a diamond, so we need the distance from opposite angles to be barwidth
            // Using barWidth as the hypotenuse, calculate equal side lengths of a right triangle
            float hypotenuseAdjustedWidth = (float)Math.Sqrt(Math.Pow(barWidth, 2) * 0.5);
            DrawSquare(hypotenuseAdjustedWidth, chartControl, chartScale);

            RenderTarget.Transform = SharpDX.Matrix3x2.Identity;
        }
    }

    public abstract class TriangleBaseFixed : ChartMarker
    {
        [XmlIgnore]
        [Browsable(false)]
        public bool IsUpTriangle { get; protected set; }

        public Fixed Fixed { get; set; } = new Fixed();

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (Anchor.IsEditing)
                return;

            areaDeviceBrush.RenderTarget = RenderTarget;
            outlineDeviceBrush.RenderTarget = RenderTarget;

            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point pixelPoint = Anchor.GetPoint(chartControl, panel, chartScale);
            float barWidth = Math.Max((float)(BarWidth * 0.9), Fixed.MinimumSize);
            pixelPoint.Y = Fixed.GetYPosition(chartControl, chartScale, barWidth);
            SharpDX.Vector2 endVector = pixelPoint.ToVector2();

            // the geometry is created with 0,0 as point origin, and pointing UP by default.
            // so translate & rotate as needed
            SharpDX.Matrix3x2 transformMatrix;

            if (IsUpTriangle)
            {
                transformMatrix = SharpDX.Matrix3x2.Translation(endVector);
            }
            else
            {
                transformMatrix = SharpDX.Matrix3x2.Rotation(MathHelper.DegreesToRadians(180), SharpDX.Vector2.Zero) * SharpDX.Matrix3x2.Translation(endVector);
                transformMatrix *= SharpDX.Matrix3x2.Translation(0, barWidth);
            }

            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
            RenderTarget.Transform = transformMatrix;

            SharpDX.Direct2D1.PathGeometry trianglePathGeometry = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
            SharpDX.Direct2D1.GeometrySink geometrySink = trianglePathGeometry.Open();

            geometrySink.BeginFigure(SharpDX.Vector2.Zero, SharpDX.Direct2D1.FigureBegin.Filled);
            geometrySink.AddLine(new SharpDX.Vector2(barWidth, barWidth));
            geometrySink.AddLine(new SharpDX.Vector2(-barWidth, barWidth));
            geometrySink.AddLine(SharpDX.Vector2.Zero);// cap off figure
            geometrySink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
            geometrySink.Close(); // note this calls dispose for you. but not the other way around

            SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineDeviceBrush.BrushDX;
            if (tmpBrush != null)
                RenderTarget.DrawGeometry(trianglePathGeometry, tmpBrush);
            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : areaDeviceBrush.BrushDX;
            if (tmpBrush != null)
                RenderTarget.FillGeometry(trianglePathGeometry, tmpBrush);

            trianglePathGeometry.Dispose();
            RenderTarget.Transform = SharpDX.Matrix3x2.Identity;
        }
    }

    /// <summary>
    /// Represents an interface that exposes information regarding a Triangle Down Fixed IDrawingTool.
    /// </summary>
    public class TriangleDownFixed : TriangleBaseFixed
    {
        public override object Icon { get { return Gui.Tools.Icons.DrawTriangleDown; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Anchor = new ChartAnchor
                {
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
                    IsEditing = true,
                };
                Name = Custom.Resource.NinjaScriptDrawingToolsChartTriangleDownMarkerName;
                AreaBrush = Brushes.Crimson;
                OutlineBrush = Brushes.DarkGray;
                IsUpTriangle = false;
            }
            else if (State == State.Terminated)
                Dispose();
        }
    }

    /// <summary>
    /// Represents an interface that exposes information regarding a Triangle Up Fixed IDrawingTool.
    /// </summary>
    public class TriangleUpFixed : TriangleBaseFixed
    {
        public override object Icon { get { return Gui.Tools.Icons.DrawTriangleUp; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Anchor = new ChartAnchor
                {
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
                    IsEditing = true,
                };
                Name = Custom.Resource.NinjaScriptDrawingToolsChartTriangleUpMarkerName;
                AreaBrush = Brushes.SeaGreen;
                OutlineBrush = Brushes.DarkGray;
                IsUpTriangle = true;
            }
            else if (State == State.Terminated)
                Dispose();
        }
    }

    public abstract class ArrowBoxMarkerBase : ChartMarker
    {
        [XmlIgnore]
        [Browsable(false)]
        public bool IsUpArrow { get; protected set; }

        public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
        {
            if (Anchor.IsEditing)
                return new Point[0];
            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point pixelPointArrowTop = Anchor.GetPoint(chartControl, panel, chartScale);
            return new[] { new Point(pixelPointArrowTop.X, pixelPointArrowTop.Y) };
        }

        public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            if (DrawingState != DrawingState.Moving || IsLocked)
                return;

            // this is reversed, we're pulling into arrow
            Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
            Anchor.UpdateFromPoint(new Point(point.X, point.Y), chartControl, chartScale);
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (Anchor.IsEditing)
                return;

            areaDeviceBrush.RenderTarget = RenderTarget;
            outlineDeviceBrush.RenderTarget = RenderTarget;

            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point pixelPoint = Anchor.GetPoint(chartControl, panel, chartScale);
            SharpDX.Vector2 endVector = pixelPoint.ToVector2();

            // the geometry is created with 0,0 as point origin, and pointing UP by default.
            // so translate & rotate as needed
            SharpDX.Matrix3x2 transformMatrix;
            if (!IsUpArrow)
            {
                // Flip it around. beware due to our translation we rotate on origin
                transformMatrix = /*SharpDX.Matrix3x2.Scaling(arrowScale, arrowScale) **/ SharpDX.Matrix3x2.Rotation(MathHelper.DegreesToRadians(180), SharpDX.Vector2.Zero) * SharpDX.Matrix3x2.Translation(endVector);
            }
            else
                transformMatrix = /*SharpDX.Matrix3x2.Scaling(arrowScale, arrowScale) **/ SharpDX.Matrix3x2.Translation(endVector);

            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
            RenderTarget.Transform = transformMatrix;

            float barWidth = Math.Max((float)BarWidth, MinimumSize);
            float arrowHeight = barWidth * 3f;
            float arrowPointHeight = barWidth;
            float arrowStemWidth = barWidth / 3f;
            float boxWidth = barWidth;
            float boxHeight = arrowHeight;

            SharpDX.Direct2D1.PathGeometry arrowPathGeometry = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
            SharpDX.Direct2D1.GeometrySink geometrySink = arrowPathGeometry.Open();
            geometrySink.BeginFigure(SharpDX.Vector2.Zero, SharpDX.Direct2D1.FigureBegin.Filled);

            geometrySink.AddLine(new SharpDX.Vector2(barWidth, arrowPointHeight));
            geometrySink.AddLine(new SharpDX.Vector2(arrowStemWidth, arrowPointHeight));
            geometrySink.AddLine(new SharpDX.Vector2(arrowStemWidth, arrowHeight));
            geometrySink.AddLine(new SharpDX.Vector2(-arrowStemWidth, arrowHeight));
            geometrySink.AddLine(new SharpDX.Vector2(-arrowStemWidth, arrowPointHeight));
            geometrySink.AddLine(new SharpDX.Vector2(-barWidth, arrowPointHeight));

            geometrySink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
            geometrySink.Close(); // note this calls dispose for you. but not the other way around

            // Add box around arrow where box is just the outline of a rectangle around the arrow
            SharpDX.Direct2D1.PathGeometry boxPathGeometry = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
            SharpDX.Direct2D1.GeometrySink boxGeometrySink = boxPathGeometry.Open();
            boxGeometrySink.BeginFigure(new SharpDX.Vector2(-boxWidth, 0), SharpDX.Direct2D1.FigureBegin.Hollow);
            boxGeometrySink.AddLine(new SharpDX.Vector2(boxWidth, 0));
            boxGeometrySink.AddLine(new SharpDX.Vector2(boxWidth, boxHeight));
            boxGeometrySink.AddLine(new SharpDX.Vector2(-boxWidth, boxHeight));
            boxGeometrySink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
            boxGeometrySink.Close();

            SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : areaDeviceBrush.BrushDX;
            if (tmpBrush != null)
                RenderTarget.FillGeometry(arrowPathGeometry, tmpBrush);
            tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineDeviceBrush.BrushDX;
            if (tmpBrush != null)
            {
                RenderTarget.DrawGeometry(arrowPathGeometry, tmpBrush);
                RenderTarget.DrawGeometry(boxPathGeometry, tmpBrush);
            }

            arrowPathGeometry.Dispose();
            boxPathGeometry.Dispose();
            RenderTarget.Transform = SharpDX.Matrix3x2.Identity;
        }
    }

    /// <summary>
    /// Represents an interface that exposes information regarding an Arrow Down Fixed IDrawingTool.
    /// </summary>
    public class ArrowBoxDown : ArrowBoxMarkerBase
    {
        public override object Icon { get { return Gui.Tools.Icons.DrawArrowDown; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Anchor = new ChartAnchor
                {
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
                    IsEditing = true,
                };
                Name = Custom.Resource.NinjaScriptDrawingToolsChartArrowDownMarkerName;
                AreaBrush = Brushes.Crimson;
                OutlineBrush = Brushes.DarkGray;
                IsUpArrow = false;
            }
            else if (State == State.Terminated)
                Dispose();
        }
    }

    /// <summary>
    /// Represents an interface that exposes information regarding an Arrow Up Fixed IDrawingTool.
    /// </summary>
    public class ArrowBoxUp : ArrowBoxMarkerBase
    {
        public override object Icon { get { return Gui.Tools.Icons.DrawArrowUp; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Anchor = new ChartAnchor
                {
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchor,
                    IsEditing = true,
                };
                Name = Custom.Resource.NinjaScriptDrawingToolsChartArrowUpMarkerName;
                AreaBrush = Brushes.SeaGreen;
                OutlineBrush = Brushes.DarkGray;
                IsUpArrow = true;
            }
            else if (State == State.Terminated)
                Dispose();
        }
    }

    public static partial class Draw
    {
        /// <summary>
        /// Draws a square.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <returns></returns>
        public static SquareFixed SquareFixed(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, SymbolPosition symbolPosition = SymbolPosition.Bottom)
        {
            SquareFixed square = ChartMarkerCore<SquareFixed>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
            square.Fixed.SymbolPosition = symbolPosition;
            return square;
        }

        public static DotFixed DotFixed(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, SymbolPosition symbolPosition = SymbolPosition.Bottom)
        {
            DotFixed dot = ChartMarkerCore<DotFixed>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
            dot.Fixed.SymbolPosition = symbolPosition;
            return dot;
        }

        public static DiamondFixed DiamondFixed(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, SymbolPosition symbolPosition = SymbolPosition.Bottom)
        {
            DiamondFixed diamond = ChartMarkerCore<DiamondFixed>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
            diamond.Fixed.SymbolPosition = symbolPosition;
            return diamond;
        }

        public static TriangleDownFixed TriangleDownFixed(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, SymbolPosition symbolPosition = SymbolPosition.Bottom)
        {
            TriangleDownFixed triangleDown = ChartMarkerCore<TriangleDownFixed>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
            triangleDown.Fixed.SymbolPosition = symbolPosition;
            return triangleDown;
        }

        public static TriangleUpFixed TriangleUpFixed(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, SymbolPosition symbolPosition = SymbolPosition.Bottom)
        {
            TriangleUpFixed triangleUp = ChartMarkerCore<TriangleUpFixed>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
            triangleUp.Fixed.SymbolPosition = symbolPosition;
            return triangleUp;
        }

        // arrow down
        /// <summary>
        /// Draws an arrow pointing down.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <returns></returns>
        public static ArrowBoxDown ArrowBoxDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
        {
            return ChartMarkerCore<ArrowBoxDown>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
        }

        /// <summary>
        /// Draws an ArrowBox pointing down.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="time"> The time the object will be drawn at.</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <returns></returns>
        public static ArrowBoxDown ArrowBoxDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
        {
            return ChartMarkerCore<ArrowBoxDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null);
        }

        /// <summary>
        /// Draws an ArrowBox pointing down.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
        /// <returns></returns>
        public static ArrowBoxDown ArrowBoxDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
        {
            return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
                ChartMarkerCore<ArrowBoxDown>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null));
        }

        /// <summary>
        /// Draws an ArrowBox pointing down.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="time"> The time the object will be drawn at.</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
        /// <returns></returns>
        public static ArrowBoxDown ArrowBoxDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
        {
            return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
                ChartMarkerCore<ArrowBoxDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null));
        }

        /// <summary>
        /// Draws an ArrowBox pointing down.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
        /// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
        /// <returns></returns>
        public static ArrowBoxDown ArrowBoxDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
        {
            return ChartMarkerCore<ArrowBoxDown>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, null, isGlobal, templateName);
        }

        /// <summary>
        /// Draws an ArrowBox pointing down.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="time"> The time the object will be drawn at.</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
        /// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
        /// <returns></returns>
        public static ArrowBoxDown ArrowBoxDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
        {
            return ChartMarkerCore<ArrowBoxDown>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
        }

        // ArrowBox up
        /// <summary>
        /// Draws an ArrowBox pointing up.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <returns></returns>
        public static ArrowBoxUp ArrowBoxUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
        {
            return ChartMarkerCore<ArrowBoxUp>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null);
        }

        /// <summary>
        /// Draws an ArrowBox pointing up.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="time"> The time the object will be drawn at.</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <returns></returns>
        public static ArrowBoxUp ArrowBoxUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
        {
            return ChartMarkerCore<ArrowBoxUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null);
        }

        /// <summary>
        /// Draws an ArrowBox pointing up.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
        /// <returns></returns>
        public static ArrowBoxUp ArrowBoxUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
        {
            return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
                ChartMarkerCore<ArrowBoxUp>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, brush, false, null));
        }

        /// <summary>
        /// Draws an ArrowBox pointing up.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="time"> The time the object will be drawn at.</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="brush">The brush used to color draw object</param>
        /// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
        /// <returns></returns>
        public static ArrowBoxUp ArrowBoxUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
        {
            return DrawingTool.DrawToggledPricePanel(owner, drawOnPricePanel, () =>
                ChartMarkerCore<ArrowBoxUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, false, null));
        }

        /// <summary>
        /// Draws an ArrowBox pointing up.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
        /// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
        /// <returns></returns>
        public static ArrowBoxUp ArrowBoxUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
        {
            return ChartMarkerCore<ArrowBoxUp>(owner, tag, isAutoScale, barsAgo, Core.Globals.MinDate, y, null, isGlobal, templateName);
        }

        /// <summary>
        /// Draws an ArrowBox pointing up.
        /// </summary>
        /// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
        /// <param name="tag">A user defined unique id used to reference the draw object</param>
        /// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
        /// <param name="time"> The time the object will be drawn at.</param>
        /// <param name="y">The y value or Price for the object</param>
        /// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
        /// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
        /// <returns></returns>
        public static ArrowBoxUp ArrowBoxUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
        {
            return ChartMarkerCore<ArrowBoxUp>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
        }
    }
}
