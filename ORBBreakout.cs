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

/* 
TODO: Add maximum allowed drawdown

TODO: Set up check so if we break trigger after BE time, and we go back under, close at the loss? So bar [1] crosses over trigger and then bar[0] goes under, just exit?
[ ] 2024-07-01 9:31:00 AM [ORB Breakout] Sell Stop Order Placed: 19962.25

TODO: If we get just to profit (near 0.5 points) then SL at 1 point positive of entry profit?
[ ] 2024-11-18 9:40:00 AM [ORB Breakout - Double Entry] Buy Stop Order Placed: 20566.25
[ ] 2024-11-19 9:35:00 AM [ORB Breakout - Double Entry] Sell Stop Order Placed: 20512.5
[ ] 2024-11-06 9:31:00 AM [ORB Breakout - Double Entry] Sell Stop Order Placed: 20676
[ ] 2024-11-15 9:31:00 AM [ORB Breakout] Buy Stop Order Placed: 20768
[ ] 2024-11-13

[ ] 11 -01 - Failure
*/

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class ORBBreakout : Strategy
	{
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "DefaultContractSize", Description = "Number of contracts to trade", Order = 2, GroupName = "1. Main Parameters")]
		public int DefaultContractSize { get; set; } = 1;

		[NinjaScriptProperty]
		[Display(Name = "Double Entry", Description = "If true, the strategy will enter a double entry mode where it will enter a trade with a larger SL and more size.", Order = 3, GroupName = "1. Main Parameters")]
		public bool DoubleEntry { get; set; } = false;
		#endregion

		#region Variables
		private DateTime triggerTime;
		private DateTime triggerDoubleEndTime;
		private DateTime triggerBETime;
		private double triggerPrice;
		private const double POINTS = 9;      // Distance for breakout in points
		private const int TICK_TARGET = 22;   // Profit target in ticks
		private bool ordersPlaced = false;
		private bool triggerSet = false;
		private bool longBreakoutTriggered = false;
		private bool shortBreakoutTriggered = false;
		private bool longBreakoutSet = false;
		private bool shortBreakoutSet = false;
		private bool triggerBETimeSet = false;
		private int barTradeClosed = 0;
		#endregion
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

				triggerTime = DateTime.Parse("09:31", System.Globalization.CultureInfo.InvariantCulture);
				triggerDoubleEndTime = DateTime.Parse("09:42", System.Globalization.CultureInfo.InvariantCulture);
				triggerBETime = DateTime.Parse("09:33", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (State == State.Realtime)
			{
				ordersPlaced = false;
				triggerSet = false;
				longBreakoutTriggered = false;
				shortBreakoutTriggered = false;
				longBreakoutSet = false;
				shortBreakoutSet = false;
				triggerBETimeSet = false;
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
				longBreakoutTriggered = false;
				shortBreakoutTriggered = false;
				longBreakoutSet = false;
				shortBreakoutSet = false;
				triggerBETimeSet = false;
				SetProfitTarget("Long Breakout", CalculationMode.Ticks, TICK_TARGET);
				SetProfitTarget("Short Breakout", CalculationMode.Ticks, TICK_TARGET);
				SetProfitTarget("Long Breakout Second Entry", CalculationMode.Ticks, TICK_TARGET);
				SetProfitTarget("Short Breakout Second Entry", CalculationMode.Ticks, TICK_TARGET);
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

				// Draw lines for breakout levels
				Draw.HorizontalLine(this, "BuyStopLine", buyStopPrice, Brushes.LightGreen, DashStyleHelper.Solid, 1);
				Draw.HorizontalLine(this, "SellStopLine", sellStopPrice, Brushes.Pink, DashStyleHelper.Solid, 1);
				Draw.HorizontalLine(this, "TriggerPrice", triggerPrice, Brushes.Gray, DashStyleHelper.Solid, 1);

				// Place Buy Stop Order if price breaks above buyStopPrice
				if (High[0] > buyStopPrice && Position.MarketPosition == MarketPosition.Flat)
				{
					Print(Time[0] + " [ORB Breakout] Buy Stop Order Placed: " + buyStopPrice);
					EnterLong(DefaultContractSize, "Long Breakout");
					SetProfitTarget("Long Breakout", CalculationMode.Price, buyStopPrice + TICK_TARGET * 0.25);
					ordersPlaced = true;
					longBreakoutTriggered = true;
				}

				// Place Sell Stop Order if price breaks below sellStopPrice
				if (Low[0] < sellStopPrice && Position.MarketPosition == MarketPosition.Flat)
				{
					Print(Time[0] + " [ORB Breakout] Sell Stop Order Placed: " + sellStopPrice);
					EnterShort(DefaultContractSize, "Short Breakout");
					SetProfitTarget("Short Breakout", CalculationMode.Price, sellStopPrice - TICK_TARGET * 0.25);
					ordersPlaced = true;
					shortBreakoutTriggered = true;
				}
			}

			if (Time[0].TimeOfDay == triggerBETime.TimeOfDay && Position.MarketPosition != MarketPosition.Flat && !triggerBETimeSet)
			{
				double beOffset = 0.5;
				triggerBETimeSet = true;
				Print(Time[0] + " [ORB Breakout] Trigger BE Time: " + triggerBETime);
				SetProfitTarget("Long Breakout", CalculationMode.Price, Position.AveragePrice + beOffset);
				SetProfitTarget("Short Breakout", CalculationMode.Price, Position.AveragePrice - beOffset);
			}

			if (triggerSet && longBreakoutSet && !longBreakoutTriggered && DoubleEntry && Time[0].TimeOfDay <= triggerDoubleEndTime.TimeOfDay)
			{
				double buyStopPrice = triggerPrice + POINTS;

				// Place Buy Stop Order if price breaks above buyStopPrice
				if (High[0] > buyStopPrice && Position.MarketPosition == MarketPosition.Flat)
				{
					Print(Time[0] + " [ORB Breakout - Double Entry] Buy Stop Order Placed: " + buyStopPrice);
					EnterLong(DefaultContractSize, "Long Breakout Second Entry");
					SetProfitTarget("Long Breakout Second Entry", CalculationMode.Price, buyStopPrice + TICK_TARGET * 0.25);
					ordersPlaced = true;
					longBreakoutTriggered = true;
				}
			}

			if (triggerSet && shortBreakoutSet && !shortBreakoutTriggered && DoubleEntry && Time[0].TimeOfDay <= triggerDoubleEndTime.TimeOfDay)
			{
				double sellStopPrice = triggerPrice - POINTS;

				// Place Sell Stop Order if price breaks below sellStopPrice
				if (Low[0] < sellStopPrice && Position.MarketPosition == MarketPosition.Flat)
				{
					Print(Time[0] + " [ORB Breakout - Double Entry] Sell Stop Order Placed: " + sellStopPrice);
					EnterShort(DefaultContractSize, "Short Breakout Second Entry");
					SetProfitTarget("Short Breakout Second Entry", CalculationMode.Price, sellStopPrice - TICK_TARGET * 0.25);
					ordersPlaced = true;
					shortBreakoutTriggered = true;
				}
			}
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			// After a long trade closes, place the short breakout order
			if (execution.Order.FromEntrySignal == "Long Breakout" && execution.Order.OrderState == OrderState.Filled && (execution.Order.Name.Contains("Profit target") || execution.Order.Name.Contains("Stop loss")) && !shortBreakoutSet && !shortBreakoutTriggered)
			{
				shortBreakoutSet = true;
				barTradeClosed = CurrentBar;
			}

			// After a short trade closes, place the long breakout order
			if (execution.Order.FromEntrySignal == "Short Breakout" && execution.Order.OrderState == OrderState.Filled && (execution.Order.Name.Contains("Profit target") || execution.Order.Name.Contains("Stop loss")) && !longBreakoutSet && !longBreakoutTriggered)
			{
				longBreakoutSet = true;
				barTradeClosed = CurrentBar;
			}
		}
	}
}
