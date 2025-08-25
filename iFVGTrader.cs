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
	public class iFVGTrader : Strategy
	{
		private string productName = "iFVGTrader";
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
		{ get; set; } = 15;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Full Stop Loss", Description = "Stop loss level for full position exit", Order = 8, GroupName = "1. Main Parameters")]
		public double FullStopLoss
		{ get; set; } = 10;
		#endregion

		#region FVG Parameters
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Maximum FVG Count", Description = "Maximum number of FVGs to track", Order = 6, GroupName = "1. FVG Parameters")]
		public int MaxFvg
		{ get; set; } = 10;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Ticks", Description = "Minimum number of ticks for FVG", Order = 7, GroupName = "1. FVG Parameters")]
		public int MinimumTicks
		{ get; set; } = 1;

		[NinjaScriptProperty]
		[Display(Name = "Enable Swing Detection", Description = "Enables swing high/low detection", Order = 8, GroupName = "1. FVG Parameters")]
		public bool EnableSwingDetection
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Swing Strength", Description = "Minimum price difference for swing validation (in ticks)", Order = 9, GroupName = "1. FVG Parameters")]
		public int MinimumSwingStrength
		{ get; set; } = 1;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Maximum Swing Count", Description = "Maximum number of swing points to track", Order = 10, GroupName = "1. FVG Parameters")]
		public int MaxSwingCount
		{ get; set; } = 20;

		[NinjaScriptProperty]
		[Display(Name = "Plot All Swing Points", Description = "Plots all swing points on chart for better visualization", Order = 12, GroupName = "1. FVG Parameters")]
		public bool PlotAllSwingPoints
		{ get; set; } = true;
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
		[Range(0, double.MaxValue)]
		[Display(Name = "Enable Dynamic Trim", Description = "Enable dynamic trim", Order = 1, GroupName = "1. TP Parameters")]
		public bool EnableDynamicTrim
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Dynamic Trim R", Description = "Dynamic trim R", Order = 2, GroupName = "1. TP Parameters")]
		public double DynamicTrimR
		{ get; set; } = 1.25;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Secondary Trim R", Description = "Secondary trim R", Order = 3, GroupName = "1. TP Parameters")]
		public double SecondaryTrimR
		{ get; set; } = 2.0;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Dynamic Trim Percent", Description = "Dynamic trim percent", Order = 4, GroupName = "1. TP Parameters")]
		public double DynamicTrimPercent
		{ get; set; } = 15;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Secondary Trim Percent", Description = "Secondary trim percent", Order = 5, GroupName = "1. TP Parameters")]
		public double SecondaryTrimPercent
		{ get; set; } = 50;
		#endregion

		#region Time Parameters
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Start Time", Description = "Session start time", Order = 1, GroupName = "4. Time Parameters")]
		public DateTime StartTime
		{ get; set; } = DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "End Time", Description = "Session end time", Order = 2, GroupName = "4. Time Parameters")]
		public DateTime EndTime
		{ get; set; } = DateTime.Parse("23:59", System.Globalization.CultureInfo.InvariantCulture);

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Blackout Start Time", Description = "Blackout start time", Order = 3, GroupName = "4. Time Parameters")]
		public DateTime BlackoutStartTime
		{ get; set; } = DateTime.Parse("09:20", System.Globalization.CultureInfo.InvariantCulture);

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Blackout End Time", Description = "Blackout end time", Order = 4, GroupName = "4. Time Parameters")]
		public DateTime BlackoutEndTime
		{ get; set; } = DateTime.Parse("09:40", System.Globalization.CultureInfo.InvariantCulture);
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


		private List<TradeExecutionDetailsStrategy> tradeExecutionDetails;

		// FVG related variables
		private List<FVGEntry> fvgList = new List<FVGEntry>();

		// Swing high/low related variables
		private List<SwingPoint> swingHighs = new List<SwingPoint>();
		private List<SwingPoint> swingLows = new List<SwingPoint>();
		private bool potentialSwingHigh = false;
		private bool potentialSwingLow = false;
		private int swingHighBar = -1;
		private int swingLowBar = -1;
		private double swingHighPrice = 0;
		private double swingLowPrice = 0;
		private int swingValidationBars = 0;

		// Horizontal line tracking
		private SwingPoint currentSwingHighLine = null;
		private SwingPoint currentSwingLowLine = null;

		#endregion
		#region Licensing
		// public iFVGTrader()
		// {
		// 	VendorLicense("TradingLevelsAlgo", productName, "www.TradingLevelsAlgo.com", "tradinglevelsalgo@gmail.com", null);
		// }
		#endregion
		#region Initialization
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = productName + @" - iFVGTrader";
				Name = productName;
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

				if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 1)
				{
					string expectedTimeframe = BarsPeriodType.Minute.ToString() + " " + 1.ToString();
					Draw.TextFixed(this, "TimeframeWarning", $"WARNING: This strategy is designed for {expectedTimeframe} charts only!", TextPosition.Center);
					Print($"WARNING: GoldBreakout strategy is designed to run on {expectedTimeframe} timeframe only. Current timeframe: "
						+ BarsPeriod.BarsPeriodType.ToString() + " " + BarsPeriod.Value.ToString());
				}

				if (Instrument.MasterInstrument.Name != "MGC")
				{
					Draw.TextFixed(this, "InstrumentWarning", $"WARNING: This strategy is designed for MGC only!", TextPosition.Center);
					Print($"WARNING: GoldFader strategy is designed to run on MGC only. Current instrument: "
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
				consecutiveLosses = 0;
				tradeExecutionDetails.Clear();
				#endregion

				#region Update Objects
				RemoveDrawObject("TargetLevel" + "ORB High");
				RemoveDrawObject("TargetLevel" + "ORB Low");
				RemoveDrawObject("Label" + "ORB High");
				RemoveDrawObject("Label" + "ORB Low");
				#endregion

				#region Clear Swing Points
				if (EnableSwingDetection)
				{
					// Clear all swing high graphics
					foreach (var swing in swingHighs)
					{
						RemoveDrawObject($"SwingHigh_{swing.BarIndex}");
						RemoveDrawObject($"SwingHighLabel_{swing.BarIndex}");
						RemoveDrawObject($"SwingHighLine_{swing.BarIndex}");
					}
					
					// Clear all swing low graphics
					foreach (var swing in swingLows)
					{
						RemoveDrawObject($"SwingLow_{swing.BarIndex}");
						RemoveDrawObject($"SwingLowLabel_{swing.BarIndex}");
						RemoveDrawObject($"SwingLowLine_{swing.BarIndex}");
					}
					
					// Clear swing point lists
					swingHighs.Clear();
					swingLows.Clear();
					potentialSwingHigh = false;
					potentialSwingLow = false;
					swingHighBar = -1;
					swingLowBar = -1;
					swingHighPrice = 0;
					swingLowPrice = 0;
					swingValidationBars = 0;
					
					// Clear horizontal line tracking
					currentSwingHighLine = null;
					currentSwingLowLine = null;
				}
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

			GetTimeSessionVariables();
			#endregion
			#region Core Engine

			// Check for FVG on current bar
			if (CurrentBar >= 3)
			{
				int upGap = (int)Math.Round((Low[0] - High[2]) / TickSize);
				int dnGap = (int)Math.Round((Low[2] - High[0]) / TickSize);

				if (upGap >= MinimumTicks || dnGap >= MinimumTicks)
				{
					// Create FVG entry
					FVGEntry fvgEntry = new FVGEntry(CurrentBar - 1, upGap >= MinimumTicks, upGap >= MinimumTicks ? High[2] : Low[2], upGap >= MinimumTicks ? Low[0] : High[0]);

					// Add to FVG list if not already present
					if (!fvgList.Any(f => f.BarIndex == fvgEntry.BarIndex))
					{
						fvgList.Add(fvgEntry);
						Print($"FVG detected at bar {CurrentBar - 1}: {(fvgEntry.IsUp ? "UP" : "DOWN")} gap from {fvgEntry.StartPrice} to {fvgEntry.EndPrice}");

						// Draw FVG on chart
						string fvgName = $"FVG_{fvgEntry.BarIndex}";

						// Calculate FVG dimensions
						double fvgStartPrice = fvgEntry.StartPrice;
						double fvgEndPrice = fvgEntry.EndPrice;

						// Draw rectangle for FVG
						Draw.Rectangle(this, fvgName,
							1, fvgStartPrice,
							0, fvgEndPrice,
							fvgEntry.IsUp ? Brushes.LimeGreen : Brushes.Red);

						Draw.Rectangle(this, fvgName, false, 1, fvgStartPrice, 0, fvgEndPrice, fvgEntry.IsUp ? Brushes.LimeGreen : Brushes.Red, fvgEntry.IsUp ? Brushes.LimeGreen : Brushes.Red, 40);

					}

					// Remove old FVGs if exceeding maximum count
					while (fvgList.Count > MaxFvg)
					{
						// Remove the FVG drawing when we remove it from the list
						FVGEntry removedFvg = fvgList[0];
						RemoveDrawObject($"FVG_{removedFvg.BarIndex}");
						fvgList.RemoveAt(0);
					}
				}
			}

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				trimOrderFilled = false;
				trimOrderShortFilled = false;
			}

			// Swing High/Low Detection
			if (EnableSwingDetection && CurrentBar >= 1)
			{
				DetectSwingPoints();
				ValidateSwingPoints();
				InvalidateSwingPoints();
				ManageSwingPoints();
				
				// Update horizontal lines to extend to current bar
				UpdateAndDrawSwingLines();
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
				// Check for FVG invalidation and generate trading signals
				if (fvgList.Count > 0)
				{
					// Start from the newest FVG (last in the list) and work backwards
					for (int i = fvgList.Count - 1; i >= 0; i--)
					{
						FVGEntry fvg = fvgList[i];
						if (fvg.BarIndex >= CurrentBar - 1 || fvg.BarIndex <= CurrentBar - 15)
						{
							continue;
						}
						// Check if FVG is invalidated by current price
						if (fvg.IsUp) // Upward FVG
						{
							// Upward FVG is invalidated when price moves down below the start price
							if (Open[0] > fvg.StartPrice && Close[0] < fvg.EndPrice)
							{
								// Invalidate this FVG
								RemoveDrawObject($"FVG_{fvg.BarIndex}");
								fvgList.RemoveAt(i);

								// Trigger sell signal
								SellTradeEnabled = true;
								Draw.SquareFixed(this, "Sell_iFVG" + CurrentBar, false, 0, Close[0], Brushes.Red);
								Print($"FVG invalidated at bar {fvg.BarIndex}: UP FVG invalidated by price {Close[0]} below {fvg.StartPrice}");
								break; // Exit after first invalidation
							}
						}
						else // Downward FVG
						{
							// Downward FVG is invalidated when price moves up above the start price
							if (Open[0] < fvg.StartPrice && Close[0] > fvg.EndPrice)
							{
								// Invalidate this FVG
								RemoveDrawObject($"FVG_{fvg.BarIndex}");
								fvgList.RemoveAt(i);

								// Trigger buy signal
								BuyTradeEnabled = true;
								Draw.SquareFixed(this, "Buy_iFVG" + CurrentBar, false, 0, Close[0], Brushes.LimeGreen);
								Print($"FVG invalidated at bar {fvg.BarIndex}: DOWN FVG invalidated by price {Close[0]} above {fvg.StartPrice}");
								break; // Exit after first invalidation
							}
						}
					}
				}
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

				SetProfitTarget("Long", CalculationMode.Ticks, FullTakeProfit / TickSize, false);
				SetProfitTarget("LongTrim", CalculationMode.Ticks, FullTakeProfit * DynamicTrimR / TickSize, false);
				SetProfitTarget("LongSecondaryTrim", CalculationMode.Ticks, FullTakeProfit * SecondaryTrimR / TickSize, false);

				SetProfitTarget("Short", CalculationMode.Ticks, FullTakeProfit / TickSize, false);
				SetProfitTarget("ShortTrim", CalculationMode.Ticks, FullTakeProfit * DynamicTrimR / TickSize, false);
				SetProfitTarget("ShortSecondaryTrim", CalculationMode.Ticks, FullTakeProfit * SecondaryTrimR / TickSize, false);
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
			int trimTradeQuantity = (int)Math.Round(TradeQuantity * DynamicTrimPercent / 100, 0);
			int secondaryTrimTradeQuantity = (int)Math.Round(TradeQuantity * SecondaryTrimPercent / 100, 0);

			if (EnableDynamicTrim && TradeQuantity > 1)
				mainTradeQuantity = TradeQuantity - trimTradeQuantity;

			if (EnableDynamicTrim && TradeQuantity > 3)
				mainTradeQuantity = TradeQuantity - trimTradeQuantity - secondaryTrimTradeQuantity;

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (BuyTradeEnabled)
				{
					EnterLongLimit(mainTradeQuantity, Close[0], "Long");
				}
				else if (SellTradeEnabled)
				{
					EnterShortLimit(mainTradeQuantity, Close[0], "Short");
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
					return productName;
				else
					return "";
			}
		}
		#endregion
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
				lastTimeSession = 0;
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
		#endregion

		#region Swing Detection Functions
		private void DetectSwingPoints()
		{
			// We need at least 3 bars to identify a swing point (bar 0, 1, 2)
			if (CurrentBar < 2) return;

			// Check for potential swing high at bar 1 (bar 1 has higher high than bars 0 and 2)
			bool isPotentialSwingHigh = High[1] > High[0] && High[1] > High[2];

			// Check for potential swing low at bar 1 (bar 1 has lower low than bars 0 and 2)
			bool isPotentialSwingLow = Low[1] < Low[0] && Low[1] < Low[2];

			// Check minimum strength requirement
			double swingHighStrength = 0;
			double swingLowStrength = 0;

			if (isPotentialSwingHigh)
			{
				// Calculate the minimum difference from surrounding bars
				double diffFromBar0 = High[1] - High[0];
				double diffFromBar2 = High[1] - High[2];
				swingHighStrength = Math.Min(diffFromBar0, diffFromBar2) / TickSize;
			}

			if (isPotentialSwingLow)
			{
				// Calculate the minimum difference from surrounding bars
				double diffFromBar0 = Low[0] - Low[1];
				double diffFromBar2 = Low[2] - Low[1];
				swingLowStrength = Math.Min(diffFromBar0, diffFromBar2) / TickSize;
			}

			// Create swing points if they meet strength requirements
			if (isPotentialSwingHigh && swingHighStrength >= MinimumSwingStrength)
			{
				SwingPoint swingHigh = new SwingPoint(CurrentBar - 1, true, High[1], Low[1]);
				swingHighs.Add(swingHigh);
				
				if (!DisableGraphics)
				{
					Draw.Dot(this, $"SwingHigh_{CurrentBar - 1}", false, 1, High[1], Brushes.Red);
				}
				
				Print($"Potential Swing High detected at bar {CurrentBar - 1}, price: {High[1]}, strength: {swingHighStrength:F1} ticks");
			}

			if (isPotentialSwingLow && swingLowStrength >= MinimumSwingStrength)
			{
				SwingPoint swingLow = new SwingPoint(CurrentBar - 1, false, Low[1], High[1]);
				swingLows.Add(swingLow);
				
				if (!DisableGraphics)
				{
					Draw.Dot(this, $"SwingLow_{CurrentBar - 1}", false, 1, Low[1], Brushes.Blue);
				}
				
				Print($"Potential Swing Low detected at bar {CurrentBar - 1}, price: {Low[1]}, strength: {swingLowStrength:F1} ticks");
			}
		}

		private void ValidateSwingPoints()
		{
			// Validate swing highs based on price action confirmation
			for (int i = swingHighs.Count - 1; i >= 0; i--)
			{
				SwingPoint swing = swingHighs[i];
				
				if (!swing.IsValidated)
				{
					// Swing high is validated when price closes below its validation price
					if (Close[0] < swing.ValidationPrice)
					{
						swing.IsValidated = true;
						Print($"Swing High validated at bar {swing.BarIndex}, price: {swing.Price}, validation price: {swing.ValidationPrice}");
						
						if (!DisableGraphics)
						{
							// Change color to indicate validation
							RemoveDrawObject($"SwingHigh_{swing.BarIndex}");
							Draw.Dot(this, $"SwingHigh_{swing.BarIndex}", false, CurrentBar - swing.BarIndex, swing.Price, Brushes.Pink);
						}
					}
				}
			}

			// Validate swing lows based on price action confirmation
			for (int i = swingLows.Count - 1; i >= 0; i--)
			{
				SwingPoint swing = swingLows[i];
				
				if (!swing.IsValidated)
				{
					// Swing low is validated when price closes above its validation price
					if (Close[0] > swing.ValidationPrice)
					{
						swing.IsValidated = true;
						Print($"Swing Low validated at bar {swing.BarIndex}, price: {swing.Price}, validation price: {swing.ValidationPrice}");
						
						if (!DisableGraphics)
						{
							// Change color to indicate validation
							RemoveDrawObject($"SwingLow_{swing.BarIndex}");
							Draw.Dot(this, $"SwingLow_{swing.BarIndex}", false, CurrentBar - swing.BarIndex, swing.Price, Brushes.SkyBlue);
						}
					}
				}
			}
		}

		private void InvalidateSwingPoints()
		{
			// Check for swing high invalidation (price breaks above the swing high)
			for (int i = swingHighs.Count - 1; i >= 0; i--)
			{
				SwingPoint swing = swingHighs[i];
				
				if (swing.IsValidated)
				{
					// Swing high is invalidated when price closes above it
					if (Close[0] > swing.Price)
					{
						Print($"Swing High invalidated at bar {swing.BarIndex}, price: {swing.Price} - Price closed above at {Close[0]}");
						
						// Remove graphics
						if (!DisableGraphics)
						{
							RemoveDrawObject($"SwingHigh_{swing.BarIndex}");
							RemoveDrawObject($"SwingHighLine_{swing.BarIndex}");
							
							// Clear current swing high line if this was the active one
							if (currentSwingHighLine != null && currentSwingHighLine.BarIndex == swing.BarIndex)
							{
								currentSwingHighLine = null;
							}
						}
						
						// Remove from list
						swingHighs.RemoveAt(i);
					}
				}
			}

			// Check for swing low invalidation (price breaks below the swing low)
			for (int i = swingLows.Count - 1; i >= 0; i--)
			{
				SwingPoint swing = swingLows[i];
				
				if (swing.IsValidated)
				{
					// Swing low is invalidated when price closes below it
					if (Close[0] < swing.Price)
					{
						Print($"Swing Low invalidated at bar {swing.BarIndex}, price: {swing.Price} - Price closed below at {Close[0]}");
						
						// Remove graphics
						if (!DisableGraphics)
						{
							RemoveDrawObject($"SwingLow_{swing.BarIndex}");
							RemoveDrawObject($"SwingLowLine_{swing.BarIndex}");
							
							// Clear current swing low line if this was the active one
							if (currentSwingLowLine != null && currentSwingLowLine.BarIndex == swing.BarIndex)
							{
								currentSwingLowLine = null;
							}
						}
						
						// Remove from list
						swingLows.RemoveAt(i);
					}
				}
			}
		}

		private void ManageSwingPoints()
		{
			// Remove old swing highs to maintain performance
			while (swingHighs.Count > MaxSwingCount / 2)
			{
				SwingPoint removedSwing = swingHighs[0];
				
				// Remove graphics
				if (!DisableGraphics)
				{
					RemoveDrawObject($"SwingHigh_{removedSwing.BarIndex}");
					RemoveDrawObject($"SwingHighLabel_{removedSwing.BarIndex}");
					RemoveDrawObject($"SwingHighLine_{removedSwing.BarIndex}");
					
					// Clear current swing high line if this was the active one
					if (currentSwingHighLine != null && currentSwingHighLine.BarIndex == removedSwing.BarIndex)
					{
						currentSwingHighLine = null;
					}
				}
				
				swingHighs.RemoveAt(0);
			}

			// Remove old swing lows to maintain performance
			while (swingLows.Count > MaxSwingCount / 2)
			{
				SwingPoint removedSwing = swingLows[0];
				
				// Remove graphics
				if (!DisableGraphics)
				{
					RemoveDrawObject($"SwingLow_{removedSwing.BarIndex}");
					RemoveDrawObject($"SwingLowLabel_{removedSwing.BarIndex}");
					RemoveDrawObject($"SwingLowLine_{removedSwing.BarIndex}");
					
					// Clear current swing low line if this was the active one
					if (currentSwingLowLine != null && currentSwingLowLine.BarIndex == removedSwing.BarIndex)
					{
						currentSwingLowLine = null;
					}
				}
				
				swingLows.RemoveAt(0);
			}
		}
		private void DrawHorizontalLine(SwingPoint swing, bool isHigh)
		{
			if (DisableGraphics) return;

			string lineName = isHigh ? $"SwingHighLine_{swing.BarIndex}" : $"SwingLowLine_{swing.BarIndex}";
			Brush lineColor = isHigh ? Brushes.Pink : Brushes.SkyBlue;
			
			// Remove previous line to update its end point
			RemoveDrawObject(lineName);
			// Draw horizontal line from swing point to current bar
			Draw.Line(this, lineName, false, CurrentBar - swing.BarIndex, swing.Price, 0, swing.Price, lineColor, DashStyleHelper.Solid, 2);
		}

		private void UpdateAndDrawSwingLines()
		{
			if (DisableGraphics) return;

			// --- Highs ---
			SwingPoint latestValidatedHigh = swingHighs.Where(s => s.IsValidated).OrderByDescending(s => s.BarIndex).FirstOrDefault();
		
			// Set the latest validated high as the one to be tracked
			currentSwingHighLine = latestValidatedHigh;

			// If there is a line to track, draw/extend it
			if (currentSwingHighLine != null)
			{
				DrawHorizontalLine(currentSwingHighLine, true);
			}

			// --- Lows ---
			SwingPoint latestValidatedLow = swingLows.Where(s => s.IsValidated).OrderByDescending(s => s.BarIndex).FirstOrDefault();

			// Set the latest validated low as the one to be tracked
			currentSwingLowLine = latestValidatedLow;
			
			// If there is a line to track, draw/extend it
			if (currentSwingLowLine != null)
			{
				DrawHorizontalLine(currentSwingLowLine, false);
			}
		}
		#endregion
		#endregion

		#region FVGEntry Class
		public class FVGEntry
		{
			public int BarIndex { get; set; }
			public bool IsUp { get; set; }
			public double StartPrice { get; set; }
			public double EndPrice { get; set; }

			public FVGEntry(int barIndex, bool isUp, double startPrice, double endPrice)
			{
				BarIndex = barIndex;
				IsUp = isUp;
				StartPrice = startPrice;
				EndPrice = endPrice;
			}
		}
		#endregion

		#region SwingPoint Class
		public class SwingPoint
		{
			public int BarIndex { get; set; }
			public bool IsHigh { get; set; }
			public double Price { get; set; }
			public bool IsValidated { get; set; }
			public double ValidationPrice { get; set; }

			public SwingPoint(int barIndex, bool isHigh, double price, double validationPrice)
			{
				BarIndex = barIndex;
				IsHigh = isHigh;
				Price = price;
				IsValidated = false;
				ValidationPrice = validationPrice;
			}
		}
		#endregion
	}
}
