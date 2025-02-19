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

#region TODO
/*
TODO: [4] Create logic to get time to bar closed from the newstimes to place order
TODO: [5] Create prop for time to bar closed
*/
#endregion
//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class NewsORB : Strategy
	{
		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Trigger Time", Description = "Time to trigger the trade", Order = 9, GroupName = "1. Main Parameters")]
		public DateTime TriggerTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Quantity", Description = "Quantity of contracts to trade", Order = 10, GroupName = "1. Main Parameters")]
		public int Quantity { get; set; } = 5;

		[NinjaScriptProperty]
		[Display(Name = "Time to Bar Closed", Description = "Time to bar closed", Order = 11, GroupName = "1. Main Parameters")]
		public int TimeToBarClosed { get; set; } = 15;

		[NinjaScriptProperty]
		[Display(Name = "News Bracket Size", Description = "Size of the news bracket", Order = 10, GroupName = "1. Main Parameters")]
		public double NewsBracketSize { get; set; } = 27.5;

		[NinjaScriptProperty]
		[Display(Name = "Profit Target", Description = "Profit target", Order = 11, GroupName = "1. Main Parameters")]
		public double ProfitTarget { get; set; } = 20;
		#endregion

		#region Variables
		private Order longStopEntry, shortStopEntry;
		private string ocoString;
		private bool ordersPlaced = false;
		private bool takeprofitPlaced = false;
		private bool triggerSet = false;
		private bool timerElapsed = false;
		private double triggerPrice;
		private System.Windows.Threading.DispatcherTimer timer;
		private DateTime now = Core.Globals.Now;
		private TimeSpan barTimeLeft;
		#endregion
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"ORB Strategy on News Events";
				Name = "NewsORB";
				Calculate = Calculate.OnEachTick;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				BarsRequiredToTrade = 20;
				IsUnmanaged = true;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;

				TriggerTime = DateTime.Parse("08:30", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (State == State.Historical)
			{
				ChartControl.Dispatcher.InvokeAsync(() =>
					{
						timer = new System.Windows.Threading.DispatcherTimer { Interval = new TimeSpan(0, 0, 1), IsEnabled = true };
						timer.Tick += OnTimerTick;
					});
			}
			else if (State == State.Realtime)
			{
				ordersPlaced = false;
				triggerSet = false;
				timerElapsed = false;
				takeprofitPlaced = false;
				RemoveDrawObject("ORBUpper");
				RemoveDrawObject("ORBLower");
			}
		}

		protected override void OnBarUpdate()
		{
			// Ensure strategy runs only during real-time or historical data
			if (State != State.Realtime && State != State.Historical)
				return;

			// Check if it's 9:29 AM EST to set trigger price
			if (!triggerSet && !ordersPlaced && Time[0].TimeOfDay == TriggerTime.TimeOfDay)
			{
				triggerSet = true;
			}

			if (triggerSet && !ordersPlaced && !timerElapsed)
			{
				triggerPrice = Close[0];
				if (barTimeLeft != null && barTimeLeft.TotalSeconds <= TimeToBarClosed && barTimeLeft.TotalSeconds > 0)
				{
					Print(Time[0] + " [News ORB] Timer Elapsed");
					Print(Time[0] + " [News ORB] Trigger Price Set: " + triggerPrice);
					timerElapsed = true;
				}
			}

			if (triggerSet && !ordersPlaced && timerElapsed)
			{
				// generate a unique oco string based on the time
				// oco means that when one entry fills, the other entry is automatically cancelled
				// in OnExecution we will protect these orders with our version of a stop loss and profit target when one of the entry orders fills
				ocoString = string.Format("unmanagedentryoco{0}", DateTime.Now.ToString("hhmmssffff"));
				longStopEntry = SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.StopMarket, Quantity, 0, triggerPrice + NewsBracketSize, ocoString, "longStopEntry");
				shortStopEntry = SubmitOrderUnmanaged(0, OrderAction.SellShort, OrderType.StopMarket, Quantity, 0, triggerPrice - NewsBracketSize, ocoString, "shortStopEntry");
				ordersPlaced = true;
				Draw.HorizontalLine(this, "ORBUpper", triggerPrice + NewsBracketSize, Brushes.Lime);
				Draw.HorizontalLine(this, "ORBLower", triggerPrice - NewsBracketSize, Brushes.Red);
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if (State == State.Realtime)
			{
				if (timer != null && !timer.IsEnabled)
					timer.IsEnabled = true;
				barTimeLeft = Bars.GetTime(Bars.Count - 1).Subtract(Now);

				if (triggerSet && !ordersPlaced)
				{
					Print(Time[0] + " [News ORB] Bar Time Left: " + barTimeLeft);
				}
			}
		}

		private DateTime Now
		{
			get
			{
				now = (Cbi.Connection.PlaybackConnection != null ? Cbi.Connection.PlaybackConnection.Now : Core.Globals.Now);

				if (now.Millisecond > 0)
					now = Core.Globals.MinDate.AddSeconds((long)Math.Floor(now.Subtract(Core.Globals.MinDate).TotalSeconds));

				return now;
			}
		}

		protected override void OnExecutionUpdate(Cbi.Execution execution, string executionId, double price, int quantity,
	Cbi.MarketPosition marketPosition, string orderId, DateTime time)
		{
			// if the long entry filled, place a profit target and stop loss to protect the order
			if (longStopEntry != null && execution.Order == longStopEntry && !takeprofitPlaced)
			{
				// generate a new oco string for the protective stop and target
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				// submit a protective profit target order
				SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.Limit, Quantity, triggerPrice + NewsBracketSize + ProfitTarget, 0, ocoString, "longProfitTarget");
				takeprofitPlaced = true;
			}
			// reverse the order types and prices for a short
			else if (shortStopEntry != null && execution.Order == shortStopEntry && !takeprofitPlaced)
			{
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.Limit, Quantity, triggerPrice - NewsBracketSize - ProfitTarget, 0, ocoString, "shortProfitTarget");
				takeprofitPlaced = true;
			}

			// I didn't use Order variables to track the stop loss and profit target, but I could have
			// Instead, I detect the orders when the fill by their signalName
			// (the execution.Name is the signalName provided with the order)

			// when the long profit or stop fills, set the long entry to null to allow a new entry
			else if (execution.Name == "longProfitTarget" || execution.Name == "longStopLoss" || execution.Name == "shortProfitTarget" || execution.Name == "shortStopLoss")
			{
				longStopEntry = null;
				shortStopEntry = null;
			}
		}
	}
}
