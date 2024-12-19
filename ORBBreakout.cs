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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class ORBBreakout : Strategy
	{
		private DateTime triggerTime;
		private double triggerPrice;
		private const double POINTS = 9;      // Distance for breakout in points
		private const int TICK_TARGET = 22;   // Profit target in ticks
		private bool ordersPlaced = false;
		private bool triggerSet = false;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"";
				Name = "ORBBreakout";
				Calculate = Calculate.OnEachTick;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IncludeCommission = true;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;

				triggerTime = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (State == State.Realtime)
			{
				ordersPlaced = false;
				triggerSet = false;
			}
		}

		protected override void OnBarUpdate()
		{
			// Ensure strategy runs only during real-time or historical data
			if (State != State.Realtime && State != State.Historical)
				return;

			if (Bars.IsFirstBarOfSession)
			{
				ordersPlaced = false;
				triggerSet = false;
			}

			// Check if it's 9:29 AM EST to set trigger price
			if (!triggerSet && !ordersPlaced && Time[0].TimeOfDay == triggerTime.TimeOfDay)
			{
				triggerPrice = Close[1];
				triggerSet = true;
				Print(Time[0] + " [ORB Breakout] Trigger Price Set: " + triggerPrice);
			}

			if (triggerSet && !ordersPlaced)
			{
				double buyStopPrice = triggerPrice + POINTS;
				double sellStopPrice = triggerPrice - POINTS;

				// Place Buy Stop Order if price breaks above buyStopPrice
				if (High[0] >= buyStopPrice && Position.MarketPosition == MarketPosition.Flat)
				{
					Print(Time[0] + " [ORB Breakout] Buy Stop Order Placed: " + buyStopPrice);
					EnterLong("Long Breakout");
					SetProfitTarget("Long Breakout", CalculationMode.Ticks, TICK_TARGET);
					ordersPlaced = true;
				}

				// Place Sell Stop Order if price breaks below sellStopPrice
				if (Low[0] <= sellStopPrice && Position.MarketPosition == MarketPosition.Flat)
				{
					Print(Time[0] + " [ORB Breakout] Sell Stop Order Placed: " + sellStopPrice);
					EnterShort("Short Breakout");
					SetProfitTarget("Short Breakout", CalculationMode.Ticks, TICK_TARGET);
					ordersPlaced = true;
				}
			}
		}
	}
}
