// Source of: wbennettjr
// From: https://forum.ninjatrader.com/forum/ninjatrader-8/add-on-development/99836-new-addon-add-tool-tips-to-drawing-objects
using NinjaTrader.Code;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml.Linq;

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator
    {
        protected void Dispatch(Action action)
        {
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.InvokeAsync(action);
        }
    }
}

namespace NinjaTrader.Custom.AddOns
{
    public class ToolTipAddOn : AddOnBase
    {
        protected override void OnStateChange()
        {
            base.OnStateChange();

            if (State == State.Terminated)
            {
                Output.Reset(PrintTo.OutputTab1);

                if (_controlCenter != null)
                {
                    _controlCenter.Dispatcher.InvokeAsync(RemoveControlCenterMenuItem);
                }
            }
        }

        protected override void OnWindowCreated(Window window)
        {
            if (window is ControlCenter)
            {
                HandleControlCenterCreated(window);
                return;
            }

            if (window is Chart)
            {
                HandleChartCreated(window);
                return;
            }
        }

        protected override void OnWindowDestroyed(Window window)
        {
            if (window is ControlCenter)
            {
                HandleControlCenterDestroyed();
                return;
            }

            if (window is Chart)
            {
                HandleChartDestroyed(window);
                return;
            }
        }

        protected override void OnWindowSaved(Window window, XElement element)
        {
            // HACK: Method not called for ControlCenter so save with each chart.
            //var cc = window as ControlCenter;
            var cc = window as Chart;

            if (cc != null)
            {
                var addOnElement = new XElement(AddOnName);
                var isEnabledElement = new XElement("IsEnabled", ToolTipUtils.IsEnabled);
                addOnElement.Add(isEnabledElement);
                element.Add(addOnElement);
            }

            base.OnWindowSaved(window, element);
        }

        protected override void OnWindowRestored(Window window, XElement element)
        {
            base.OnWindowRestored(window, element);

            // TODO: Method not called on recompile.  Need to find a workaround.
            // HACK: Method not called for ControlCenter so load from each chart.
            //var cc = window as ControlCenter;
            var cc = window as Chart;

            if (cc == null) return;

            XElement addOnElement = element.Element(AddOnName);

            if (addOnElement == null) return;

            XElement isEnabledElement = addOnElement.Element("IsEnabled");

            if (isEnabledElement == null) return;

            bool isEnabled;

            if (bool.TryParse(isEnabledElement.Value, out isEnabled))
                ToolTipUtils.IsEnabled = isEnabled;
        }

        private const string AddOnName = "ToolTipAddOn";
        private const string MenuCaption = "Show Drawing Tool Tips";
        private const string MainMenuId = "ControlCenterMenuItemTools";

        private NTMenuItem _addOnMenuItem;
        private ControlCenter _controlCenter;

        private void RemoveExistingMenu(ControlCenter cc, string mainMenuItemId, string menuHeader)
        {
            var mainMenuItem = cc.FindFirst(mainMenuItemId) as NTMenuItem;

            if (mainMenuItem == null) return;

            var existing = mainMenuItem.Items.OfType<NTMenuItem>().FirstOrDefault(mi => (string)mi.Header == menuHeader);

            if (existing == null) return;

            mainMenuItem.Items.Remove(existing);
        }

        private void HookMenuEvents()
        {
            if (_addOnMenuItem == null) return;

            _addOnMenuItem.Click += OnMenuItemClick;
        }

        private void UnhookMenuEvents()
        {
            if (_addOnMenuItem == null) return;

            _addOnMenuItem.Click -= OnMenuItemClick;
        }

        private void AddControlCenterMenuItem()
        {
            if (_controlCenter == null) return;

            RemoveExistingMenu(_controlCenter, MainMenuId, MenuCaption);
            var mainMenuItem = _controlCenter.FindFirst(MainMenuId) as NTMenuItem;

            if (mainMenuItem == null) return;

            _addOnMenuItem = new NTMenuItem
            {
                Header = MenuCaption,
                Style = Application.Current.TryFindResource("MainMenuItem") as Style,
                IsCheckable = true,
                IsChecked = ToolTipUtils.IsEnabled
            };

            mainMenuItem.Items.Add(_addOnMenuItem);
            HookMenuEvents();
        }

        private void RemoveControlCenterMenuItem()
        {
            if (_addOnMenuItem == null || _controlCenter == null) return;

            RemoveExistingMenu(_controlCenter, MainMenuId, MenuCaption);
            UnhookMenuEvents();
            _addOnMenuItem = null;
        }

