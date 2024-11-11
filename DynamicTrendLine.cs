#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using NinjaTrader.Gui.PropertiesTest;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class DynamicTrendLine : Indicator
	{
        // Momentum Constants
        private int dataLength = 8;
        private int atrMALength = 5;
        private int atrSmoothLength = 3;

        // Trend/Chop Constants
        private double volatileLimit = 5.0;
        private double trendLimit = 2.5;
        private double chopLimit = 1.5;

        // IMD Constants
        private double fK = 0.6;
        private double fExponent = 4.0;

        // Momentum Variables
        private EMA momentumMA;
        private EMA momentumMain;
        private EMA momentumSignal;
        private Series<double> momentum;
		private Series<double> trendScore;

        public override string DisplayName
        {
            get
            {
                if (State == State.SetDefaults)
                    return "DynamicTrendLine";
                else
                    return "";
            }
        }

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Line using IMD and three periods to produce a realtime trend line";
				Name										= "DynamicTrendLine";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
                PeriodFastMA = 8;
				PeriodSlowMA					= 13;
				PeriodStableMA					= 21;
				AddPlot(Brushes.Gray, "Trendline");
			}
			else if (State == State.Configure)
			{
                momentum = new Series<double>(this);
                trendScore = new Series<double>(this);
            }
		}

		protected override void OnBarUpdate()
        {
            if (CurrentBar < 21)
            {
                Value[0] = Input[0];
            }
            else
            {
                // Generate momentum signals
                momentum[0] = 0;
                for (int i = 0; i < dataLength; i++)
                    momentum[0] += (Close[0] > Open[i] ? 1 : Close[0] < Open[i] ? -1 : 0);

                momentumMA = EMA(momentum, atrMALength);
                momentumMain = EMA(momentumMA, atrSmoothLength);
                momentumSignal = EMA(momentumMain, atrSmoothLength);
                trendScore[0] = momentumMain[0] + (momentumMain[0] - momentumSignal[0]);

                // Set color based on trendScore
                if (trendScore[0] > volatileLimit)
                    PlotBrushes[0][0] = Brushes.LimeGreen;
                else if (trendScore[0] > trendLimit)
                    PlotBrushes[0][0] = Brushes.SeaGreen;
                else if (trendScore[0] > chopLimit)
                    PlotBrushes[0][0] = Brushes.DarkGreen;
                else if (trendScore[0] < (-1 * volatileLimit))
                    PlotBrushes[0][0] = Brushes.Red;
                else if (trendScore[0] < (-1 * trendLimit))
                    PlotBrushes[0][0] = Brushes.Tomato;
                else if (trendScore[0] < (-1 * chopLimit))
                    PlotBrushes[0][0] = Brushes.DarkRed;

				int adaptivePeriod = 1;
                if (Math.Abs(trendScore[0]) > volatileLimit)
                    adaptivePeriod = PeriodFastMA;
                else if (Math.Abs(trendScore[0]) > trendLimit)
                    adaptivePeriod = PeriodSlowMA;
                else
                    adaptivePeriod = PeriodStableMA;

				Value[0] = imd(Input[0], Value[1], adaptivePeriod);
            }
        }

		private double imd (double input, double prev, int fPeriod)
		{
			double period = Math.Max(1, fPeriod);
			return prev + (input - prev) / Math.Min(period, Math.Max(1.0, fK * period * Math.Pow(input / prev, fExponent)));
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name= "PeriodFastMA", Description="Fast MA Period", Order=1, GroupName="Parameters")]
		public int PeriodFastMA
        { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name= "PeriodSlowMA", Description="Slow MA Period", Order=2, GroupName="Parameters")]
		public int PeriodSlowMA
        { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="PeriodStableMA", Description="Stable MA Period", Order=3, GroupName="Parameters")]
		public int PeriodStableMA
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Trendline
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DynamicTrendLine[] cacheDynamicTrendLine;
		public DynamicTrendLine DynamicTrendLine(int periodFastMA, int periodSlowMA, int periodStableMA)
		{
			return DynamicTrendLine(Input, periodFastMA, periodSlowMA, periodStableMA);
		}

		public DynamicTrendLine DynamicTrendLine(ISeries<double> input, int periodFastMA, int periodSlowMA, int periodStableMA)
		{
			if (cacheDynamicTrendLine != null)
				for (int idx = 0; idx < cacheDynamicTrendLine.Length; idx++)
					if (cacheDynamicTrendLine[idx] != null && cacheDynamicTrendLine[idx].PeriodFastMA == periodFastMA && cacheDynamicTrendLine[idx].PeriodSlowMA == periodSlowMA && cacheDynamicTrendLine[idx].PeriodStableMA == periodStableMA && cacheDynamicTrendLine[idx].EqualsInput(input))
						return cacheDynamicTrendLine[idx];
			return CacheIndicator<DynamicTrendLine>(new DynamicTrendLine(){ PeriodFastMA = periodFastMA, PeriodSlowMA = periodSlowMA, PeriodStableMA = periodStableMA }, input, ref cacheDynamicTrendLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DynamicTrendLine DynamicTrendLine(int periodFastMA, int periodSlowMA, int periodStableMA)
		{
			return indicator.DynamicTrendLine(Input, periodFastMA, periodSlowMA, periodStableMA);
		}

		public Indicators.DynamicTrendLine DynamicTrendLine(ISeries<double> input , int periodFastMA, int periodSlowMA, int periodStableMA)
		{
			return indicator.DynamicTrendLine(input, periodFastMA, periodSlowMA, periodStableMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DynamicTrendLine DynamicTrendLine(int periodFastMA, int periodSlowMA, int periodStableMA)
		{
			return indicator.DynamicTrendLine(Input, periodFastMA, periodSlowMA, periodStableMA);
		}

		public Indicators.DynamicTrendLine DynamicTrendLine(ISeries<double> input , int periodFastMA, int periodSlowMA, int periodStableMA)
		{
			return indicator.DynamicTrendLine(input, periodFastMA, periodSlowMA, periodStableMA);
		}
	}
}

#endregion
