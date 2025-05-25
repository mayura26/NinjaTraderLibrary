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
	public class AntiORB : Strategy
	{
		#region Properties
		#region Main Parameters
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Number of Contracts", Description = "Number of contracts to trade", Order = 1, GroupName = "1. Main Parameters")]
		public int TradeQuantity
		{ get; set; } = 5;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Enable Target Risk", Description = "Enable target risk", Order = 2, GroupName = "1. Main Parameters")]
		public bool EnableTargetRisk
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Target Risk", Description = "Target risk", Order = 3, GroupName = "1. Main Parameters")]
		public double TargetRisk
		{ get; set; } = 250;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Target Risk Multiplier", Description = "Target risk multiplier", Order = 4, GroupName = "1. Main Parameters")]
		public double TargetRiskMultiplier
		{ get; set; } = 4.5;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Full Take Profit", Description = "Take profit level for full position exit", Order = 7, GroupName = "1. Main Parameters")]
		public double FullTakeProfit
		{ get; set; } = 30;

		[NinjaScriptProperty]
		[Range(0.5, double.MaxValue)]
		[Display(Name = "Full Stop Loss", Description = "Stop loss level for full position exit", Order = 8, GroupName = "1. Main Parameters")]
		public double FullStopLoss
		{ get; set; } = 10;
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
		[Range(0, 2)]
		[Display(Name = "SL Ratio", Description = "SL ratio", Order = 1, GroupName = "2. Dynamic SL")]
		public double SLRatio
		{ get; set; } = 0.65;

		[NinjaScriptProperty]
		[Range(-100, 100)]
		[Display(Name = "SL Offset", Description = "Offset for stop loss level", Order = 2, GroupName = "2. Dynamic SL")]
		public double SLOffset
		{ get; set; } = 1.5;

		[NinjaScriptProperty]
		[Display(Name = "Enable Trailing SL", Description = "Enable trailing stop loss", Order = 3, GroupName = "2. Dynamic SL")]
		public bool EnableTrailingSL
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Breakeven Offset", Description = "Offset for breakeven stop loss level", Order = 4, GroupName = "2. Dynamic SL")]
		public double BreakevenOffset
		{ get; set; } = 0.2;

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Breakeven Trigger R", Description = "R multiple to trigger breakeven stop loss", Order = 4, GroupName = "2. Dynamic SL")]
		public double BreakevenTriggerR
		{ get; set; } = 1.25;

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Trailing Trigger R", Description = "R multiple to trigger trailing stop loss", Order = 6, GroupName = "2. Dynamic SL")]
		public double TrailingTriggerR
		{ get; set; } = 5;

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Trailing Trigger Offset", Description = "Offset for trailing stop loss trigger", Order = 7, GroupName = "2. Dynamic SL")]
		public double TrailingTriggerOffset
		{ get; set; } = 0.5;
		#endregion

		#region Trim Settings
		#endregion

		#region Time Parameters
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Start Time", Description = "Session start time", Order = 1, GroupName = "4. Time Parameters")]
		public DateTime StartTimeORB
		{ get; set; } = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "End Time", Description = "Session end time", Order = 2, GroupName = "4. Time Parameters")]
		public DateTime EndTimeORB
		{ get; set; } = DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "End of Day", Description = "End of day time", Order = 3, GroupName = "4. Time Parameters")]
		public DateTime EndOfDay
		{ get; set; } = DateTime.Parse("16:30", System.Globalization.CultureInfo.InvariantCulture);

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

		private double currentSL = 0;
		private double currentTP = 0;
		private bool initialSetupTP = false;
		private bool initialSetupSL = false;
		private bool initialSetupEntry = false;
		private bool initialSetupReversal = false;
		private double startingTP = 0;
		private double startingSL = 0;
		private double startingEntry = 0;
		private double startingEntryReversal = 0;

		private double ORBHigh;
		private double ORBLow;
		private int ORBFirstBar;
		private int PostORBFirstBar;
		private bool ORBStarted = false;
		private bool PostORBStarted = false;
		private bool ORBTradeLong = false;
		private bool ORBTradeShort = false;
		private bool OrderPlaced = false;
		private bool OrderTriggered = false;
		private bool ORBBreakToHigh = false;
		private bool ORBBreakToLow = false;
		private int OrderPlacedBar = 0;

		private int executionCount = 0;

		private List<TradeExecutionDetailsStrategy> tradeExecutionDetails;

		#endregion
		#region Licensing
		// public AntiORB()
		// {
		// 	string productName = "AntiORB";
		// 	VendorLicense("TradingLevelsAlgo", productName, "www.TradingLevelsAlgo.com", "tradinglevelsalgo@gmail.com", null);
		// }
		#endregion
		#region Initialization
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Anti ORB";
				Name = "AntiORB";
				Calculate = Calculate.OnBarClose;
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
				StartBehavior = StartBehavior.WaitUntilFlatSynchronizeAccount;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;

				tradeExecutionDetails = new List<TradeExecutionDetailsStrategy>();
			}
			else if (State == State.DataLoaded)
			{
				ClearOutputWindow();
				if (!DisableGraphics)
				{

				}

				if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 5)
				{
					string expectedTimeframe = BarsPeriodType.Minute.ToString() + " " + 5.ToString();
					Draw.TextFixed(this, "TimeframeWarning", $"WARNING: This strategy is designed for {expectedTimeframe} charts only!", TextPosition.Center);
					Print($"WARNING: GoldBreakout strategy is designed to run on {expectedTimeframe} timeframe only. Current timeframe: "
						+ BarsPeriod.BarsPeriodType.ToString() + " " + BarsPeriod.Value.ToString());
				}

				if (Instrument.MasterInstrument.Name != "MNQ" && Instrument.MasterInstrument.Name != "NQ")
				{
					Draw.TextFixed(this, "InstrumentWarning", $"WARNING: This strategy is designed for MNQ or NQ only!", TextPosition.Center);
					Print($"WARNING: AntiORB strategy is designed to run on MNQ or NQ only. Current instrument: "
						+ Instrument.MasterInstrument.Name);
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
		#endregion
		#region Main Strategy Logic
		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || BarsInProgress != 0)
				return;

			#region Initialize/Time Setup
			if (Bars.IsFirstBarOfSession)
			{
				#region Reset PnL Variables
				currentPnL = 0;
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
				initialSetupTP = false;
				initialSetupSL = false;
				initialSetupEntry = false;
				initialSetupReversal = false;

				Print(Time[0] + " ******** TRADING ENABLED ******** ");
			}

			#endregion
			#region Core Engine
			EnableTrading = true;
			BuyTradeEnabled = false;
			SellTradeEnabled = false;
			if (IsORBPeriod())
			{
				if (!ORBStarted)
				{
					ORBStarted = true;
					ORBFirstBar = CurrentBar;
				}

				if (High[0] > ORBHigh)
				{
					ORBHigh = High[0];
				}
				if (Low[0] < ORBLow)
				{
					ORBLow = Low[0];
				}
				if (!DisableGraphics)
				{
					Draw.Line(this, "ORB High" + executionCount, CurrentBar - ORBFirstBar, ORBHigh, 0, ORBHigh, Brushes.Green);
					Draw.Line(this, "ORB Low" + executionCount, CurrentBar - ORBFirstBar, ORBLow, 0, ORBLow, Brushes.Red);
				}
			}
			else if (IsPreORBPeriod())
			{
				OrderPlaced = false;
				OrderTriggered = false;
				ORBStarted = false;
				PostORBStarted = false;
				ORBTradeLong = false;
				ORBTradeShort = false;
				initialSetupTP = false;
				initialSetupSL = false;
				initialSetupEntry = false;
				initialSetupReversal = false;
				ORBBreakToHigh = false;
				ORBBreakToLow = false;


				ORBHigh = double.MinValue;
				ORBLow = double.MaxValue;
			}
			else if (IsPostORBPeriod())
			{
				if (Open[0] > ORBHigh && Close[0] > ORBHigh)
				{
					ORBBreakToHigh = true;
					OrderTriggered = false;
				}
				if (Open[0] < ORBLow && Close[0] < ORBLow)
				{
					ORBBreakToLow = true;
					OrderTriggered = false;
				}
				if (!PostORBStarted)
				{
					executionCount++;
					PostORBStarted = true;
					PostORBFirstBar = CurrentBar;
				}

				if (!DisableGraphics && !OrderPlaced)
				{
					Draw.Line(this, "ORB High" + (executionCount - 1), CurrentBar - ORBFirstBar, ORBHigh, 0, ORBHigh, Brushes.Green);
					Draw.Line(this, "ORB Low" + (executionCount - 1), CurrentBar - ORBFirstBar, ORBLow, 0, ORBLow, Brushes.Red);
				}

				if (!OrderPlaced && !OrderTriggered)
				{
					if (Close[0] < ORBHigh && ORBBreakToHigh)
					{
						if (Low[1] > Close[0])
						{
							SellTradeEnabled = true;
							OrderPlaced = true;
							ORBTradeShort = true;
							OrderPlacedBar = CurrentBar;
						}
						OrderTriggered = true;
						ORBBreakToHigh = false;
						Draw.Dot(this, "TriggerCandle" + executionCount, false, 0, ORBTradeLong ? Low[0] - 2 * TickSize : High[0] + 2 * TickSize, ORBTradeLong ? Brushes.Red : Brushes.Green);
					}
					else if (Close[0] > ORBLow && ORBBreakToLow)
					{
						if (High[1] < Close[0])
						{
							BuyTradeEnabled = true;
							OrderPlaced = true;
							ORBTradeLong = true;
							OrderPlacedBar = CurrentBar;
						}
						OrderTriggered = true;
						ORBBreakToLow = false;
						Draw.Dot(this, "TriggerCandle" + executionCount, false, 0, ORBTradeLong ? Low[0] - 2 * TickSize : High[0] + 2 * TickSize, ORBTradeLong ? Brushes.Red : Brushes.Green);
					}
				}
			}

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				trimOrderFilled = false;
				trimOrderShortFilled = false;
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

				Print(Time[0] + " [PNL UPDATE] COMPLETED TRADE PnL: $" + currentTradePnL + " | Total PnL: $" + currentPnL);
				currentTradePnL = 0;
				newTradeCalculated = false;
				barsInTrade = 0;
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
			#region Trading Management
			#region TP/SL Management
			CloseBuyTrade = false;
			CloseSellTrade = false;
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (EnableTargetRisk && OrderPlaced && (BuyTradeEnabled || SellTradeEnabled))
				{
					FullStopLoss = Math.Max(Math.Abs(Close[0] - Open[0]) * SLRatio + SLOffset, 0.5);
					FullTakeProfit = FullStopLoss * TargetRiskMultiplier;
					TradeQuantity = Math.Max(1, (int)(TargetRisk / (FullStopLoss * Instrument.MasterInstrument.PointValue)));
				}

				SetStopLoss("Long", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);
				SetStopLoss("Short", CalculationMode.Ticks, GetConvertedValue(FullStopLoss) / TickSize, false);

				SetProfitTarget("Long", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
				SetProfitTarget("Short", CalculationMode.Ticks, GetConvertedValue(FullTakeProfit) / TickSize, false);
			}
			else if (Position.MarketPosition == MarketPosition.Long)
			{
				#region SL Management
				double CurrentR = (Close[0] - startingEntry) / FullStopLoss;
				if (barsInTrade > 1)
					CurrentR = (High[0] - startingEntry) / FullStopLoss;
				if (EnableTrailingSL)
				{
					if (CurrentR > TrailingTriggerR)
					{
						double newSL = High[0] - FullStopLoss * TrailingTriggerOffset;
						if (newSL > Close[0])
						{
							CloseBuyTrade = true;
						}
						else if (currentSL < newSL)
						{
							SetStopLoss("Long", CalculationMode.Price, newSL, false);
						}
					}
					else if (CurrentR > BreakevenTriggerR)
					{
						double newSL = startingEntry - BreakevenOffset * FullStopLoss;
						if (newSL > Close[0])
						{
							CloseBuyTrade = true;
						}
						else if (currentSL < newSL)
						{
							SetStopLoss("Long", CalculationMode.Price, newSL, false);
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
				double CurrentR = (startingEntry - Close[0]) / FullStopLoss;
				if (barsInTrade > 1)
					CurrentR = (startingEntry - Low[0]) / FullStopLoss;

				if (EnableTrailingSL)
				{
					if (CurrentR > TrailingTriggerR)
					{
						double newSL = Low[0] + FullStopLoss * TrailingTriggerOffset;
						if (newSL < Close[0])
						{
							CloseSellTrade = true;
						}
						else if (newSL < currentSL)
						{
							SetStopLoss("Short", CalculationMode.Price, newSL, false);
						}
					}
					else if (CurrentR > BreakevenTriggerR)
					{
						double newSL = startingEntry + BreakevenOffset * FullStopLoss;
						if (newSL < Close[0])
						{
							CloseSellTrade = true;
						}
						else if (currentSL > newSL)
						{
							SetStopLoss("Short", CalculationMode.Price, newSL, false);
						}
					}
				}
				#endregion

				#region TP Management
				#endregion
			}
			#endregion

			#region Close/Trim Trades
			string tradeCloseReason = "";
			

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

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (BuyTradeEnabled)
				{
					EnterLong(mainTradeQuantity, "Long");
					Print(Time[0] + " [TRADE PLACED] LONG ORDER PLACED: " + mainTradeQuantity);
				}
				else if (SellTradeEnabled)
				{
					EnterShort(mainTradeQuantity, "Short");
					Print(Time[0] + " [TRADE PLACED] SHORT ORDER PLACED: " + mainTradeQuantity);
				}
			}
			#endregion

			#endregion
			#region Dashboard
			if (!DisableGraphics)
			{
				double currentTP1R;
				double currentTP2R;
				double currentTP35R;
				if (Position.MarketPosition == MarketPosition.Long)
				{
					currentTP1R = startingEntry + Math.Abs(startingSL - startingEntry);
					currentTP2R = startingEntry + Math.Abs(startingSL - startingEntry) * 2;
					currentTP35R = startingEntry + Math.Abs(startingSL - startingEntry) * 3.5;
				}
				else
				{
					currentTP1R = startingEntry - Math.Abs(startingSL - startingEntry);
					currentTP2R = startingEntry - Math.Abs(startingSL - startingEntry) * 2;
					currentTP35R = startingEntry - Math.Abs(startingSL - startingEntry) * 3.5;
				}


				if (Position.MarketPosition != MarketPosition.Flat)
				{
					Draw.Line(this, "TP" + executionCount, false, CurrentBar - OrderPlacedBar, currentTP, 0, currentTP, Brushes.SkyBlue, DashStyleHelper.DashDotDot, 1);
					Draw.Line(this, "TP1R" + executionCount, false, CurrentBar - OrderPlacedBar, currentTP1R, 0, currentTP1R, Brushes.Aquamarine, DashStyleHelper.Dot, 1);
					Draw.Text(this, "TP1RLLabel" + executionCount, false, "1R", 0, currentTP1R, 0, Brushes.Aquamarine, null, TextAlignment.Left, null, null, 100);
					Draw.Line(this, "TP2R" + executionCount, false, CurrentBar - OrderPlacedBar, currentTP2R, 0, currentTP2R, Brushes.Aquamarine, DashStyleHelper.Dot, 1);
					Draw.Text(this, "TP2RLLabel" + executionCount, false, "2R", 0, currentTP2R, 0, Brushes.Aquamarine, null, TextAlignment.Left, null, null, 100);
					Draw.Line(this, "TP35R" + executionCount, false, CurrentBar - OrderPlacedBar, currentTP35R, 0, currentTP35R, Brushes.Aquamarine, DashStyleHelper.Dot, 1);
					Draw.Text(this, "TP35RLLabel" + executionCount, false, "3.5R", 0, currentTP35R, 0, Brushes.Aquamarine, null, TextAlignment.Left, null, null, 100);
					Draw.Line(this, "OGEntry" + executionCount, false, CurrentBar - OrderPlacedBar, startingEntry, 0, startingEntry, Brushes.WhiteSmoke, DashStyleHelper.Solid, 1);
					Draw.Line(this, "SL" + executionCount, false, CurrentBar - OrderPlacedBar, currentSL, 0, currentSL, Brushes.Goldenrod, DashStyleHelper.DashDotDot, 1);
				}
			}

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
		#endregion
		#region Order Update
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

			if (order.OrderState == OrderState.Accepted && (order.Name == "Stop loss" || order.Name == "Profit target") && !order.FromEntrySignal.Contains("Dip Buy") && !order.FromEntrySignal.Contains("Dip Sell"))
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
		#region Execution Update
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
					return "AntiORB";
				else
					return "";
			}
		}
		#endregion
		#region Functions
		#region Protective Functions

		#endregion
		#region Time Functions
		private bool IsORBPeriod()
		{
			TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;
			return barTime > StartTimeORB.TimeOfDay && barTime <= EndTimeORB.TimeOfDay;
		}

		private bool IsPreORBPeriod()
		{
			TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;
			return barTime < StartTimeORB.TimeOfDay || barTime > EndOfDay.TimeOfDay;
		}

		private bool IsPostORBPeriod()
		{
			TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;
			return barTime > EndTimeORB.TimeOfDay && barTime <= EndOfDay.TimeOfDay;
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
}
