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
using SharpDX;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using static NinjaTrader.CQG.ProtoBuf.MarketDataSubscription.Types;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using DayOfWeek = System.DayOfWeek;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	#region TODO
	/*

	*/
	#endregion
	public class StrategyTemplate : Strategy
	{
		#region Properties
		#region Main Parameters
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Number of Contracts", Description = "Number of contracts to trade", Order = 1, GroupName = "1. Main Parameters")]
		public int TradeQuantity
		{ get; set; } = 5;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Max Daily Gain", Description = "Maximum daily gain before trading stops", Order = 2, GroupName = "1. Main Parameters")]
		public double MaxGainRatio
		{ get; set; } = 200;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Max Daily Loss", Description = "Maximum loss before trading stops", Order = 3, GroupName = "1. Main Parameters")]
		public double MaxLossRatio
		{ get; set; } = 150;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Consecutive Losses", Description = "Maximum number of consecutive losses before trading stops", Order = 4, GroupName = "1. Main Parameters")]
		public int MaxConsecutiveLosses
		{ get; set; } = 3;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Loss Cut Off", Description = "Loss cut off level before trading stops", Order = 5, GroupName = "1. Main Parameters")]
		public double LossCutOffRatio
		{ get; set; } = 60;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Full Take Profit", Description = "Take profit level for full position exit", Order = 7, GroupName = "1. Main Parameters")]
		public double FullTakeProfit
		{ get; set; } = 100;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Full Stop Loss", Description = "Stop loss level for full position exit", Order = 8, GroupName = "1. Main Parameters")]
		public double FullStopLoss
		{ get; set; } = 60;
		#endregion

		#region Core Engine Parameters
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Conversion Factor ES", Description = "Conversion factor for ES", Order = 3, GroupName = "2. Core Engine Parameters")]
		public double ConversionFactorES
		{ get; set; } = 0.33;
		#endregion

		#region Dynamic TP/SL Parameters
		[NinjaScriptProperty]
		[Display(Name = "Enable Dynamic SL", Description = "Enables dynamic stop loss functionality", Order = 0, GroupName = "2. Dynamic TP/SL")]
		public bool EnableDynamicSL
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Profit To Move SL To Mid", Description = "Profit level required to move stop loss to mid", Order = 1, GroupName = "2. Dynamic TP/SL")]
		public double ProfitToMoveSLToMid
		{ get; set; } = 20;

		[NinjaScriptProperty]
		[Range(-100, 100)]
		[Display(Name = "SL Offset", Description = "Offset for stop loss level", Order = 2, GroupName = "2. Dynamic TP/SL")]
		public double SLOffset
		{ get; set; } = 4;
		#endregion

		#region Trim Settings
		[NinjaScriptProperty]
		[Display(Name = "Enable Dynamic Trim", Description = "Enables dynamic trim functionality", Order = 1, GroupName = "3. Trim Settings")]
		public bool EnableDynamicTrim
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Display(Name = "Enable Secondary Trim", Description = "Enables secondary trim functionality", Order = 2, GroupName = "3. Trim Settings")]
		public bool EnableSecondaryTrim
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Trim Percent", Description = "Percentage of position to trim at first target", Order = 3, GroupName = "3. Trim Settings")]
		public double TrimPercent
		{ get; set; } = 30;

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Secondary Trim Percent", Description = "Percentage of position to trim at second target", Order = 4, GroupName = "3. Trim Settings")]
		public double SecondaryTrimPercent
		{ get; set; } = 30;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Trim Take Profit", Description = "Take profit level for partial position exit", Order = 5, GroupName = "3. Trim Settings")]
		public double TrimTakeProfit
		{ get; set; } = 50;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Secondary Trim Take Profit", Description = "Second take profit level for partial position exit", Order = 6, GroupName = "3. Trim Settings")]
		public double SecondaryTrimTakeProfit
		{ get; set; } = 75;
		#endregion

		#region Time Parameters
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Start Time", Description = "Session start time", Order = 1, GroupName = "4. Time Parameters")]
		public DateTime StartTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "End Time", Description = "Session end time", Order = 2, GroupName = "4. Time Parameters")]
		public DateTime EndTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Blackout Start Time", Description = "Blackout start time", Order = 3, GroupName = "4. Time Parameters")]
		public DateTime BlackoutStartTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Blackout End Time", Description = "Blackout end time", Order = 4, GroupName = "4. Time Parameters")]
		public DateTime BlackoutEndTime
		{ get; set; }
		#endregion

		#region Extra Parameters	
		[NinjaScriptProperty]
		[Display(Name = "Disable Graphics", Description = "Disables drawing of chart graphics", Order = 1, GroupName = "9. Extra Parameters")]
		public bool DisableGraphics
		{ get; set; } = false;
		#endregion
		#endregion

		#region Variables
		private double currentPnL;
		private int consecutiveLosses = 0;
		private int lastTradeChecked = -1;
		private double currentTradePnL = 0;
		private int partialTradeQty = 0;
		private double partialTradePnL = 0;
		private int lastTimeSession = 0;
		private bool EnableTrading = false;
		private bool BuyTradeEnabled = false;
		private bool SellTradeEnabled = false;
		private bool CloseBuyTrade = false;
		private bool CloseSellTrade = false;
		private double tradeCompleteTPPoints = 0;
		private double tradeCompleteSLPoints = 0;
		private int tradeCompleteBar = 0;
		private int tradeTPHitBar = 0;
		private int tradeSLHitBar = 0;

		private double MaxGain;
		private double MaxLoss;
		private double LossCutOff;
		private double tradeSL = 0;

		private Order entryOrder;
		private Order entryOrderTrim;
		private Order entryOrderSecondaryTrim;
		private Order entryOrderShort;
		private Order entryOrderTrimShort;
		private Order entryOrderSecondaryTrimShort;

		private bool trimOrderFilled = false;
		private bool trimOrderShortFilled = false;


		private bool newTradeCalculated = false;
		private bool partialTradeCalculated = false;
		private bool newTradeExecuted = false;

		private int barsInTrade = 0;
		private bool MiniMode = false;

		private bool orderFilledLong = false;
		private bool orderFilledShort = false;

		private List<TradeExecutionDetailsStrategy> tradeExecutionDetails;

		#endregion

		public StrategyTemplate()
		{
			string productName = "StrategyTemplate";
			VendorLicense("TradingLevelsAlgo", productName, "www.TradingLevelsAlgo.com", "tradinglevelsalgo@gmail.com", null);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Strategy Template";
				Name = "StrategyTemplate";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.UniqueEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.WaitUntilFlatSynchronizeAccount;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;

				StartTime = DateTime.Parse("12:00", System.Globalization.CultureInfo.InvariantCulture);
				EndTime = DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				BlackoutStartTime = DateTime.Parse("09:20", System.Globalization.CultureInfo.InvariantCulture);
				BlackoutEndTime = DateTime.Parse("09:40", System.Globalization.CultureInfo.InvariantCulture);


				tradeExecutionDetails = new List<TradeExecutionDetailsStrategy>();
			}
			else if (State == State.DataLoaded)
			{
				ClearOutputWindow();
				if (!DisableGraphics)
				{

				}

				if (Instrument.MasterInstrument.Name == "MNQ" || Instrument.MasterInstrument.Name == "MES")
				{
					MiniMode = false;
				}
				else if (Instrument.MasterInstrument.Name == "NQ" || Instrument.MasterInstrument.Name == "ES")
				{
					MiniMode = true;
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || BarsInProgress != 0)
				return;

			#region Initialize/Time Setup
			if (Bars.IsFirstBarOfSession)
			{
				#region Reset PnL Variables
				currentPnL = 0;
				consecutiveLosses = 0;
				tradeExecutionDetails.Clear();
				#endregion

				#region Update Objects
				RemoveDrawObject("TargetLevel" + "ORB High");
				RemoveDrawObject("TargetLevel" + "ORB Low");
				RemoveDrawObject("Label" + "ORB High");
				RemoveDrawObject("Label" + "ORB Low");
				#endregion

				EnableTrading = true;
				lastTimeSession = 0;
				tradeCompleteSLPoints = 0;
				tradeCompleteTPPoints = 0;

				Print(Time[0] + " ******** TRADING ENABLED ******** ");
			}

			GetTimeSessionVariables();
			#endregion
			#region Core Engine
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				trimOrderFilled = false;
				trimOrderShortFilled = false;
			}
			#endregion
			#region PnL Calculation
			#region Dynamic Gain/Loss
			MaxGain = MaxGainRatio * TradeQuantity * (MiniMode ? 10 : 1);
			MaxLoss = MaxLossRatio * TradeQuantity * -1 * (MiniMode ? 10 : 1);
			LossCutOff = LossCutOffRatio * TradeQuantity * -1 * (MiniMode ? 10 : 1);
			#endregion

			#region Trade Updates
			if (newTradeExecuted)
			{
				foreach (var detail in tradeExecutionDetails)
				{
					string tradeCloseType = "";
					string tradeDistance = "";
					if (detail.TradeExecuteType == "Profit target")
					{
						tradeCloseType = " - TP";
						if (detail.TradeExecName == "Long" || detail.TradeExecName == "LongTrim")
							tradeDistance = " | Missed Points: " + (High[0] - detail.TradeExecPrice);
						else if (detail.TradeExecName == "Short" || detail.TradeExecName == "ShortTrim")
							tradeDistance = " | Missed Points: " + (detail.TradeExecPrice - Low[0]);
					}
					else if (detail.TradeExecuteType == "Stop loss")
					{
						tradeCloseType = " - SL";
						if (detail.TradeExecName == "Long" || detail.TradeExecName == "LongTrim")
						{
							tradeDistance = " | Loss Averted: " + (detail.TradeExecPrice - Low[0]);
						}
						else if (detail.TradeExecName == "Short" || detail.TradeExecName == "ShortTrim")
						{
							tradeDistance = " | Loss Averted: " + (High[0] - detail.TradeExecPrice);
						}
					}
					else if (detail.TradeExecuteType == "Buy to cover")
					{
						detail.TradeExecuteType = "Short CLOSED";
					}
					else if (detail.TradeExecuteType == "Sell")
					{
						detail.TradeExecuteType = "Long CLOSED";
					}

					tradeCompleteTPPoints = 0;
					tradeCompleteSLPoints = 0;
					if (detail.TradeExecuteType == "Profit target")
					{
						string tradeInfo = "";
						if (detail.TradeExecName == "LongTrim")
						{
							tradeInfo = "Long TRIMMED at: ";
						}
						else if (detail.TradeExecName == "ShortTrim")
						{
							tradeInfo = "Short TRIMMED at: ";
						}
						else if (detail.TradeExecName == "Long" || detail.TradeExecName == "Short")
						{
							tradeInfo = detail.TradeExecName + " CLOSED (TP) at: ";
							tradeCompleteBar = CurrentBar;
							tradeTPHitBar = CurrentBar;
							tradeCompleteTPPoints = RoundToNearestTick(partialTradePnL / partialTradeQty / Bars.Instrument.MasterInstrument.PointValue);
						}
					}
					else if (detail.TradeExecuteType == "Stop loss")
					{
						string tradeInfo = "";
						if (detail.TradeExecName == "Long" || detail.TradeExecName == "LongTrim")
						{
							tradeInfo = "Long CLOSED (SL) at: ";
						}
						else if (detail.TradeExecName == "Short" || detail.TradeExecName == "ShortTrim")
						{
							tradeInfo = "Short CLOSED (SL) at: ";
						}
						tradeCompleteBar = CurrentBar;
						tradeSLHitBar = CurrentBar;
					}
					else
					{
						tradeCompleteBar = CurrentBar;
						if (RoundToNearestTick(partialTradePnL / partialTradeQty / Bars.Instrument.MasterInstrument.PointValue) > 0)
						{
							tradeCompleteTPPoints = RoundToNearestTick(partialTradePnL / partialTradeQty / Bars.Instrument.MasterInstrument.PointValue);
							tradeTPHitBar = CurrentBar;
						}
						else
						{
							tradeSLHitBar = CurrentBar;
						}
					}
				}
				tradeExecutionDetails.Clear();
				newTradeExecuted = false;
			}

			if (partialTradeCalculated)
			{

				Print(Time[0] + " [PNL UPDATE] CURRENT TRADE PnL: $" + partialTradePnL +
					" | Current PnL: $" + currentPnL +
					" | Points : " + RoundToNearestTick(partialTradePnL / partialTradeQty / Bars.Instrument.MasterInstrument.PointValue) +
					" | Quantity: " + partialTradeQty);
				partialTradePnL = 0;
				partialTradeQty = 0;
				partialTradeCalculated = false;
			}

			if (Position.MarketPosition == MarketPosition.Flat && newTradeCalculated)
			{
				tradeCompleteSLPoints = RoundToNearestTick(currentTradePnL / TradeQuantity / Bars.Instrument.MasterInstrument.PointValue);

				if (currentTradePnL < LossCutOff)
				{
					consecutiveLosses++;
					Print(Time[0] + " [PNL TRACK] CONSECUTIVE LOSSES: " + consecutiveLosses);
				}
				else if (currentTradePnL >= 0 && consecutiveLosses > 0)
				{
					consecutiveLosses = 0; // Reset the count on a non-loss trade
					Print(Time[0] + " [PNL TRACK] CONSECUTIVE LOSSES RESET");
				}
				// Check if there have been three consecutive losing trades
				if (consecutiveLosses >= MaxConsecutiveLosses)
				{
					EnableTrading = false;
					Print(Time[0] + $" ******** TRADING DISABLED ({consecutiveLosses} losses in a row) ******** : $" + currentPnL + " | Account: " + Account.Name);
				}

				Print(Time[0] + " [PNL UPDATE] COMPLETED TRADE PnL: $" + currentTradePnL + " | Total PnL: $" + currentPnL);
				currentTradePnL = 0;
				newTradeCalculated = false;
				barsInTrade = 0;
				tradeSL = 0;
			}

			if (Position.MarketPosition != MarketPosition.Flat)
			{
				barsInTrade++;
			}
			#endregion

			#region Trading Cutoff
			double roundingStopPercent = 1 - 5 / 100;
			if ((currentPnL < MaxLoss * roundingStopPercent || currentPnL > MaxGain * roundingStopPercent) && EnableTrading)
			{
				EnableTrading = false;
				Print(Time[0] + " ******** TRADING DISABLED ******** : $" + currentPnL + " | Account: " + Account.Name);
			}

			double realtimPnL = Math.Round(currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]), 1);

			// if in a position and the realized day's PnL plus the position PnL is greater than the loss limit then exit the order
			double roundingStopPercentLive = 1 - 5 / 2 / 100;
			if ((realtimPnL <= MaxLoss * roundingStopPercentLive || realtimPnL >= MaxGain * roundingStopPercentLive) && EnableTrading)
			{
				EnableTrading = false;
				Print(Time[0] + " ******** TRADING DISABLED (mid-trade) ******** : $" + realtimPnL + " | Account: " + Account.Name);
			}
			#endregion
			#endregion
			#region Trading Signals
			BuyTradeEnabled = false;
			SellTradeEnabled = false;
			if (EnableTrading)
			{

				BuyTradeEnabled = true;
				SellTradeEnabled = true;
			}
			#endregion
			#region Trading Management
			#region TP/SL Management
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				SetStopLoss("Long", CalculationMode.Ticks, FullStopLoss / TickSize, false);
				SetStopLoss("LongTrim", CalculationMode.Ticks, FullStopLoss / TickSize, false);
				SetStopLoss("LongSecondaryTrim", CalculationMode.Ticks, FullStopLoss / TickSize, false);

				SetStopLoss("Short", CalculationMode.Ticks, FullStopLoss / TickSize, false);
				SetStopLoss("ShortTrim", CalculationMode.Ticks, FullStopLoss / TickSize, false);
				SetStopLoss("ShortSecondaryTrim", CalculationMode.Ticks, FullStopLoss / TickSize, false);

				SetProfitTarget("Long", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
				SetProfitTarget("LongTrim", CalculationMode.Ticks, GetConvertedValue(TrimTakeProfit) / TickSize, false);
				SetProfitTarget("LongSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(SecondaryTrimTakeProfit) / TickSize, false);

				SetProfitTarget("Short", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
				SetProfitTarget("ShortTrim", CalculationMode.Ticks, GetConvertedValue(TrimTakeProfit) / TickSize, false);
				SetProfitTarget("ShortSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(SecondaryTrimTakeProfit) / TickSize, false);
			}
			else if (Position.MarketPosition == MarketPosition.Long)
			{
				#region SL Management
				#endregion

				#region TP Management
				#endregion
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				#region SL Management
				#endregion

				#region TP Management
				#endregion
			}
			#endregion

			#region Close/Trim Trades
			string tradeCloseReason = "";
			CloseBuyTrade = false;
			CloseSellTrade = false;

			if (!EnableTrading)
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					ExitLong();
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					ExitShort();
				}
			}
			else if (CloseBuyTrade)
			{
				if (entryOrder != null && entryOrder.OrderState == OrderState.Filled)
				{
					Print(Time[0] + " [TRADE CLOSE - " + tradeCloseReason + "] Long Order CLOSED: " + Close[0]);
				}
				if (Position.MarketPosition == MarketPosition.Long)
				{
					ExitLong();
				}
			}
			else if (CloseSellTrade)
			{
				if (entryOrderShort != null && entryOrderShort.OrderState == OrderState.Filled)
				{
					Print(Time[0] + " [TRADE CLOSE - " + tradeCloseReason + "] Short Order CLOSED: " + Close[0]);
				}
				if (Position.MarketPosition == MarketPosition.Short)
				{
					ExitShort();
				}
			}
			#endregion

			#region Buy/Sell Orders
			int mainTradeQuantity = TradeQuantity;
			int trimTradeQuantity = (int)Math.Round(TradeQuantity * TrimPercent / 100, 0);
			int secondaryTrimTradeQuantity = (int)Math.Round(TradeQuantity * SecondaryTrimPercent / 100, 0);

			if (TradeQuantity < 4)
				EnableSecondaryTrim = false;

			if (EnableDynamicTrim)
				mainTradeQuantity = TradeQuantity - trimTradeQuantity;

			if (EnableSecondaryTrim && TradeQuantity > 3)
				mainTradeQuantity = TradeQuantity - trimTradeQuantity - secondaryTrimTradeQuantity;

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (BuyTradeEnabled)
				{

				}
				else if (SellTradeEnabled)
				{

				}
			}
			#endregion

			#endregion
			#region Dashboard
			string dashBoard =
				$"PnL: $"
				+ realtimPnL.ToString()
				+ " | Trading: "
				+ (EnableTrading ? "Active" : "Off");

			if (!DisableGraphics)
			{
				Draw.TextFixed(this, "Dashboard", dashBoard, TextPosition.BottomRight);
			}
			#endregion

		}

		protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
		{
			// One time only, as we transition from historical
			// Convert any old historical order object references to the live order submitted to the real-time account
			if (entryOrder != null && entryOrder.IsBacktestOrder && State == State.Realtime)
				entryOrder = GetRealtimeOrder(entryOrder);

			if (entryOrderTrim != null && entryOrderTrim.IsBacktestOrder && State == State.Realtime)
				entryOrderTrim = GetRealtimeOrder(entryOrderTrim);

			if (entryOrderSecondaryTrim != null && entryOrderSecondaryTrim.IsBacktestOrder && State == State.Realtime)
				entryOrderSecondaryTrim = GetRealtimeOrder(entryOrderSecondaryTrim);

			if (entryOrderShort != null && entryOrderShort.IsBacktestOrder && State == State.Realtime)
				entryOrderShort = GetRealtimeOrder(entryOrderShort);

			if (entryOrderTrimShort != null && entryOrderTrimShort.IsBacktestOrder && State == State.Realtime)
				entryOrderTrimShort = GetRealtimeOrder(entryOrderTrimShort);

			if (entryOrderSecondaryTrimShort != null && entryOrderSecondaryTrimShort.IsBacktestOrder && State == State.Realtime)
				entryOrderSecondaryTrimShort = GetRealtimeOrder(entryOrderSecondaryTrimShort);

			if (order.Name == "Long")
			{
				if (orderState == OrderState.Filled)
				{
					Print(Time[0] + " [TRADE FILL] LONG FILLED: " + averageFillPrice + " | Bars Since Last Trade: " + (CurrentBar - tradeCompleteBar));
					orderFilledLong = true;
					orderFilledShort = false;
				}
			}

			if (order.Name == "Short")
			{
				if (orderState == OrderState.Filled)
				{
					Print(Time[0] + " [TRADE FILL] SHORT FILLED: " + averageFillPrice + " | Bars Since Last Trade: " + (CurrentBar - tradeCompleteBar));
					orderFilledShort = true;
					orderFilledLong = false;
				}
			}

			if (orderState == OrderState.Rejected || orderState == OrderState.Cancelled)
			{
				if (order.Name == "Stop loss" && order.OrderState == OrderState.Rejected)
				{
					ExitLong();
					ExitShort();
				}
			}
		}

		// Execution Update
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			#region Trade Closed
			if (
				execution.Order.OrderState == OrderState.Filled
				&& (
					execution.Order.Name.Contains("Stop loss")
					|| execution.Order.Name.Contains("Profit target")
					|| execution.Order.Name.Contains("to cover")
					|| execution.Order.Name.Contains("Sell")
					|| execution.Order.Name.Contains("Buy")
				)
			)
			{
				newTradeExecuted = true;
				if (!tradeExecutionDetails.Any(detail => detail.TradeExecName == execution.Order.FromEntrySignal) &&
					!(execution.Order.Name.Contains("Stop loss") && tradeExecutionDetails.Any(detail => detail.TradeExecuteType.Contains("Stop loss"))))
				{
					tradeExecutionDetails.Add(new TradeExecutionDetailsStrategy(price, execution.Order.Name, execution.Order.FromEntrySignal));
				}

				if (entryOrderTrim != null && execution.Order.FromEntrySignal == entryOrderTrim.Name && execution.Order.Name.Contains("Profit target"))
				{
					trimOrderFilled = true;
				}
				else if (entryOrderTrimShort != null && execution.Order.FromEntrySignal == entryOrderTrimShort.Name && execution.Order.Name.Contains("Profit target"))
				{
					trimOrderShortFilled = true;
				}
			}
			#endregion

			#region PnL Calculation
			if (SystemPerformance.AllTrades.Count > 0)
			{
				Cbi.Trade lastTrade = SystemPerformance.AllTrades[
					SystemPerformance.AllTrades.Count - 1
				];

				// Sum the profits of trades with similar exit times
				double execTradePnL = lastTrade.ProfitCurrency;
				int execQty = lastTrade.Quantity;
				DateTime exitTime = lastTrade.Exit.Time;
				if (lastTrade.TradeNumber > lastTradeChecked)
				{
					for (int i = SystemPerformance.AllTrades.Count - 2; i >= 0; i--)
					{
						Cbi.Trade trade = SystemPerformance.AllTrades[i];
						if (Math.Abs((trade.Exit.Time - exitTime).TotalSeconds) <= 10 && trade.TradeNumber > lastTradeChecked)
						{
							execTradePnL += trade.ProfitCurrency;
							execQty += trade.Quantity;
						}
						else
						{
							break; // Exit the loop if the exit time is different
						}
					}
					lastTradeChecked = lastTrade.TradeNumber;
					currentPnL += execTradePnL;
					currentTradePnL += execTradePnL;
					partialTradePnL += execTradePnL;
					partialTradeQty += execQty;
					newTradeCalculated = true;
					partialTradeCalculated = true;
				}
			}
			#endregion
		}

		public override string DisplayName
		{
			get
			{
				if (State == State.SetDefaults)
					return "StrategyTemplate";
				else
					return "";
			}
		}

		#region Functions
		#region Protective Functions

		#endregion
		#region Time Functions
		private void GetTimeSessionVariables()
		{
			#region Load Time Session Variables
			TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;

			if (barTime >= StartTime.TimeOfDay && barTime < EndTime.TimeOfDay)
			{
				if (lastTimeSession != 1)
				{
					lastTimeSession = 1;
					Print(Time[0] + " ******** TRADING SESSION 1 (Main) ******** ");
					Draw.VerticalLine(this, "Session1", 0, Brushes.Aquamarine, DashStyleHelper.Dash, 2);
					EnableTrading = true;
				}
			}
			else
			{
				EnableTrading = false;
			}
			#endregion
		}

		private bool IsBlackoutPeriod()
		{
			TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;
			return barTime >= BlackoutStartTime.TimeOfDay && barTime < BlackoutEndTime.TimeOfDay;
		}

		private DateTime ConvertBarTimeToEST(DateTime barTime)
		{
			// Define the Eastern Standard Time zone
			TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

			// Convert the bar time to EST
			DateTime barTimeInEST = TimeZoneInfo.ConvertTime(barTime, estTimeZone);

			return barTimeInEST;
		}
		#endregion
		#region General Functions
		private double RoundToNearestTick(double price)
		{
			double tickSize = Instrument.MasterInstrument.TickSize;
			return Math.Round(price / tickSize) * tickSize;
		}

		private double GetConvertedValue(double value)
		{
			if (Instrument.MasterInstrument.Name == "MES" || Instrument.MasterInstrument.Name == "ES")
				return RoundToNearestTick(value * ConversionFactorES);
			else
				return value;
		}
		#endregion
		#endregion
	}

	public class TradeExecutionDetailsStrategy
	{
		public double TradeExecPrice { get; set; }
		public string TradeExecuteType { get; set; }
		public string TradeExecName { get; set; }

		public TradeExecutionDetailsStrategy(double tradeExecPrice, string tradeExecuteType, string tradeExecName)
		{
			TradeExecPrice = tradeExecPrice;
			TradeExecuteType = tradeExecuteType;
			TradeExecName = tradeExecName;
		}
	}
}
