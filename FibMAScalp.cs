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
TODO: [1] After x bars SL will always be on mid

	TODO: [1] Add offset for SL on fibMID

	TODO:: [7] If TP near VWAP then move to offset
	[ ] 4/22/2025 8:00:00 AM [PNL UPDATE] COMPLETED TRADE PnL: $595 | Total PnL: $342.5 MNQ

	TODO: [8] If bar comes up aggresively we use more conservative TP

TODO: [1] If VWAP is too close avoid trade?
[ ] 2025;-4;11- 04:00:00 AM [PNL UPDATE] 


		TODO: [2] look at slight increase of range to place order to 65? If its further away we can take it in the middle with smaller TP?
	 [ ] 2025-04-09 3:39:00 AM [PNL UPDATE] 

	 TODO: [3] Same if we close just inside, take the trade with smaller TP
	 [ ] 2025-04-10 10:30:00 AM [PNL UPDATE] 

	TODO: [3] Middle TP should go to smooth confirm MA? maybe we need a filter of it being with a range
	[ ] 2025-04-01 9:55

	TODO: [1] Rework early close to look at smooth coming into fibMA
	TODO: [1] Rework brekaeven TP to handle direction of smooth coming into fibMA

	TODO: [6] Add RSI

	TODO: [8] Adjust size based on RSI

	TODO: [4] Add dynamic SL functionality
	*/
	#endregion
	public class FibMAScalp : Strategy
	{
		#region Properties
		#region Pre Loaded Parameters
		[NinjaScriptProperty]
		[Display(Name = "Enable Instrument Specific Settings", Description = "Enable instrument specific settings", Order = 4, GroupName = "1. Main Parameters")]
		public bool EnableInstrumentSpecificSettings
		{ get; set; } = false;
		#endregion

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
		#endregion

		#region Core Engine Parameters
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Distance To Place Order", Description = "Maximum distance from FibMA to place order", Order = 2, GroupName = "2. Core Engine Parameters")]
		public double DistanceToPlaceOrder
		{ get; set; } = 90;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Conversion Factor ES", Description = "Conversion factor for ES", Order = 3, GroupName = "2. Core Engine Parameters")]
		public double ConversionFactorES
		{ get; set; } = 0.33;
		#endregion

		#region Predictive Features
		[NinjaScriptProperty]
		[Display(Name = "Enable Early Close", Description = "Enable early close", Order = 1, GroupName = "3. Predictive Features")]
		public bool EnableEarlyClose
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Early Close Range", Description = "Early close range", Order = 2, GroupName = "3. Predictive Features")]
		public double EarlyCloseRange
		{ get; set; } = 10;

		[NinjaScriptProperty]
		[Display(Name = "Enable Break Even Close", Description = "Enable break even close", Order = 3, GroupName = "3. Predictive Features")]
		public bool EnableBreakEvenClose
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(-100, 100)]
		[Display(Name = "Break Even Close Range", Description = "Break even close range", Order = 4, GroupName = "3. Predictive Features")]
		public double BreakEvenCloseRange
		{ get; set; } = -2;

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Break Even Close Bars", Description = "Break even close bars", Order = 5, GroupName = "3. Predictive Features")]
		public int BreakEvenCloseBars
		{ get; set; } = 5;

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Enable Predictive Exit", Description = "Enable predictive exit", Order = 6, GroupName = "3. Predictive Features")]
		public bool EnablePredictiveExit
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Predictive Exit Trend High", Description = "Predictive exit trend high", Order = 7, GroupName = "3. Predictive Features")]
		public int PredictiveExitTrendHigh
		{ get; set; } = 3;
		#endregion

		#region Protective Features
		[NinjaScriptProperty]
		[Display(Name = "Enable Trade Completion Protection", Description = "Enable trade completion protection", Order = 1, GroupName = "3. Protective Features")]
		public bool EnableTradeCompletionProtect
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Trade Completion High Profit Bars", Description = "Number of bars required since trade completion before triggering trade completion protection", Order = 2, GroupName = "3. Protective Features")]
		public int TradeCompletionHighProfitBars
		{ get; set; } = 2;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Trade Completion Big Loss Bars", Description = "Number of bars required since trade completion before triggering trade completion protection", Order = 3, GroupName = "3. Protective Features")]
		public int TradeCompletionBigLossBars
		{ get; set; } = 2;

		[NinjaScriptProperty]
		[Display(Name = "Enable Bar Cross", Description = "Enable bar cross functionality", Order = 4, GroupName = "3. Protective Features")]
		public bool EnableBarCross
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Bars Since Cross", Description = "Number of bars required since FibMA cross before entering trade", Order = 5, GroupName = "3. Protective Features")]
		public int BarsSinceCross
		{ get; set; } = 1;

		[NinjaScriptProperty]
		[Display(Name = "Enable SmoothConfirm Protect", Description = "Enable protection based on distance from SmoothConfirm MA", Order = 6, GroupName = "3. Protective Features")]
		public bool EnableSmoothConfirmProtect
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Min Distance From SmoothConfirm", Description = "Minimum distance required from SmoothConfirm MA", Order = 7, GroupName = "3. Protective Features")]
		public double MinDistanceFromSmoothConfirm
		{ get; set; } = 20;
		#endregion

		#region Split Entry Parameters
		[NinjaScriptProperty]
		[Display(Name = "Enable Split Entry", Description = "Enable split entry functionality", Order = 1, GroupName = "2. Split Entry Parameters")]
		public bool EnableSplitEntry
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Split Entry Percentage", Description = "Percentage of position to enter initially", Order = 2, GroupName = "2. Split Entry Parameters")]
		public int SplitEntryPercentage
		{ get; set; } = 50;

		[NinjaScriptProperty]
		[Display(Name = "Enable Sniper Entry Mode", Description = "Enable sniper entry mode for more precise entries", Order = 3, GroupName = "2. Split Entry Parameters")]
		public bool EnableSniperEntryMode
		{ get; set; } = true;
		#endregion

		#region Dynamic TP/SL Parameters
		[NinjaScriptProperty]
		[Display(Name = "Enable Dynamic SL", Description = "Enables dynamic stop loss functionality", Order = 0, GroupName = "2. Dynamic SL")]
		public bool EnableDynamicSL
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Profit To Move SL To Mid", Description = "Profit level required to move stop loss to mid", Order = 1, GroupName = "2. Dynamic SL")]
		public double ProfitToMoveSLToMid
		{ get; set; } = 20;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Bars To Move SL To Mid", Description = "Number of bars required to move stop loss to mid", Order = 2, GroupName = "2. Dynamic SL")]
		public int BarsToMoveSLToMid
		{ get; set; } = 2;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Bars To Force SL To Mid", Description = "Number of bars required to force stop loss to mid", Order = 3, GroupName = "2. Dynamic SL")]
		public int BarsToForceSLToMid
		{ get; set; } = 10;

		[NinjaScriptProperty]
		[Range(-100, 100)]
		[Display(Name = "SL Offset", Description = "Offset for stop loss level", Order = 4, GroupName = "2. Dynamic SL")]
		public double SLOffset
		{ get; set; } = 4;

		[NinjaScriptProperty]
		[Display(Name = "Enable SL Boost", Description = "Enables stop loss boost functionality", Order = 4, GroupName = "2. Dynamic SL")]
		public bool EnableSLBoost
		{ get; set; } = false;

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "SL Boost Percent", Description = "Percentage of position to boost stop loss level", Order = 4, GroupName = "2. Dynamic SL")]
		public double SLBoostPercent
		{ get; set; } = 25;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SL Boost Bars", Description = "Number of bars to boost stop loss level", Order = 5, GroupName = "2. Dynamic SL")]
		public int SLBoostBars
		{ get; set; } = 1;

		[NinjaScriptProperty]
		[Display(Name = "Enable TP Near Miss", Description = "Enables TP near miss functionality", Order = 1, GroupName = "2. Dynamic TP")]
		public bool EnableTPNearMiss
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "TP Near Miss Range", Description = "Range for TP near miss", Order = 2, GroupName = "2. Dynamic TP")]
		public int TPNearMissRange
		{ get; set; } = 7;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "TP Near Miss Offset", Description = "Offset for TP near miss", Order = 3, GroupName = "2. Dynamic TP")]
		public int TPNearMissOffset
		{ get; set; } = 2;

		#region TP Creep Settings
		[NinjaScriptProperty]
		[Display(Name = "Enable Full TP Creep", Description = "Enables creeping take profit for full position", Order = 4, GroupName = "2. Dynamic TP")]
		public bool EnableFullTPCreep
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Display(Name = "Enable Trim TP Creep", Description = "Enables creeping take profit for trim position", Order = 5, GroupName = "2. Dynamic TP")]
		public bool EnableTrimTPCreep
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Display(Name = "Enable Secondary Trim TP Creep", Description = "Enables creeping take profit for secondary trim position", Order = 6, GroupName = "2. Dynamic TP")]
		public bool EnableSecondaryTrimTPCreep
		{ get; set; } = true;

		[NinjaScriptProperty]
		[Range(0.1, 10)]
		[Display(Name = "TP Creep Rate", Description = "Rate at which take profit creeps higher", Order = 7, GroupName = "2. Dynamic TP")]
		public double FullTPCreepRate
		{ get; set; } = 1.0;

		[NinjaScriptProperty]
		[Range(0.1, 10)]
		[Display(Name = "Trim TP Creep Rate", Description = "Rate at which take profit creeps higher", Order = 8, GroupName = "2. Dynamic TP")]
		public double TrimTPCreepRate
		{ get; set; } = 1.0;

		[NinjaScriptProperty]
		[Range(0.1, 10)]
		[Display(Name = "Secondary Trim TP Creep Rate", Description = "Rate at which take profit creeps higher", Order = 9, GroupName = "2. Dynamic TP")]
		public double SecondaryTrimTPCreepRate
		{ get; set; } = 1.0;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "TP Min Creep Bars", Description = "Minimum bars before take profit starts creeping", Order = 10, GroupName = "2. Dynamic TP")]
		public int FullTPMinCreepBars
		{ get; set; } = 6;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Trim TP Min Creep Bars", Description = "Minimum bars before take profit starts creeping", Order = 11, GroupName = "2. Dynamic TP")]
		public int TrimTPMinCreepBars
		{ get; set; } = 4;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Secondary Trim TP Min Creep Bars", Description = "Minimum bars before take profit starts creeping", Order = 12, GroupName = "2. Dynamic TP")]
		public int SecondaryTrimTPMinCreepBars
		{ get; set; } = 5;

		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "TP Max Creep Factor", Description = "Maximum factor by which take profit can increase", Order = 13, GroupName = "2. Dynamic TP")]
		public double FullTPMaxCreepFactor
		{ get; set; } = 0.5;

		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "Trim TP Max Creep Factor", Description = "Maximum factor by which take profit can increase", Order = 14, GroupName = "2. Dynamic TP")]
		public double TrimTPMaxCreepFactor
		{ get; set; } = 0.5;

		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "Secondary Trim TP Max Creep Factor", Description = "Maximum factor by which take profit can increase", Order = 15, GroupName = "2. Dynamic TP")]
		public double SecondaryTrimTPMaxCreepFactor
		{ get; set; } = 0.5;
		#endregion
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
		private FibMA fibMA;
		private DynamicTrendLine smoothConfirmMA;
		private VWAP vwap;

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

		private int barsSinceBuyCross = 0;
		private int barsSinceSellCross = 0;
		private int barsSinceBuyTrade = 0;
		private int barsSinceSellTrade = 0;

		private bool newTradeCalculated = false;
		private bool partialTradeCalculated = false;
		private bool newTradeExecuted = false;

		private int barsInTrade = 0;
		private bool MiniMode = false;

		private bool orderFilledLong = false;
		private bool orderFilledShort = false;

		private List<TradeExecutionDetailsStrategy> tradeExecutionDetails;

		#endregion

		#region Licensing	
		public FibMAScalp()
		{
			string productName = "FibMAScalp";
			VendorLicense("TradingLevelsAlgo", productName, "www.TradingLevelsAlgo.com", "tradinglevelsalgo@gmail.com", null);
		}
		#endregion

		#region OnStateChange
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Fibonacci Scalping Strategy";
				Name = "FibMAScalp";
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

				StartTime = DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);
				EndTime = DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				BlackoutStartTime = DateTime.Parse("09:25", System.Globalization.CultureInfo.InvariantCulture);
				BlackoutEndTime = DateTime.Parse("09:40", System.Globalization.CultureInfo.InvariantCulture);


				tradeExecutionDetails = new List<TradeExecutionDetailsStrategy>();
			}
			else if (State == State.DataLoaded)
			{
				fibMA = FibMA();
				smoothConfirmMA = DynamicTrendLine(8, 13, 21);
				vwap = VWAP();

				if (!DisableGraphics)
				{
					AddChartIndicator(smoothConfirmMA);
					AddChartIndicator(vwap);
					AddChartIndicator(fibMA);
				}

				if (Instrument.MasterInstrument.Name == "MNQ" || Instrument.MasterInstrument.Name == "MES")
				{
					MiniMode = false;
				}
				else if (Instrument.MasterInstrument.Name == "NQ" || Instrument.MasterInstrument.Name == "ES")
				{
					MiniMode = true;
				}


				if (EnableInstrumentSpecificSettings)
				{
					if (Instrument.MasterInstrument.Name == "MNQ" || Instrument.MasterInstrument.Name == "NQ")
					{
						FullTakeProfit = 100;
						DistanceToPlaceOrder = 90;
						MinDistanceFromSmoothConfirm = 20;
					}
					else if (Instrument.MasterInstrument.Name == "ES" || Instrument.MasterInstrument.Name == "MES")
					{
						FullTakeProfit = 100;
						DistanceToPlaceOrder = 90;
						MinDistanceFromSmoothConfirm = 20;
					}
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
			if (Low[0] > fibMA.FibHigh[0])
			{
				barsSinceBuyCross++;
			}
			else
			{
				barsSinceBuyCross = 0;
			}

			if (High[0] < fibMA.FibLow[0])
			{
				barsSinceSellCross++;
			}
			else
			{
				barsSinceSellCross = 0;
			}

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
				tradeSL = 0;
			}

			if (Position.MarketPosition != MarketPosition.Flat)
			{
				barsInTrade++;
			}
			else 
			{
				barsInTrade = 0;
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
				if (Close[0] > fibMA.FibHigh[0] && Math.Abs(Close[0] - fibMA.FibHigh[0]) < GetConvertedValue(DistanceToPlaceOrder)
				&& IsTradeCompletionSafe(true)
				&& IsBarCrossSafe(true)
				&& IsSmoothConfirmSafe(true)
				&& !IsBlackoutPeriod())
				{
					BuyTradeEnabled = true;
				}
				else if (Close[0] < fibMA.FibLow[0] && Math.Abs(Close[0] - fibMA.FibLow[0]) < GetConvertedValue(DistanceToPlaceOrder)
				&& IsTradeCompletionSafe(false)
				&& IsBarCrossSafe(false)
				&& IsSmoothConfirmSafe(false)
				&& !IsBlackoutPeriod())
				{
					SellTradeEnabled = true;
				}
			}
			#endregion
			#region Trading Management
			#region TP/SL Management
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (EnableSLBoost)
				{
					double slBoost = SLBoostPercent / 100 * (fibMA.FibHigh[0] - fibMA.FibLow[0]);
					SetStopLoss("Long", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset) - slBoost, false);
					SetStopLoss("LongTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset) - slBoost, false);
					SetStopLoss("LongSecondaryTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset) - slBoost, false);

					SetStopLoss("Short", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset) + slBoost, false);
					SetStopLoss("ShortTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset) + slBoost, false);
					SetStopLoss("ShortSecondaryTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset) + slBoost, false);
				}
				else
				{
					SetStopLoss("Long", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset), false);
					SetStopLoss("LongTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset), false);
					SetStopLoss("LongSecondaryTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset), false);

					SetStopLoss("Short", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset), false);
					SetStopLoss("ShortTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset), false);
					SetStopLoss("ShortSecondaryTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset), false);
				}

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
				bool dynamicSLMoved = false;
				if (EnableDynamicSL)
				{
					#region Push SL
					if ((High[0] > Position.AveragePrice + GetConvertedValue(ProfitToMoveSLToMid) && (barsInTrade > BarsToMoveSLToMid || trimOrderFilled))
					|| barsInTrade >= BarsToForceSLToMid)
					{
						SetStopLoss("Long", CalculationMode.Price, fibMA.FibMAAverage[0], false);
						SetStopLoss("LongTrim", CalculationMode.Price, fibMA.FibMAAverage[0], false);
						SetStopLoss("LongSecondaryTrim", CalculationMode.Price, fibMA.FibMAAverage[0], false);
						dynamicSLMoved = true;
					}
					#endregion
				}

				if (!dynamicSLMoved)
				{
					if (EnableSLBoost && barsInTrade <= SLBoostBars)
					{
						double slBoost = SLBoostPercent / 100 * (fibMA.FibHigh[0] - fibMA.FibLow[0]);
						SetStopLoss("Long", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset) - slBoost, false);
						SetStopLoss("LongTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset) - slBoost, false);
						SetStopLoss("LongSecondaryTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset) - slBoost, false);
					}
					else
					{
						SetStopLoss("Long", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset), false);
						SetStopLoss("LongTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset), false);
						SetStopLoss("LongSecondaryTrim", CalculationMode.Price, fibMA.FibLow[0] - GetConvertedValue(SLOffset), false);
					}
				}
				#endregion

				#region TP Management
				if (EnableBreakEvenClose)
				{
					if (barsInTrade > BreakEvenCloseBars && smoothConfirmMA[0] < Position.AveragePrice + GetConvertedValue(BreakEvenCloseRange))
					{
						SetProfitTarget("Long", CalculationMode.Price, smoothConfirmMA[0], false);
						SetProfitTarget("LongTrim", CalculationMode.Price, smoothConfirmMA[0], false);
						SetProfitTarget("LongSecondaryTrim", CalculationMode.Price, smoothConfirmMA[0], false);
					}
				}

				double currentLongTradeTP = GetConvertedValue(FullTakeProfit) + Position.AveragePrice;
				double currentLongTradeTPTrim = GetConvertedValue(TrimTakeProfit) + Position.AveragePrice;
				double currentLongTradeTPSecondaryTrim = GetConvertedValue(SecondaryTrimTakeProfit) + Position.AveragePrice;

				currentLongTradeTP = CalculateTPCreep(true, EnableFullTPCreep, currentLongTradeTP, GetConvertedValue(FullTPCreepRate), FullTPMinCreepBars, FullTPMaxCreepFactor, GetConvertedValue(FullTakeProfit));
				currentLongTradeTPTrim = CalculateTPCreep(true, EnableTrimTPCreep, currentLongTradeTPTrim, GetConvertedValue(TrimTPCreepRate), TrimTPMinCreepBars, TrimTPMaxCreepFactor, GetConvertedValue(TrimTakeProfit));
				currentLongTradeTPSecondaryTrim = CalculateTPCreep(true, EnableSecondaryTrimTPCreep, currentLongTradeTPSecondaryTrim, GetConvertedValue(SecondaryTrimTPCreepRate), SecondaryTrimTPMinCreepBars, SecondaryTrimTPMaxCreepFactor, GetConvertedValue(SecondaryTrimTakeProfit));

				if (EnableTPNearMiss && Math.Abs(High[0] - currentLongTradeTP) < GetConvertedValue(TPNearMissRange) && currentLongTradeTP > Close[0])
				{
					currentLongTradeTP = High[0] - GetConvertedValue(TPNearMissOffset);
				}
				if (EnableTPNearMiss && Math.Abs(High[0] - currentLongTradeTPTrim) < GetConvertedValue(TPNearMissRange) && currentLongTradeTPTrim > Close[0])
				{
					currentLongTradeTPTrim = High[0] - GetConvertedValue(TPNearMissOffset);
				}
				if (EnableTPNearMiss && Math.Abs(High[0] - currentLongTradeTPSecondaryTrim) < GetConvertedValue(TPNearMissRange) && currentLongTradeTPSecondaryTrim > Close[0])
				{
					currentLongTradeTPSecondaryTrim = High[0] - GetConvertedValue(TPNearMissOffset);
				}

				SetProfitTarget("Long", CalculationMode.Price, currentLongTradeTP, false);
				SetProfitTarget("LongTrim", CalculationMode.Price, currentLongTradeTPTrim, false);
				SetProfitTarget("LongSecondaryTrim", CalculationMode.Price, currentLongTradeTPSecondaryTrim, false);
				#endregion
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				#region SL Management
				bool dynamicSLMoved = false;
				if (EnableDynamicSL)
				{
					#region Push SL
					if ((Low[0] < Position.AveragePrice - GetConvertedValue(ProfitToMoveSLToMid) && (barsInTrade > BarsToMoveSLToMid || trimOrderShortFilled))
					|| barsInTrade >= BarsToForceSLToMid)
					{
						SetStopLoss("Short", CalculationMode.Price, fibMA.FibMAAverage[0], false);
						SetStopLoss("ShortTrim", CalculationMode.Price, fibMA.FibMAAverage[0], false);
						SetStopLoss("ShortSecondaryTrim", CalculationMode.Price, fibMA.FibMAAverage[0], false);
						dynamicSLMoved = true;
					}
					#endregion
				}

				if (!dynamicSLMoved)
				{
					if (EnableSLBoost && barsInTrade <= SLBoostBars)
					{
						double slBoost = SLBoostPercent / 100 * (fibMA.FibHigh[0] - fibMA.FibLow[0]);
						SetStopLoss("Short", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset) + slBoost, false);
						SetStopLoss("ShortTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset) + slBoost, false);
						SetStopLoss("ShortSecondaryTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset) + slBoost, false);
					}
					else
					{
						SetStopLoss("Short", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset), false);
						SetStopLoss("ShortTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset), false);
						SetStopLoss("ShortSecondaryTrim", CalculationMode.Price, fibMA.FibHigh[0] + GetConvertedValue(SLOffset), false);
					}
				}
				#endregion

				#region TP Management
				if (EnableBreakEvenClose)
				{
					if (barsInTrade > BreakEvenCloseBars && smoothConfirmMA[0] > Position.AveragePrice - GetConvertedValue(BreakEvenCloseRange))
					{
						SetProfitTarget("Short", CalculationMode.Price, smoothConfirmMA[0], false);
						SetProfitTarget("ShortTrim", CalculationMode.Price, smoothConfirmMA[0], false);
						SetProfitTarget("ShortSecondaryTrim", CalculationMode.Price, smoothConfirmMA[0], false);
					}
				}

				double currentShortTradeTP = Position.AveragePrice - GetConvertedValue(FullTakeProfit);
				double currentShortTradeTPTrim = Position.AveragePrice - GetConvertedValue(TrimTakeProfit);
				double currentShortTradeTPSecondaryTrim = Position.AveragePrice - GetConvertedValue(SecondaryTrimTakeProfit);

				currentShortTradeTP = CalculateTPCreep(false, EnableFullTPCreep, currentShortTradeTP, GetConvertedValue(FullTPCreepRate), FullTPMinCreepBars, FullTPMaxCreepFactor, GetConvertedValue(FullTakeProfit));
				currentShortTradeTPTrim = CalculateTPCreep(false, EnableTrimTPCreep, currentShortTradeTPTrim, GetConvertedValue(TrimTPCreepRate), TrimTPMinCreepBars, TrimTPMaxCreepFactor, GetConvertedValue(TrimTakeProfit));
				currentShortTradeTPSecondaryTrim = CalculateTPCreep(false, EnableSecondaryTrimTPCreep, currentShortTradeTPSecondaryTrim, GetConvertedValue(SecondaryTrimTPCreepRate), SecondaryTrimTPMinCreepBars, SecondaryTrimTPMaxCreepFactor, GetConvertedValue(SecondaryTrimTakeProfit));

				if (EnableTPNearMiss && Math.Abs(Low[0] - currentShortTradeTP) < GetConvertedValue(TPNearMissRange) && currentShortTradeTP > Close[0])
				{
					currentShortTradeTP = Low[0] - GetConvertedValue(TPNearMissOffset);
				}
				if (EnableTPNearMiss && Math.Abs(Low[0] - currentShortTradeTPTrim) < GetConvertedValue(TPNearMissRange) && currentShortTradeTPTrim > Close[0])
				{
					currentShortTradeTPTrim = Low[0] - GetConvertedValue(TPNearMissOffset);
				}
				if (EnableTPNearMiss && Math.Abs(Low[0] - currentShortTradeTPSecondaryTrim) < GetConvertedValue(TPNearMissRange) && currentShortTradeTPSecondaryTrim > Close[0])
				{
					currentShortTradeTPSecondaryTrim = Low[0] - GetConvertedValue(TPNearMissOffset);
				}

				SetProfitTarget("Short", CalculationMode.Price, currentShortTradeTP, false);
				SetProfitTarget("ShortTrim", CalculationMode.Price, currentShortTradeTPTrim, false);
				SetProfitTarget("ShortSecondaryTrim", CalculationMode.Price, currentShortTradeTPSecondaryTrim, false);
				#endregion
			}
			#endregion

			#region Close/Trim Trades
			string tradeCloseReason = "";
			CloseBuyTrade = false;
			CloseSellTrade = false;
			if (EnableEarlyClose)
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (smoothConfirmMA[0] > fibMA.FibHigh[0] && Math.Abs(smoothConfirmMA[0] - fibMA.FibHigh[0]) <= GetConvertedValue(EarlyCloseRange))
					{
						CloseBuyTrade = true;
						tradeCloseReason = "Early Close";
					}
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					if (smoothConfirmMA[0] < fibMA.FibLow[0] && Math.Abs(smoothConfirmMA[0] - fibMA.FibLow[0]) <= GetConvertedValue(EarlyCloseRange))
					{
						CloseSellTrade = true;
						tradeCloseReason = "Early Close";
					}
				}
			}

			if (EnablePredictiveExit)
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					bool reversal = smoothConfirmMA.MomentumMA[0] < smoothConfirmMA.MomentumSignal[0] && smoothConfirmMA.MomentumMA[1] >= smoothConfirmMA.MomentumSignal[1];
					if (!DisableGraphics && reversal)
						Draw.TriangleDownSmall(this, "ReversalDown" + CurrentBar, false, 0, High[0], Brushes.Aqua);

					if (smoothConfirmMA.TrendDirection[0] > PredictiveExitTrendHigh && reversal)
					{
						CloseBuyTrade = true;
						tradeCloseReason = "Predictive Exit";
					}
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					bool reversal = smoothConfirmMA.MomentumMA[0] > smoothConfirmMA.MomentumSignal[0] && smoothConfirmMA.MomentumMA[1] <= smoothConfirmMA.MomentumSignal[1];
					if (!DisableGraphics && reversal)
						Draw.TriangleUpSmall(this, "ReversalUp" + CurrentBar, false, 0, Low[0], Brushes.Aqua);

					if (smoothConfirmMA.TrendDirection[0] < -PredictiveExitTrendHigh && reversal)
					{
						CloseSellTrade = true;
						tradeCloseReason = "Predictive Exit";
					}
				}
			}

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

			if (Position.MarketPosition == MarketPosition.Flat && !newTradeExecuted)
			{
				if (BuyTradeEnabled)
				{
					double entryPrice = fibMA.FibHigh[0];
					if (EnableSniperEntryMode && smoothConfirmMA[0] < fibMA.FibHigh[0])
					{
						entryPrice = fibMA.FibMAAverage[0];
					}

					entryOrder = EnterLongLimit(0, false, mainTradeQuantity, entryPrice, "Long");
					if (EnableDynamicTrim)
						entryOrderTrim = EnterLongLimit(0, false, trimTradeQuantity, entryPrice, "LongTrim");
					if (EnableSecondaryTrim)
						entryOrderSecondaryTrim = EnterLongLimit(0, false, secondaryTrimTradeQuantity, entryPrice, "LongSecondaryTrim");
				}
				else if (SellTradeEnabled)
				{
					double entryPrice = fibMA.FibLow[0];
					if (EnableSniperEntryMode && smoothConfirmMA[0] > fibMA.FibLow[0])
					{
						entryPrice = fibMA.FibMAAverage[0];
					}

					entryOrderShort = EnterShortLimit(0, false, mainTradeQuantity, entryPrice, "Short");
					if (EnableDynamicTrim)
						entryOrderTrimShort = EnterShortLimit(0, false, trimTradeQuantity, entryPrice, "ShortTrim");
					if (EnableSecondaryTrim)
						entryOrderSecondaryTrimShort = EnterShortLimit(0, false, secondaryTrimTradeQuantity, entryPrice, "ShortSecondaryTrim");
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

			string tradeStatus = "";

			if (BuyTradeEnabled)
				tradeStatus += (tradeStatus != "" ? " | " : "") + "Buy Signal";

			if (SellTradeEnabled)
				tradeStatus += (tradeStatus != "" ? " | " : "") + "Sell Signal";

			if (barsSinceBuyCross > 0)
				tradeStatus += (tradeStatus != "" ? " | " : "") + "Buy Cross: " + barsSinceBuyCross;

			if (barsSinceSellCross > 0)
				tradeStatus += (tradeStatus != "" ? " | " : "") + "Sell Cross: " + barsSinceSellCross;

			if (tradeStatus != "")
				dashBoard += "\nTriggers: " + tradeStatus;

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
					return "FibMAScalp";
				else
					return "";
			}
		}
		#endregion

		#region Functions
		#region TP/SL Functions
		private double CalculateTPCreep(bool isBuy, bool creepEnabled, double initialTP, double creepRate, int minCreepBars, double maxCreepFactor, double TPValue)
		{
			// If we haven't reached the minimum number of bars to start creeping, return the initial TP
			if (!creepEnabled || barsInTrade < minCreepBars)
				return initialTP;
			
			// Calculate the creep amount based on bars in trade beyond the minimum
			double creepAmount = (barsInTrade - minCreepBars + 1) * creepRate;
			
			// Calculate the new TP value
			double newTP = initialTP;
			
			if (isBuy)
				newTP = initialTP - creepAmount;
			else
				newTP = initialTP + creepAmount;
			
			// Ensure we don't exceed the maximum creep factor
			double maxTP = isBuy ? 
				initialTP - TPValue * maxCreepFactor : 
				initialTP + TPValue * maxCreepFactor;
			
			// Cap the TP at the maximum allowed value
			if (isBuy)
				return Math.Max(newTP, maxTP);
			else
				return Math.Min(newTP, maxTP);

		}
		#endregion
		#region Protective Functions
		private bool IsTradeCompletionSafe(bool isBuy)
		{
			bool isOrderSameDir = isBuy && orderFilledLong || !isBuy && orderFilledShort;
			double ratio = 0.9;
			double bigLoss = 10;
			if (EnableTradeCompletionProtect)
			{
				if (tradeCompleteBar == CurrentBar)
				{
					Print(Time[0] + " [Trade Completion]: Trigger not generated as trade completed on bar: " + CurrentBar);
					return false;
				}
				else if (tradeCompleteTPPoints >= ratio * GetConvertedValue(FullTakeProfit) && (CurrentBar - tradeCompleteBar) <= TradeCompletionHighProfitBars)
				{
					Print(Time[0] + " [Trade Completion - High Profit]: Trigger not generated as trade completed " + (CurrentBar - tradeCompleteBar) + " bars ago with profit of: " + tradeCompleteTPPoints + " points");
					return false;
				}
				else if (tradeCompleteSLPoints <= GetConvertedValue(bigLoss) && (CurrentBar - tradeCompleteBar) <= TradeCompletionBigLossBars)
				{
					Print(Time[0] + " [Trade Completion - Big Loss]: Trigger not generated as trade completed " + (CurrentBar - tradeCompleteBar) + " bars ago with loss of: " + tradeCompleteSLPoints + " points");
					return false;
				}
			}
			return true;
		}

		private bool IsBarCrossSafe(bool isBuy)
		{
			if (EnableBarCross)
			{
				if (isBuy)
				{
					if (barsSinceBuyCross >= BarsSinceCross)
						return true;
				}
				else
				{
					if (barsSinceSellCross >= BarsSinceCross)
						return true;
				}
				return false;
			}
			return true;
		}

		private bool IsSmoothConfirmSafe(bool isBuy)
		{
			if (EnableSmoothConfirmProtect)
			{
				if (isBuy)
				{
					if (smoothConfirmMA[0] > fibMA.FibHigh[0] && Math.Abs(smoothConfirmMA[0] - fibMA.FibHigh[0]) < GetConvertedValue(MinDistanceFromSmoothConfirm))
						return false;
				}
				else
				{
					if (smoothConfirmMA[0] < fibMA.FibLow[0] && Math.Abs(smoothConfirmMA[0] - fibMA.FibLow[0]) < GetConvertedValue(MinDistanceFromSmoothConfirm))
						return false;
				}
			}
			return true;
		}
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
}
