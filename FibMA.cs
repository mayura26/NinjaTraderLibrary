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
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class FibMA : Indicator
	{
		#region Variables
		private EMA hfib_ma_5;
		private EMA hfib_ma_8;
		private EMA hfib_ma_13;
		private EMA hfib_ma_21;
		private EMA hfib_ma_34;
		private EMA hfib_ma_55;
		private EMA hfib_ma_89;
		private EMA hfib_ma_144;
		private EMA hfib_ma_233;
		private EMA hfib_ma_377;
		private EMA hfib_ma_610;
		private EMA hfib_ma_987;
		private EMA hfib_ma_1597;
		private EMA hfib_ma_2584;
		private EMA hfib_ma_4181;
		private EMA lfib_ma_5;
		private EMA lfib_ma_8;
		private EMA lfib_ma_13;
		private EMA lfib_ma_21;
		private EMA lfib_ma_34;
		private EMA lfib_ma_55;
		private EMA lfib_ma_89;
		private EMA lfib_ma_144;
		private EMA lfib_ma_233;
		private EMA lfib_ma_377;
		private EMA lfib_ma_610;
		private EMA lfib_ma_987;
		private EMA lfib_ma_1597;
		private EMA lfib_ma_2584;
		private EMA lfib_ma_4181;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Fibonacci Moving Average";
				Name = "FibMA";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsAutoScale = false;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive = true;
				AddPlot(Brushes.Azure, "FibMAAverage");
				Plots[0].DashStyleHelper = DashStyleHelper.Dash;
				AddPlot(Brushes.Purple, "FibHigh");
				AddPlot(Brushes.Purple, "FibLow");
			}
			else if (State == State.DataLoaded)
			{
				hfib_ma_5 = EMA(High, 5);
				hfib_ma_8 = EMA(High, 8);
				hfib_ma_13 = EMA(High, 13);
				hfib_ma_21 = EMA(High, 21);
				hfib_ma_34 = EMA(High, 34);
				hfib_ma_55 = EMA(High, 55);
				hfib_ma_89 = EMA(High, 89);
				hfib_ma_144 = EMA(High, 144);
				hfib_ma_233 = EMA(High, 233);
				hfib_ma_377 = EMA(High, 377);
				hfib_ma_610 = EMA(High, 610);
				hfib_ma_987 = EMA(High, 987);
				hfib_ma_1597 = EMA(High, 1597);
				hfib_ma_2584 = EMA(High, 2584);
				hfib_ma_4181 = EMA(High, 4181);

				lfib_ma_5 = EMA(Low, 5);
				lfib_ma_8 = EMA(Low, 8);
				lfib_ma_13 = EMA(Low, 13);
				lfib_ma_21 = EMA(Low, 21);
				lfib_ma_34 = EMA(Low, 34);
				lfib_ma_55 = EMA(Low, 55);
				lfib_ma_89 = EMA(Low, 89);
				lfib_ma_144 = EMA(Low, 144);
				lfib_ma_233 = EMA(Low, 233);
				lfib_ma_377 = EMA(Low, 377);
				lfib_ma_610 = EMA(Low, 610);
				lfib_ma_987 = EMA(Low, 987);
				lfib_ma_1597 = EMA(Low, 1597);
				lfib_ma_2584 = EMA(Low, 2584);
				lfib_ma_4181 = EMA(Low, 4181);
			}
		}

		protected override void OnBarUpdate()
		{

			Values[1][0] = (hfib_ma_5[0] + hfib_ma_8[0] + hfib_ma_13[0] + hfib_ma_21[0] + hfib_ma_34[0] + hfib_ma_55[0] + hfib_ma_89[0] + hfib_ma_144[0] + hfib_ma_233[0] + hfib_ma_377[0] + hfib_ma_610[0] + hfib_ma_987[0] + hfib_ma_1597[0] + hfib_ma_2584[0] + hfib_ma_4181[0]) / 15;
			Values[2][0] = (lfib_ma_5[0] + lfib_ma_8[0] + lfib_ma_13[0] + lfib_ma_21[0] + lfib_ma_34[0] + lfib_ma_55[0] + lfib_ma_89[0] + lfib_ma_144[0] + lfib_ma_233[0] + lfib_ma_377[0] + lfib_ma_610[0] + lfib_ma_987[0] + lfib_ma_1597[0] + lfib_ma_2584[0] + lfib_ma_4181[0]) / 15;

			Values[0][0] = (Values[1][0] + Values[2][0]) / 2;
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FibMAAverage
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FibHigh
		{
			get { return Values[1]; }
		}	

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FibLow
		{
			get { return Values[2]; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FibMA[] cacheFibMA;
		public FibMA FibMA()
		{
			return FibMA(Input);
		}

		public FibMA FibMA(ISeries<double> input)
		{
			if (cacheFibMA != null)
				for (int idx = 0; idx < cacheFibMA.Length; idx++)
					if (cacheFibMA[idx] != null &&  cacheFibMA[idx].EqualsInput(input))
						return cacheFibMA[idx];
			return CacheIndicator<FibMA>(new FibMA(), input, ref cacheFibMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FibMA FibMA()
		{
			return indicator.FibMA(Input);
		}

		public Indicators.FibMA FibMA(ISeries<double> input )
		{
			return indicator.FibMA(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FibMA FibMA()
		{
			return indicator.FibMA(Input);
		}

		public Indicators.FibMA FibMA(ISeries<double> input )
		{
			return indicator.FibMA(input);
		}
	}
}

#endregion