        private void HandleControlCenterCreated(Window window)
        {
            _controlCenter = window as ControlCenter;

            if (_controlCenter == null) return;

            AddControlCenterMenuItem();
        }

        private void HandleControlCenterDestroyed()
        {
            RemoveControlCenterMenuItem();
            _controlCenter = null;
        }

        private void HandleChartCreated(Window window)
        {
            ToolTipUtils.AddChart(window as Chart);
        }

        private void HandleChartDestroyed(Window window)
        {
            ToolTipUtils.RemoveChart(window as Chart);
        }

        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (_addOnMenuItem == null) return;

            ToolTipUtils.IsEnabled = _addOnMenuItem.IsChecked;
        }

        private void WriteLine(string message, params object[] args)
        {
            Output.Process(string.Format(message, args), PrintTo.OutputTab1);
        }
    }

    public static class ToolTipUtils
    {
        private static bool _isEnabled = true;
        public static bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value) return;

                _isEnabled = value;
                UpdateChartsEnabled();
            }
        }

        public static void AddChart(Chart chart)
        {
            if (chart == null) return;
            if (_chartLookup.ContainsKey(chart)) return;

            _chartLookup[chart] = new ChartToolTipHelper();
        }

        public static void RemoveChart(Chart chart)
        {
            if (chart == null) return;
            if (!_chartLookup.ContainsKey(chart)) return;

            ChartToolTipHelper helper = _chartLookup[chart];
            _chartLookup.Remove(chart);
            helper.Dispose();
        }

        public static Chart GetChart(this DrawingTool drawing)
        {
            if (drawing == null) return null;

            ChartPanel chartPanel = drawing.ChartPanel;

            if (chartPanel == null) return null;

            ChartControl chartControl = chartPanel.ChartControl;

            if (chartControl == null) return null;

            return chartControl.OwnerChart;
        }

        public static void SetToolTipText(this DrawingTool drawing, string text)
        {
            WeakToolTipInfos toolTipInfos = GetToolTipInfos(drawing);

            if (toolTipInfos == null) return;

            toolTipInfos.Add(new ToolTipInfo(drawing) { ToolTipText = text });
        }

        public static void SetToolTipContent(this DrawingTool drawing, object content)
        {
            WeakToolTipInfos toolTipInfos = GetToolTipInfos(drawing);

            if (toolTipInfos == null) return;

            toolTipInfos.Add(new ToolTipInfo(drawing) { ToolTipContent = content });
        }

        public static void SetToolTipContentFactory(this DrawingTool drawing, Func<object> factory)
        {
            WeakToolTipInfos toolTipInfos = GetToolTipInfos(drawing);

            if (toolTipInfos == null) return;

            toolTipInfos.Add(new ToolTipInfo(drawing) { ToolTipContentFactory = factory });
        }

        private static readonly Dictionary<Chart, ChartToolTipHelper> _chartLookup = new Dictionary<Chart, ChartToolTipHelper>();

        private static WeakToolTipInfos GetToolTipInfos(DrawingTool drawing)
        {
            Chart chart = drawing.GetChart();

            if (chart == null) return null;

            ChartToolTipHelper chartHelper;
            _chartLookup.TryGetValue(chart, out chartHelper);

            if (chartHelper == null) return null;

            return chartHelper.GetToolTipInfos(drawing);
        }

        private static void UpdateChartsEnabled()
        {
            _chartLookup.Values.ToList().ForEach(helper => helper.IsEnabled = IsEnabled);
        }
    }

    public class ToolTipInfo
    {
        public ToolTipInfo(DrawingTool drawing)
        {
            _drawingReference.SetTarget(drawing);
            DrawingTag = drawing.Tag;
        }

        private WeakReference<DrawingTool> _drawingReference = new WeakReference<DrawingTool>(null);
        public DrawingTool Drawing
        {
            get
            {
                DrawingTool result;
                _drawingReference.TryGetTarget(out result);
                return result;
            }
        }

        private WeakReference toolTipFactoryContent = new WeakReference(null);
        private object toolTipContent;
        public object ToolTipContent
        {
            get
            {
                if (toolTipContent == null)
                {
                    if (ToolTipContentFactory != null)
                    {
                        object result = toolTipFactoryContent.Target;

                        if (result == null)
                        {
                            toolTipFactoryContent.Target = ToolTipContentFactory();
                            result = toolTipFactoryContent.Target;
                        }

                        return result;
                    }
                }

                return toolTipContent;
            }
            set { toolTipContent = value; }
        }

        public Func<object> ToolTipContentFactory { get; set; }

        public string ToolTipText
        {
            get { return toolTipContent != null ? toolTipContent.ToString() : string.Empty; }
            set
            {
                ToolTipContent = !string.IsNullOrEmpty(value) ? value : null;
            }
        }

        public string DrawingTag { get; }
    }

    public class WeakToolTipInfos : IEnumerable<ToolTipInfo>
    {
        public WeakToolTipInfos()
        {
            _items = new LinkedList<ToolTipInfo>();
            _tagLookup = new Dictionary<string, LinkedListNode<ToolTipInfo>>();
        }

        public void Add(ToolTipInfo item)
        {
            if (_tagLookup.ContainsKey(item.DrawingTag))
            {
                _items.Remove(_tagLookup[item.DrawingTag]);
                _tagLookup.Remove(item.DrawingTag);
            }

            LinkedListNode<ToolTipInfo> node = _items.AddLast(item);
            _tagLookup.Add(item.DrawingTag, node);
        }

        public void Clear()
        {
            _items.Clear();
            _tagLookup.Clear();
        }

        public IEnumerator<ToolTipInfo> GetEnumerator()
        {
            LinkedListNode<ToolTipInfo> current = _items.First;

            while (current != null)
            {
                LinkedListNode<ToolTipInfo> next = current.Next;

                if (current.Value.Drawing != null)
                    yield return current.Value;
                else
                {
                    _items.Remove(current);
                    _tagLookup.Remove(current.Value.DrawingTag);
                }

                current = next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private LinkedList<ToolTipInfo> _items;
        private Dictionary<string, LinkedListNode<ToolTipInfo>> _tagLookup;
    }

    public class ToolTipDisposable : IDisposable
    {
        private bool _disposed = false;

        protected void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                DisposeManagedResources();
            }

            DisposeUnmanagedResources();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void DisposeManagedResources()
        {
        }

        protected virtual void DisposeUnmanagedResources()
        {
        }

        protected void WriteLine(string message, params object[] args)
        {
            Output.Process(string.Format(message, args), PrintTo.OutputTab1);
        }
    }

    public class ChartToolTipHelper : ToolTipDisposable
    {
        public WeakToolTipInfos GetToolTipInfos(DrawingTool drawing)
        {
            if (drawing == null) return null;

            ChartPanel chartPanel = drawing.ChartPanel;

            if (chartPanel == null) return null;

            WeakToolTipInfos toolTipInfos = GetToolTips(chartPanel);

            if (toolTipInfos == null)
            {
                var helper = new ChartPanelToolTipHelper(chartPanel);
                _panelLookup.AddFirst(helper);
                toolTipInfos = helper.ToolTipInfos;
            }

            return toolTipInfos;
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value) return;

                _isEnabled = value;
                UpdatePanelsEnabled();
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            LinkedListNode<ChartPanelToolTipHelper> current = _panelLookup.First;

            while (current != null)
            {
                current.Value.Dispose();
                current = current.Next;
            }
        }

        private readonly LinkedList<ChartPanelToolTipHelper> _panelLookup = new LinkedList<ChartPanelToolTipHelper>();

        private WeakToolTipInfos GetToolTips(ChartPanel chartPanel)
        {
            WeakToolTipInfos result = null;
            LinkedListNode<ChartPanelToolTipHelper> current = _panelLookup.First;

            while (current != null)
            {
                LinkedListNode<ChartPanelToolTipHelper> next = current.Next;

                if (current.Value.ChartPanel == null)
                {
                    _panelLookup.Remove(current);
                    current.Value.Dispose();
                    continue;
                }

                if (current.Value.ChartPanel == chartPanel)
                    result = current.Value.ToolTipInfos;

                current = next;
            }

            return result;
        }

        private void UpdatePanelsEnabled()
        {
            LinkedListNode<ChartPanelToolTipHelper> current = _panelLookup.First;

            while (current != null)
            {
                LinkedListNode<ChartPanelToolTipHelper> next = current.Next;

                if (current.Value.ChartPanel == null)
                {
                    _panelLookup.Remove(current);
                    current.Value.Dispose();
                    continue;
                }

                current.Value.IsEnabled = IsEnabled;
                current = next;
            }
        }
    }

    public class ChartPanelToolTipHelper : ToolTipDisposable
    {
        public ChartPanelToolTipHelper(ChartPanel chartPanel)
        {
            _chartPanelReference.SetTarget(chartPanel);

            ToolTipInfos = new WeakToolTipInfos();
            HookEvents();
        }

        private WeakReference<ChartPanel> _chartPanelReference = new WeakReference<ChartPanel>(null);

        public ChartPanel ChartPanel
        {
            get
            {
                ChartPanel result;
                _chartPanelReference.TryGetTarget(out result);
                return result;
            }
        }

        public WeakToolTipInfos ToolTipInfos { get; }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value) return;

                _isEnabled = value;

                if (_isEnabled)
                    HookEvents();
                else
                    UnhookEvents();
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            ToolTipInfos.Clear();
            UnhookEvents();
        }

        private DrawingTool _currentDrawing;
        private Popup _toolTip;

        private void HookEvents()
        {
            ChartPanel chartPanel = ChartPanel;

            if (chartPanel == null) return;

            chartPanel.MouseMove += HandleMouseMove;
        }

        private void UnhookEvents()
        {
            ChartPanel chartPanel = ChartPanel;

            if (chartPanel == null) return;

            chartPanel.MouseMove -= HandleMouseMove;
        }

        private Panel CreateToolTipChild(object content)
        {
            var element = content as UIElement;

            if (element == null)
            {
                element = new TextBlock
                {
                    Text = content.ToString(),
                    Background = SystemColors.InfoBrush,
                    Foreground = SystemColors.InfoTextBrush,
                };
            }

            var result = new Grid();
            result.Children.Add(element);
            return result;
        }

        private Popup CreateToolTip(object content)
        {
            return new Popup
            {
                AllowsTransparency = true,
                Placement = PlacementMode.MousePoint,
                IsOpen = false,
                Child = CreateToolTipChild(content),
            };
        }

        private void ClearToolTip(Popup toolTip)
        {
            if (toolTip == null) return;

            ((Panel)toolTip.Child).Children.Clear();
        }

        private void DisplayToolTip(ToolTipInfo info, DrawingTool drawing, ChartControl chartControl)
        {
            try
            {
                if (drawing == null) return;
                if (drawing.DrawingState != DrawingState.Normal) return;
                if (info.ToolTipContent == null) return;

                _currentDrawing = drawing;
                _toolTip = CreateToolTip(info.ToolTipContent);
                _toolTip.IsOpen = true;
                _toolTip.StaysOpen = true;

                MouseButtonEventHandler handleMouseDown = null;

                handleMouseDown = (_, __) =>
                {
                    if (_toolTip == null)
                    {
                        chartControl.OwnerChart.PreviewMouseDown -= handleMouseDown;
                        return;
                    }

                    if (_toolTip.IsMouseDirectlyOver) return;

                    _toolTip.IsOpen = false;
                };

                chartControl.OwnerChart.PreviewMouseDown += handleMouseDown;
                var toolTip = _toolTip;

                _toolTip.Closed += (sender, args) =>
                {
                    chartControl.OwnerChart.PreviewMouseDown -= handleMouseDown;
                    ClearToolTip(toolTip);
                    _toolTip = null;
                    _currentDrawing = null;
                };
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
        }

        private ChartScale GetScale(IDrawingTool drawing)
        {
            if (drawing == null) return null;
            if (drawing.ChartPanel == null) return null;
            if (drawing.ChartPanel.Scales == null) return null;

            ChartScale result = null;

            foreach (ChartScale scale in drawing.ChartPanel.Scales)
            {
                if (scale.ScaleJustification == drawing.ScaleJustification)
                {
                    result = scale;
                    break;
                }
            }

            return result;
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            var chartPanel = sender as ChartPanel;

            if (chartPanel == null) return;

            ChartControl chartControl = chartPanel.ChartControl;

            if (chartControl == null) return;

            foreach (ToolTipInfo toolTipInfo in ToolTipInfos)
            {
                DrawingTool drawing = toolTipInfo.Drawing;

                if (drawing == null) continue;

                ChartScale scale = GetScale(drawing);

                if (!drawing.IsVisibleOnChart(chartControl, scale, chartControl.FirstTimePainted, chartControl.LastTimePainted))
                    continue;

                Point position = e.GetPosition(chartControl);
                position.X = ChartingExtensions.ConvertToHorizontalPixels(position.X, chartControl.PresentationSource);
                position.Y = ChartingExtensions.ConvertToVerticalPixels(position.Y, chartControl.PresentationSource);

                if (drawing.GetCursor(chartControl, drawing.ChartPanel, scale, position) == null) continue;

                if (_toolTip != null)
                {
                    if (_currentDrawing == drawing) break;

                    _toolTip.IsOpen = false;
                    break;
                }

                ClearToolTip(_toolTip);
                DisplayToolTip(toolTipInfo, drawing, chartControl);
                break;
            }
        }
    }
}
