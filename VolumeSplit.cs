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
	public class VolumeSplit : Indicator
	{
        private Series<double> BuyVol;
        private Series<double> SellVol;
        private WMA smoothBuy;
        private WMA smoothSell;
        private Series<double> smoothNetVol;
        private SMA avgVolume;
        private SMA avgBuyVol;
        private SMA avgSellVol;
        private Series<bool> bullVolPump;
        private Series<bool> bullVolDump;
        private Series<bool> midVolPump;
        private Series<bool> midVolDump;
        private Series<bool> volCrossBuy;
        private Series<bool> volCrossSell;
        private Series<bool> irregVol;

        // Volume Constants
        private double volTopLimit = 85;
        private double volUpperLimit = 75;
        private double volPumpGainLimit = 5;
        private double volIrregLimit = 150;
        private double regVolLevel = 60;

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Calculate buy/sell volume splits";
				Name										= "VolumeSplit";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AveVolPeriod					= 15;
				VolSmooth					= 8;
				AddPlot(Brushes.ForestGreen, "SmoothBuy");
				AddPlot(Brushes.Firebrick, "SmoothSell");
                AddPlot(new Stroke(Brushes.Transparent), PlotStyle.Bar, "SmoothNetVol");

				Plots[2].AutoWidth = true;
            }
			else if (State == State.Configure)
			{
                BuyVol = new Series<double>(this);
                SellVol = new Series<double>(this);
                smoothNetVol = new Series<double>(this);
                bullVolPump = new Series<bool>(this);
                bullVolDump = new Series<bool>(this);
                midVolPump = new Series<bool>(this);
                midVolDump = new Series<bool>(this);
                volCrossBuy = new Series<bool>(this);
                volCrossSell = new Series<bool>(this);
                irregVol = new Series<bool>(this);
            }
		}

		protected override void OnBarUpdate()
		{
            if (CurrentBar < AveVolPeriod)
                return;

            double gap = Open[0] - Close[1];

            double bull_gap = Math.Max(gap, 0);
            double bear_gap = Math.Abs(Math.Min(gap, 0));

            double body = Math.Abs(Close[0] - Open[0]);
            double BarRange = High[0] - Low[0];
            double wick = BarRange - body;

            bool up_bar = Close[0] > Open[0];

            double bull = wick + (up_bar ? body : 0) + bull_gap;
            double bear = wick + (up_bar ? 0 : body) + bear_gap;
            double VolRange = bull + bear;
            double BScore = VolRange > 0 ? bull / VolRange : 0.5;
            BuyVol[0] = BScore * Volume[0];
            SellVol[0] = Volume[0] - BuyVol[0];
            double buy_percent = (BuyVol[0] / Volume[0]) * 100;
            double sell_percent = (SellVol[0] / Volume[0]) * 100;

            smoothBuy = WMA(WMA(BuyVol, AveVolPeriod), VolSmooth);
            smoothSell = WMA(WMA(SellVol, AveVolPeriod), VolSmooth);
            smoothNetVol[0] = smoothBuy[0] - smoothSell[0];

            double netVolH = Math.Max(smoothBuy[0], smoothSell[0]);
            double netVolL = Math.Min(smoothBuy[0], smoothSell[0]);
            bool risingVol = (smoothNetVol[0] - smoothNetVol[1]) > 0;

            double volPumpLevel = volPumpGainLimit / 100;
            double irregVolLevel = volIrregLimit / 100;

            avgVolume = SMA(Volume, AveVolPeriod);
            avgBuyVol = SMA(smoothBuy, AveVolPeriod);
            avgSellVol = SMA(smoothSell, AveVolPeriod);

            bullVolPump[0] =
                risingVol
                && Math.Abs(smoothBuy[0] - smoothBuy[1]) / avgBuyVol[0] > volPumpLevel
                && smoothBuy[0] > smoothSell[0];

            bullVolDump[0] =
                !risingVol
                && Math.Abs(smoothSell[0] - smoothSell[1]) / avgSellVol[0] > volPumpLevel
                && smoothBuy[0] < smoothSell[0];

            midVolPump[0] = smoothBuy[0] > smoothSell[0] && risingVol;
            midVolDump[0] = smoothBuy[0] < smoothSell[0] && !risingVol;
            volCrossBuy[0] = CrossAbove(smoothBuy, smoothSell, 1);
            volCrossSell[0] = CrossAbove(smoothSell, smoothBuy, 1);
            irregVol[0] = Volume[0] / avgVolume[0] > irregVolLevel;

            Values[0][0] = smoothBuy[0];
            Values[1][0] = smoothSell[0];
            Values[2][0] = smoothNetVol[0];

			if (midVolPump[0])
			{
                PlotBrushes[2][0] = Brushes.ForestGreen;
            }
            else if (midVolDump[0])
            {
                PlotBrushes[2][0] = Brushes.Firebrick;
            }
			else
            {
                PlotBrushes[2][0] = Brushes.Gray;
            }
        }

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="AveVolPeriod", Description="Average Volume Period", Order=1, GroupName="Parameters")]
		public int AveVolPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="VolSmooth", Description="Volume smoothing period", Order=2, GroupName="Parameters")]
		public int VolSmooth
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SmoothBuy
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SmoothSell
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SmoothNetVol
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
		private VolumeSplit[] cacheVolumeSplit;
		public VolumeSplit VolumeSplit(int aveVolPeriod, int volSmooth)
		{
			return VolumeSplit(Input, aveVolPeriod, volSmooth);
		}

		public VolumeSplit VolumeSplit(ISeries<double> input, int aveVolPeriod, int volSmooth)
		{
			if (cacheVolumeSplit != null)
				for (int idx = 0; idx < cacheVolumeSplit.Length; idx++)
					if (cacheVolumeSplit[idx] != null && cacheVolumeSplit[idx].AveVolPeriod == aveVolPeriod && cacheVolumeSplit[idx].VolSmooth == volSmooth && cacheVolumeSplit[idx].EqualsInput(input))
						return cacheVolumeSplit[idx];
			return CacheIndicator<VolumeSplit>(new VolumeSplit(){ AveVolPeriod = aveVolPeriod, VolSmooth = volSmooth }, input, ref cacheVolumeSplit);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VolumeSplit VolumeSplit(int aveVolPeriod, int volSmooth)
		{
			return indicator.VolumeSplit(Input, aveVolPeriod, volSmooth);
		}

		public Indicators.VolumeSplit VolumeSplit(ISeries<double> input , int aveVolPeriod, int volSmooth)
		{
			return indicator.VolumeSplit(input, aveVolPeriod, volSmooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VolumeSplit VolumeSplit(int aveVolPeriod, int volSmooth)
		{
			return indicator.VolumeSplit(Input, aveVolPeriod, volSmooth);
		}

		public Indicators.VolumeSplit VolumeSplit(ISeries<double> input , int aveVolPeriod, int volSmooth)
		{
			return indicator.VolumeSplit(input, aveVolPeriod, volSmooth);
		}
	}
}

#endregion
