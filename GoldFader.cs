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
	TODO: [3] Add reversal mode
	TODO: [4] Check if we hit the rversal zone and cut there
	*/
	#endregion
	public class GoldFader : Strategy
	{
		#region Properties
		#region Main Parameters
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Number of Contracts", Description = "Number of contracts to trade", Order = 1, GroupName = "1. Main Parameters")]
		public int TradeQuantity
		{ get; set; } = 4;

		[NinjaScriptProperty]
		[Display(Name = "Enable Target Risk", Description = "Enable target risk mode", Order = 2, GroupName = "1. Main Parameters")]
		public bool EnableTargetRisk
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Target Risk", Description = "Target risk amount in points", Order = 3, GroupName = "1. Main Parameters")]
		public double TargetRisk
		{ get; set; } = 250;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Full Take Profit", Description = "Take profit level for full position exit", Order = 7, GroupName = "1. Main Parameters")]
		public double FullTakeProfit
		{ get; set; } = 20;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Full Stop Loss", Description = "Stop loss level for full position exit", Order = 8, GroupName = "1. Main Parameters")]
		public double FullStopLoss
		{ get; set; } = 6;

		// --- Tariff Mode Properties ---
		[NinjaScriptProperty]
		[Display(Name = "Enable Tariff Mode", Description = "Enable special TP/SL for trades before tariff gate", Order = 20, GroupName = "1. Main Parameters")]
		public bool EnableTariffMode { get; set; } = false;

		[NinjaScriptProperty]
		[Display(Name = "Tariff Gate Date", Description = "Date/time of tariff gate (default: April 1st)", Order = 21, GroupName = "1. Main Parameters")]
		public DateTime TariffGateDate { get; set; } = new DateTime(DateTime.Now.Year, 4, 1, 0, 0, 0);

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Tariff Take Profit", Description = "Take profit for pre-tariff trades", Order = 22, GroupName = "1. Main Parameters")]
		public double TariffTakeProfit { get; set; } = 10;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Tariff Stop Loss", Description = "Stop loss for pre-tariff trades", Order = 23, GroupName = "1. Main Parameters")]
		public double TariffStopLoss { get; set; } = 3;
		// --- End Tariff Mode Properties ---
		#endregion

		#region Core Engine Parameters

		[NinjaScriptProperty]
		[Display(Name = "Enable Doji Mode", Description = "Enables doji mode", Order = 1, GroupName = "2. Core Engine Parameters")]
		public bool EnableDojiMode
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Doji Size", Description = "Size of doji", Order = 2, GroupName = "2. Core Engine Parameters")]
		public double DojiSize
		{ get; set; } = 0.5;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Conversion Factor ES", Description = "Conversion factor for ES", Order = 3, GroupName = "2. Core Engine Parameters")]
		public double ConversionFactorES
		{ get; set; } = 0.33;
		#endregion

		#region Reversal Trade Parameters
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Enable Reversal Trade", Description = "Enable reversal trade", Order = 1, GroupName = "2. Reversal Trade Parameters")]
		public bool EnableReversalTrade
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Reversal Bars Tolerance", Description = "Reversal bars tolerance", Order = 6, GroupName = "2. Reversal Trade Parameters")]
		public int ReversalBarsTolerance
		{ get; set; } = 2;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Reversal Target R", Description = "Reversal target R", Order = 7, GroupName = "2. Reversal Trade Parameters")]
		public double ReversalTargetR
		{ get; set; } = 5;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Reversal SL Break Even R", Description = "Reversal SL break even R", Order = 8, GroupName = "2. Reversal Trade Parameters")]
		public double ReversalSLBreakEvenR
		{ get; set; } = 1;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Reversal SL Trailing R", Description = "Reversal SL trailing R", Order = 9, GroupName = "2. Reversal Trade Parameters")]
		public double ReversalSLTrailingR
		{ get; set; } = 1.75;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Reversal SL Trailing Offset R", Description = "Reversal SL trailing offset R", Order = 10, GroupName = "2. Reversal Trade Parameters")]
		public double ReversalSLTrailingOffsetR
		{ get; set; } = 1.15;

		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "Reversal SL Ratio", Description = "Reversal SL ratio", Order = 11, GroupName = "2. Reversal Trade Parameters")]
		public double ReversalSLRatio
		{ get; set; } = 0.9;
		#endregion

		#region Dynamic TP/SL Parameters
		[NinjaScriptProperty]
		[Display(Name = "Enable Trailing SL", Description = "Enables trailing SL", Order = 1, GroupName = "2. Core Engine Parameters")]
		public bool EnableTrailingSL
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Trailing SL Break Even R", Description = "Trailing SL break even R", Order = 2, GroupName = "2. Core Engine Parameters")]
		public double TrailingSLBreakEvenR
		{ get; set; } = 1;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Trailing SL Trailing R", Description = "Trailing SL trailing R", Order = 3, GroupName = "2. Core Engine Parameters")]
		public double TrailingSLTrailingR
		{ get; set; } = 1.75;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Trailing SL Trailing Offset R", Description = "Trailing SL trailing offset R", Order = 4, GroupName = "2. Core Engine Parameters")]
		public double TrailingSLTrailingOffsetR
		{ get; set; } = 1.15;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Trailing SL Break Even Offset", Description = "Trailing SL break even offset", Order = 5, GroupName = "2. Core Engine Parameters")]
		public double TrailingSLBreakEvenOffset
		{ get; set; } = 0.4;
		#endregion

		#region Trim Settings
		[NinjaScriptProperty]
		[Display(Name = "Enable Dynamic Trim", Description = "Enables dynamic trim functionality", Order = 1, GroupName = "3. Trim Settings")]
		public bool EnableDynamicTrim
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Display(Name = "Enable Secondary Trim", Description = "Enables secondary trim functionality", Order = 2, GroupName = "3. Trim Settings")]
		public bool EnableSecondaryTrim
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Trim Percent", Description = "Percentage of position to trim at first target", Order = 3, GroupName = "3. Trim Settings")]
		public double TrimPercent
		{ get; set; } = 75;

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Secondary Trim Percent", Description = "Percentage of position to trim at second target", Order = 4, GroupName = "3. Trim Settings")]
		public double SecondaryTrimPercent
		{ get; set; } = 30;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Trim Take Profit", Description = "Take profit level for partial position exit", Order = 5, GroupName = "3. Trim Settings")]
		public double TrimTakeProfit
		{ get; set; } = 10;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Secondary Trim Take Profit", Description = "Second take profit level for partial position exit", Order = 6, GroupName = "3. Trim Settings")]
		public double SecondaryTrimTakeProfit
		{ get; set; } = 75;
		#endregion

		#region Time Parameters
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Trigger Time", Description = "Trigger time", Order = 1, GroupName = "4. Time Parameters")]
		public DateTime TriggerTime
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable DST Mode", Description = "If false, subtracts one hour from Trigger Time (for non-DST periods)", Order = 2, GroupName = "4. Time Parameters")]
		public bool EnableDSTMode { get; set; } = true;
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

		private int executionCount = 0;

		private double startingEntry = 0;
		private double startingEntryReversal = 0;
		private double startingSL = 0;
		private double startingTP = 0;
		private bool initialSetupEntry = false;
		private bool initialSetupSL = false;
		private bool initialSetupTP = false;
		private bool initialSetupReversal = false;

		private double currentSL = 0;
		private double currentTP = 0;

		private double CurrentR = 0;
		private double OneR = 0;
		private bool ReversalOrderPlaced = false;
		private bool SLAdjusted = false;
		#endregion
		#region Licensing
		// public GoldFader()
		// {
		// 	string productName = "GoldFader";
		// 	VendorLicense("TradingLevelsAlgo", productName, "www.TradingLevelsAlgo.com", "tradinglevelsalgo@gmail.com", null);
		// }
		#endregion
		#region OnStateChange
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Gold Fader";
				Name = "GoldFader";
				Calculate = Calculate.OnEachTick;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.UniqueEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.High;
				OrderFillResolutionType = BarsPeriodType.Tick;
				OrderFillResolutionValue = 1;
				Slippage = 0;
				StartBehavior = StartBehavior.ImmediatelySubmitSynchronizeAccount;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;

				TriggerTime = DateTime.Parse("21:00", System.Globalization.CultureInfo.InvariantCulture);

				tradeExecutionDetails = new List<TradeExecutionDetailsStrategy>();
			}
			else if (State == State.DataLoaded)
			{
				ClearOutputWindow();
				if (!DisableGraphics)
				{

				}

				if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 15)
				{
					string expectedTimeframe = BarsPeriodType.Minute.ToString() + " " + 15.ToString();
					Draw.TextFixed(this, "TimeframeWarning", $"WARNING: This strategy is designed for {expectedTimeframe} charts only!", TextPosition.Center);
					Print($"WARNING: GoldFader strategy is designed to run on {expectedTimeframe} timeframe only. Current timeframe: "
						+ BarsPeriod.BarsPeriodType.ToString() + " " + BarsPeriod.Value.ToString());
				}

				if (Instrument.MasterInstrument.Name != "MGC")
				{
					Draw.TextFixed(this, "InstrumentWarning", $"WARNING: This strategy is designed for MGC only!", TextPosition.Center);
					Print($"WARNING: GoldFader strategy is designed to run on MGC only. Current instrument: "
						+ Instrument.MasterInstrument.Name);
				}


				if (Instrument.MasterInstrument.Name == "MGC")
				{
					MiniMode = false;
				}
				else if (Instrument.MasterInstrument.Name == "GC")
				{
					MiniMode = true;
				}
			}
		}
		#endregion
		#region OnBarUpdate
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
				#endregion

				EnableTrading = true;
				ReversalOrderPlaced = false;
				SLAdjusted = false;
				lastTimeSession = 0;
				tradeCompleteSLPoints = 0;
				tradeCompleteTPPoints = 0;
				initialSetupSL = false;
				initialSetupTP = false;
				initialSetupReversal = false;
				initialSetupEntry = false;
				startingSL = 0;
				startingTP = 0;
				startingEntry = 0;
				startingEntryReversal = 0;
				currentSL = 0;
				currentTP = 0;
				executionCount++;

				Print(Time[0] + " ******** TRADING ENABLED ******** ");
			}
			#endregion

			#region PnL Calculation
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
				bool reversalTradeEnabled = false;
				if (currentTradePnL < 0 && !SLAdjusted)
				{
					reversalTradeEnabled = true;
				}

				Print(Time[0] + " [PNL UPDATE] COMPLETED TRADE PnL: $" + currentTradePnL + " | Total PnL: $" + currentPnL);
				currentTradePnL = 0;
				newTradeCalculated = false;
				barsInTrade = 0;
				tradeSL = 0;

				bool reversalTradeLong = false;
				bool reversalTradeShort = false;
				if (EnableReversalTrade && !ReversalOrderPlaced && reversalTradeEnabled)
				{
					if (orderFilledLong)
					{
						reversalTradeShort = true;
					}
					else if (orderFilledShort)
					{
						reversalTradeLong = true;
					}
					ReversalOrderPlaced = true;
				}

				int mainTradeQuantity = TradeQuantity;

				if (reversalTradeLong)
				{
					Print(Time[0] + " [GoldFader] Long Entry at: " + Close[0] + " | Quantity: " + mainTradeQuantity);
					EnterLong(mainTradeQuantity, "Reversal Long");
				}
				else if (reversalTradeShort)
				{
					Print(Time[0] + " [GoldFader] Short Entry at: " + Close[0] + " | Quantity: " + mainTradeQuantity);
					EnterShort(mainTradeQuantity, "Reversal Short");
				}
			}

			if (Position.MarketPosition != MarketPosition.Flat)
			{
				barsInTrade++;
			}
			#endregion

			#region Trading Cutoff
			double realtimPnL = Math.Round(currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]), 1);
			#endregion
			#endregion
			#region Core Engine
			OneR = Math.Abs(startingEntry - startingSL);
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				trimOrderFilled = false;
				trimOrderShortFilled = false;
			}
			else if (Position.MarketPosition == MarketPosition.Long)
			{
				CurrentR = (Close[0] - startingEntry) / OneR;
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				CurrentR = (startingEntry - Close[0]) / OneR;
			}
			CurrentR = Math.Round(CurrentR, 1);
			OneR = Math.Round(OneR, 1);
			#endregion
			#region TP/SL Management
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				// Determine if next trade is pre-tariff
				bool isCurrentTradePreTariff = false;
				if (EnableTariffMode)
				{
					DateTime barTimeInEST = ConvertBarTimeToEST(Time[0]);
					if (barTimeInEST < TariffGateDate)
						isCurrentTradePreTariff = true;
				}

				// Set SL/TP for next trade based on tariff mode
				if (isCurrentTradePreTariff)
				{
					SetStopLoss("Long", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);
					SetStopLoss("LongTrim", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);
					SetStopLoss("LongSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);
					SetStopLoss("Short", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);
					SetStopLoss("ShortTrim", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);
					SetStopLoss("ShortSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);
					SetStopLoss("Reversal Long", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);
					SetStopLoss("Reversal Short", CalculationMode.Ticks, GetConvertedValue(TariffStopLoss) / TickSize, false);

					SetProfitTarget("Long", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
					SetProfitTarget("LongTrim", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
					SetProfitTarget("LongSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
					SetProfitTarget("Short", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
					SetProfitTarget("ShortTrim", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
					SetProfitTarget("ShortSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
					SetProfitTarget("Reversal Long", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
					SetProfitTarget("Reversal Short", CalculationMode.Ticks, GetConvertedValue(TariffTakeProfit) / TickSize, false);
				}
				else
				{
					SetStopLoss("Long", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);
					SetStopLoss("LongTrim", CalculationMode.Ticks, GetConvertedValue(TradeQuantity) / TickSize, false);
					SetStopLoss("LongSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);
					SetStopLoss("Short", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);
					SetStopLoss("ShortTrim", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);
					SetStopLoss("ShortSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);
					SetStopLoss("Reversal Long", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);
					SetStopLoss("Reversal Short", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);

					SetProfitTarget("Long", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
					SetProfitTarget("LongTrim", CalculationMode.Ticks, GetConvertedValue(TrimTakeProfit) / TickSize, false);
					SetProfitTarget("LongSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(SecondaryTrimTakeProfit) / TickSize, false);
					SetProfitTarget("Short", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
					SetProfitTarget("ShortTrim", CalculationMode.Ticks, GetConvertedValue(TrimTakeProfit) / TickSize, false);
					SetProfitTarget("ShortSecondaryTrim", CalculationMode.Ticks, GetConvertedValue(SecondaryTrimTakeProfit) / TickSize, false);
					SetProfitTarget("Reversal Long", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
					SetProfitTarget("Reversal Short", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
				}

				if (EnableTargetRisk)
				{
					double targetStop = isCurrentTradePreTariff ? TariffStopLoss : FullStopLoss;
					TradeQuantity = (int)(TargetRisk / (targetStop * Bars.Instrument.MasterInstrument.PointValue));
					if (TradeQuantity < 1)
					{
						TradeQuantity = 1;
					}
				}
			}
			else if (Position.MarketPosition == MarketPosition.Long)
			{
				#region SL Management
				if (EnableTrailingSL)
				{
					if (!initialSetupReversal)
					{
						if (CurrentR > TrailingSLTrailingR)
						{
							double newSL = Close[0] - OneR * TrailingSLTrailingOffsetR;
							if (newSL > currentSL)
							{
								SetStopLoss("Long", CalculationMode.Price, newSL, false);
								SLAdjusted = true;
							}
						}
						else if (CurrentR > TrailingSLBreakEvenR)
						{
							if (currentSL < startingEntry)
							{
								SetStopLoss("Long", CalculationMode.Price, startingEntry - TrailingSLBreakEvenOffset, false);
								SLAdjusted = true;
							}
						}
					}
				}
				#endregion

				#region TP Management
				#endregion
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				#region SL Management
				if (EnableTrailingSL)
				{
					if (!initialSetupReversal)
					{
						if (CurrentR > TrailingSLTrailingR)
						{
							double newSL = Close[0] + OneR * TrailingSLTrailingOffsetR;
							if (newSL < currentSL)
							{
								SetStopLoss("Short", CalculationMode.Price, newSL, false);
								SLAdjusted = true;
							}
						}
						else if (CurrentR > TrailingSLBreakEvenR)
						{
							if (currentSL > startingEntry)
							{
								SetStopLoss("Short", CalculationMode.Price, startingEntry + TrailingSLBreakEvenOffset, false);
								SLAdjusted = true;
							}
						}
					}
				}
				#endregion

				#region TP Management
				#endregion
			}
			#endregion

			if (IsFirstTickOfBar)
			{
				#region Trading Signals
				BuyTradeEnabled = false;
				SellTradeEnabled = false;
				if (EnableTrading)
				{
					DateTime triggerTimeToUse = TriggerTime;
					DateTime barTimeInEST = ConvertBarTimeToEST(Time[0]);
					// If DST mode is disabled and the trading date is within DST, subtract one hour from TriggerTime
					if (EnableDSTMode && !IsInUSDaylightSavingTime(barTimeInEST))
					{
						triggerTimeToUse = TriggerTime.AddHours(-1);
					}

					if (State == State.Realtime)
					{
						triggerTimeToUse = triggerTimeToUse.AddMinutes(15);
					}

					if (ConvertBarTimeToEST(Time[0]).TimeOfDay == triggerTimeToUse.TimeOfDay)
					{
						int barNum = 1;

						if (EnableDojiMode && Math.Abs(Open[0] - Close[0]) <= DojiSize)
						{
							barNum = 2;
						}

						if (Open[barNum] > Close[barNum])
						{
							BuyTradeEnabled = true;
						}
						else if (Open[barNum] < Close[barNum])
						{
							SellTradeEnabled = true;
						}
						Draw.Diamond(this, "TriggerTime" + executionCount, true, 0, High[0] + TickSize * 4, Brushes.Aquamarine);
					}
				}
				#endregion
				#region Trading Management
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
						Print(Time[0] + " [GoldFader] Long Entry at: " + Close[0] + " | Quantity: " + mainTradeQuantity);
						EnterLong(mainTradeQuantity, "Long");
						if (EnableDynamicTrim)
							EnterLong(trimTradeQuantity, "LongTrim");

						if (EnableSecondaryTrim)
							EnterLong(secondaryTrimTradeQuantity, "LongSecondaryTrim");

					}
					else if (SellTradeEnabled)
					{
						Print(Time[0] + " [GoldFader] Short Entry at: " + Close[0] + " | Quantity: " + mainTradeQuantity);
						EnterShort(mainTradeQuantity, "Short");
						if (EnableDynamicTrim)
							EnterShort(trimTradeQuantity, "ShortTrim");

						if (EnableSecondaryTrim)
							EnterShort(secondaryTrimTradeQuantity, "ShortSecondaryTrim");
					}
				}
				#endregion
				#endregion
			}

			#region Dashboard
			string dashBoard =
				$"PnL: $"
				+ realtimPnL.ToString()
				+ " | Trading: "
				+ (EnableTrading ? "Active" : "Off");

			if (Position.MarketPosition == MarketPosition.Long)
			{
				dashBoard += "\n Current R: " + CurrentR;
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				dashBoard += "\n Current R: " + CurrentR;
			}

			if (!DisableGraphics)
			{
				Draw.TextFixed(this, "Dashboard", dashBoard, TextPosition.BottomRight);
			}
			#endregion

		}
		#endregion
		#region OnOrderUpdate
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
					if (!initialSetupEntry)
					{
						initialSetupEntry = true;
						startingEntry = averageFillPrice;
						Print(Time[0] + " [TRADE FILL - Initial Setup] ENTRY SET: " + startingEntry);
					}
				}
			}

			if (order.Name == "Short")
			{
				if (orderState == OrderState.Filled)
				{
					Print(Time[0] + " [TRADE FILL] SHORT FILLED: " + averageFillPrice + " | Bars Since Last Trade: " + (CurrentBar - tradeCompleteBar));
					orderFilledShort = true;
					orderFilledLong = false;
					if (!initialSetupEntry)
					{
						initialSetupEntry = true;
						startingEntry = averageFillPrice;
						Print(Time[0] + " [TRADE FILL - Initial Setup] ENTRY SET: " + startingEntry);
					}
				}
			}

			if (order.Name == "Reversal Long")
			{
				if (orderState == OrderState.Filled)
				{
					Print(Time[0] + " [TRADE FILL] REVERSAL LONG FILLED: " + averageFillPrice);
					if (!initialSetupReversal)
					{
						initialSetupReversal = true;
						startingEntryReversal = averageFillPrice;
						Print(Time[0] + " [TRADE FILL - Reversal Setup] ENTRY SET: " + startingEntryReversal);
					}
				}
			}

			if (order.Name == "Reversal Short")
			{
				if (orderState == OrderState.Filled)
				{
					Print(Time[0] + " [TRADE FILL] REVERSAL SHORT FILLED: " + averageFillPrice);
					if (!initialSetupReversal)
					{
						initialSetupReversal = true;
						startingEntryReversal = averageFillPrice;
						Print(Time[0] + " [TRADE FILL - Reversal Setup] ENTRY SET: " + startingEntryReversal);
					}
				}
			}

			if (order.OrderState == OrderState.Accepted && (order.Name == "Stop loss" || order.Name == "Profit target"))
			{
				if (order.Name == "Stop loss")
				{
					currentSL = order.StopPrice;
					if (!initialSetupSL)
					{
						initialSetupSL = true;
						startingSL = currentSL;
						Print(Time[0] + " [TRADE FILL - Initial Setup] SL SET: " + currentSL);
					}
				}
				else if (order.Name == "Profit target")
				{
					currentTP = order.LimitPrice;
					if (!initialSetupTP)
					{
						initialSetupTP = true;
						startingTP = currentTP;
						Print(Time[0] + " [TRADE FILL - Initial Setup] TP SET: " + currentTP);
					}
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
		#endregion
		#region OnExecutionUpdate
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
		#endregion
		#region DisplayName	
		public override string DisplayName
		{
			get
			{
				if (State == State.SetDefaults)
					return "GoldFader";
				else
					return "";
			}
		}
		#endregion
		#region Functions
		#region Protective Functions

		#endregion
		#region Time Functions
		private DateTime ConvertBarTimeToEST(DateTime barTime)
		{
			// Define the Eastern Standard Time zone
			TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

			// Convert the bar time to EST
			DateTime barTimeInEST = TimeZoneInfo.ConvertTime(barTime, estTimeZone);

			return barTimeInEST;
		}

		// Returns true if the given EST date is within US Daylight Saving Time
		private bool IsInUSDaylightSavingTime(DateTime estDate)
		{
			// DST starts at 2:00 AM on the second Sunday in March
			// DST ends at 2:00 AM on the first Sunday in November
			int year = estDate.Year;

			// Find second Sunday in March
			DateTime march = new DateTime(year, 3, 1);
			int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)march.DayOfWeek + 7) % 7;
			DateTime firstSundayInMarch = march.AddDays(daysUntilSunday);
			DateTime secondSundayInMarch = firstSundayInMarch.AddDays(7);
			DateTime dstStart = new DateTime(year, 3, secondSundayInMarch.Day, 2, 0, 0);

			// Find first Sunday in November
			DateTime november = new DateTime(year, 11, 1);
			daysUntilSunday = ((int)DayOfWeek.Sunday - (int)november.DayOfWeek + 7) % 7;
			DateTime firstSundayInNovember = november.AddDays(daysUntilSunday);
			DateTime dstEnd = new DateTime(year, 11, firstSundayInNovember.Day, 2, 0, 0);

			return estDate >= dstStart && estDate < dstEnd;
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
}
