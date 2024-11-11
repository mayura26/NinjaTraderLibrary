#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class VWAP : Indicator
	{
		double	iCumVolume			= 0;
		double	iCumTypicalVolume	= 0;
		double curVWAP = 0;
		double v2Sum = 0;
		double hl3 = 0;

        public override string DisplayName
        {
            get
            {
                if (State == State.SetDefaults)
                    return "VWAPx";
                else
                    return "";
            }
        }
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description							= @"Volume Weighted Average Price";
				Name								= "VWAPx";
				Calculate							= Calculate.OnBarClose;
				IsOverlay							= true;
				DisplayInDataBox					= true;
				DrawOnPricePanel					= true;
				DrawHorizontalGridLines				= true;
				DrawVerticalGridLines				= true;
				PaintPriceMarkers					= true;
				ScaleJustification					= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive			= true;
				AddPlot(new Stroke(Brushes.Yellow,DashStyleHelper.Dot,1), PlotStyle.Line, "PlotVWAP");
			}
		}
		
		protected override void OnBarUpdate()
		{
			hl3 = (High[0] + Low[0] + Close[0]) / 3;
			
			if (Bars.IsFirstBarOfSession)
			{
				iCumVolume = VOL()[0];
				iCumTypicalVolume = VOL()[0] * hl3;
				v2Sum = VOL()[0] * hl3 * hl3;
			}
			else
			{
				iCumVolume = iCumVolume + VOL()[0];
				iCumTypicalVolume = iCumTypicalVolume + ( VOL()[0] * hl3 );
				v2Sum = v2Sum + VOL()[0] * hl3 * hl3;
			}
			
			curVWAP = iCumTypicalVolume / iCumVolume;

			PlotVWAP[0] = curVWAP;			
		}
		
		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> PlotVWAP
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
		private VWAP[] cacheVWAP;
		public VWAP VWAP()
		{
			return VWAP(Input);
		}

		public VWAP VWAP(ISeries<double> input)
		{
			if (cacheVWAP != null)
				for (int idx = 0; idx < cacheVWAP.Length; idx++)
					if (cacheVWAP[idx] != null &&  cacheVWAP[idx].EqualsInput(input))
						return cacheVWAP[idx];
			return CacheIndicator<VWAP>(new VWAP(), input, ref cacheVWAP);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VWAP VWAP()
		{
			return indicator.VWAP(Input);
		}

		public Indicators.VWAP VWAP(ISeries<double> input )
		{
			return indicator.VWAP(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VWAP VWAP()
		{
			return indicator.VWAP(Input);
		}

		public Indicators.VWAP VWAP(ISeries<double> input )
		{
			return indicator.VWAP(input);
		}
	}
}

#endregion
