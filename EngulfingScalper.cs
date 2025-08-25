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
TODO: [1] Add SL/TP order tracking
	*/
    #endregion
    public class EngulfingScalper : Strategy
    {
        private string productName = "EngulfingScalper";
        #region Properties
        #region Main Parameters
        [NinjaScriptProperty]
        [Display(Name = "Enable Target Risk Mode", Description = "Enable position sizing based on target risk, otherwise use fixed contract count", Order = 1, GroupName = "1. Main Parameters")]
        public bool EnableTargetRiskMode
        { get; set; } = true;

        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Target Risk", Description = "Target risk per trade in dollars", Order = 2, GroupName = "1. Main Parameters")]
        public double TargetRisk
        { get; set; } = 500;

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Fixed Contract Count", Description = "Number of contracts to trade when target risk mode is disabled", Order = 3, GroupName = "1. Main Parameters")]
        public int FixedContractCount
        { get; set; } = 5;
        #endregion

        #region Engulfing Parameters
        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Max Stop Loss (Points)", Description = "Maximum stop loss size in points", Order = 1, GroupName = "2. Engulfing Parameters")]
        public double MaxStopLossPoints
        { get; set; } = 5;
        #endregion

        #region Day Selection Parameters
        [NinjaScriptProperty]
        [Display(Name = "Monday", Description = "Trading mode for Monday", Order = 1, GroupName = "3. Day Selection")]
        public DayTradingMode MondayMode
        { get; set; } = DayTradingMode.Both;

        [NinjaScriptProperty]
        [Display(Name = "Tuesday", Description = "Trading mode for Tuesday", Order = 2, GroupName = "3. Day Selection")]
        public DayTradingMode TuesdayMode
        { get; set; } = DayTradingMode.Both;

        [NinjaScriptProperty]
        [Display(Name = "Wednesday", Description = "Trading mode for Wednesday", Order = 3, GroupName = "3. Day Selection")]
        public DayTradingMode WednesdayMode
        { get; set; } = DayTradingMode.Both;

        [NinjaScriptProperty]
        [Display(Name = "Thursday", Description = "Trading mode for Thursday", Order = 4, GroupName = "3. Day Selection")]
        public DayTradingMode ThursdayMode
        { get; set; } = DayTradingMode.Both;

        [NinjaScriptProperty]
        [Display(Name = "Friday", Description = "Trading mode for Friday", Order = 5, GroupName = "3. Day Selection")]
        public DayTradingMode FridayMode
        { get; set; } = DayTradingMode.Both;
        #endregion

        #region Day-Specific SL/TP Parameters
        [NinjaScriptProperty]
        [Range(50, 200)]
        [Display(Name = "Monday SL %", Description = "Stop loss percentage for Monday", Order = 1, GroupName = "4. Day-Specific SL/TP")]
        public double MondayStopLossPercent
        { get; set; } = 100;

        [NinjaScriptProperty]
        [Range(25, 200)]
        [Display(Name = "Monday TP %", Description = "Take profit percentage for Monday", Order = 2, GroupName = "4. Day-Specific SL/TP")]
        public double MondayTakeProfitPercent
        { get; set; } = 75;

        [NinjaScriptProperty]
        [Range(50, 200)]
        [Display(Name = "Tuesday SL %", Description = "Stop loss percentage for Tuesday", Order = 3, GroupName = "4. Day-Specific SL/TP")]
        public double TuesdayStopLossPercent
        { get; set; } = 100;

        [NinjaScriptProperty]
        [Range(25, 200)]
        [Display(Name = "Tuesday TP %", Description = "Take profit percentage for Tuesday", Order = 4, GroupName = "4. Day-Specific SL/TP")]
        public double TuesdayTakeProfitPercent
        { get; set; } = 75;

        [NinjaScriptProperty]
        [Range(50, 200)]
        [Display(Name = "Wednesday SL %", Description = "Stop loss percentage for Wednesday", Order = 5, GroupName = "4. Day-Specific SL/TP")]
        public double WednesdayStopLossPercent
        { get; set; } = 100;

        [NinjaScriptProperty]
        [Range(25, 200)]
        [Display(Name = "Wednesday TP %", Description = "Take profit percentage for Wednesday", Order = 6, GroupName = "4. Day-Specific SL/TP")]
        public double WednesdayTakeProfitPercent
        { get; set; } = 75;

        [NinjaScriptProperty]
        [Range(50, 200)]
        [Display(Name = "Thursday SL %", Description = "Stop loss percentage for Thursday", Order = 7, GroupName = "4. Day-Specific SL/TP")]
        public double ThursdayStopLossPercent
        { get; set; } = 100;

        [NinjaScriptProperty]
        [Range(25, 200)]
        [Display(Name = "Thursday TP %", Description = "Take profit percentage for Thursday", Order = 8, GroupName = "4. Day-Specific SL/TP")]
        public double ThursdayTakeProfitPercent
        { get; set; } = 75;

        [NinjaScriptProperty]
        [Range(50, 200)]
        [Display(Name = "Friday SL %", Description = "Stop loss percentage for Friday", Order = 9, GroupName = "4. Day-Specific SL/TP")]
        public double FridayStopLossPercent
        { get; set; } = 100;

        [NinjaScriptProperty]
        [Range(25, 200)]
        [Display(Name = "Friday TP %", Description = "Take profit percentage for Friday", Order = 10, GroupName = "4. Day-Specific SL/TP")]
        public double FridayTakeProfitPercent
        { get; set; } = 75;
        #endregion

        #region Day-Specific Engulfing Size Parameters
        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Monday Min Engulfing %", Description = "Minimum engulfing body size as percentage of market price for Monday", Order = 1, GroupName = "4a. Day-Specific Engulfing Size")]
        public double MondayMinEngulfingPercent
        { get; set; } = 0;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Monday Max Engulfing %", Description = "Maximum engulfing body size as percentage of market price for Monday", Order = 2, GroupName = "4a. Day-Specific Engulfing Size")]
        public double MondayMaxEngulfingPercent
        { get; set; } = 2;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Tuesday Min Engulfing %", Description = "Minimum engulfing body size as percentage of market price for Tuesday", Order = 3, GroupName = "4a. Day-Specific Engulfing Size")]
        public double TuesdayMinEngulfingPercent
        { get; set; } = 0;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Tuesday Max Engulfing %", Description = "Maximum engulfing body size as percentage of market price for Tuesday", Order = 4, GroupName = "4a. Day-Specific Engulfing Size")]
        public double TuesdayMaxEngulfingPercent
        { get; set; } = 2;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Wednesday Min Engulfing %", Description = "Minimum engulfing body size as percentage of market price for Wednesday", Order = 5, GroupName = "4a. Day-Specific Engulfing Size")]
        public double WednesdayMinEngulfingPercent
        { get; set; } = 0;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Wednesday Max Engulfing %", Description = "Maximum engulfing body size as percentage of market price for Wednesday", Order = 6, GroupName = "4a. Day-Specific Engulfing Size")]
        public double WednesdayMaxEngulfingPercent
        { get; set; } = 2;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Thursday Min Engulfing %", Description = "Minimum engulfing body size as percentage of market price for Thursday", Order = 7, GroupName = "4a. Day-Specific Engulfing Size")]
        public double ThursdayMinEngulfingPercent
        { get; set; } = 0;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Thursday Max Engulfing %", Description = "Maximum engulfing body size as percentage of market price for Thursday", Order = 8, GroupName = "4a. Day-Specific Engulfing Size")]
        public double ThursdayMaxEngulfingPercent
        { get; set; } = 2;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Friday Min Engulfing %", Description = "Minimum engulfing body size as percentage of market price for Friday", Order = 9, GroupName = "4a. Day-Specific Engulfing Size")]
        public double FridayMinEngulfingPercent
        { get; set; } = 0;

        [NinjaScriptProperty]
        [Range(0, 10)]
        [Display(Name = "Friday Max Engulfing %", Description = "Maximum engulfing body size as percentage of market price for Friday", Order = 10, GroupName = "4a. Day-Specific Engulfing Size")]
        public double FridayMaxEngulfingPercent
        { get; set; } = 2;
        #endregion

        #region Time Parameters
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start Time", Description = "Session start time", Order = 1, GroupName = "5. Time Parameters")]
        public DateTime StartTime
        { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "End Time", Description = "Session end time", Order = 2, GroupName = "5. Time Parameters")]
        public DateTime EndTime
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Trading Times", Description = "Enable trading time restrictions", Order = 3, GroupName = "5. Time Parameters")]
        public bool EnableTradingTimes
        { get; set; } = false;
        #endregion

        #region Extra Parameters	
        [NinjaScriptProperty]
        [Display(Name = "Disable Graphics", Description = "Disables drawing of chart graphics", Order = 1, GroupName = "9. Extra Parameters")]
        public bool DisableGraphics
        { get; set; } = false;
        #endregion
        #endregion

        #region Instrument-Specific Settings
        [NinjaScriptProperty]
        [Display(Name = "Mode", Description = "Instrument-specific trading mode", Order = 1, GroupName = "0. Instrument Settings")]
        public EngulfingScalperSettings Mode
        { get; set; } = EngulfingScalperSettings.Custom;
        #endregion

        #region Variables
        private double currentPnL;
        private int consecutiveLosses = 0;
        private int lastTradeChecked = -1;
        private double currentTradePnL = 0;
        private int partialTradeQty = 0;
        private double partialTradePnL = 0;
        private int lastTimeSession = 0;
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
        private Order entryOrderShort;

        private bool newTradeCalculated = false;
        private bool partialTradeCalculated = false;
        private bool newTradeExecuted = false;

        private int barsInTrade = 0;

        private bool orderFilledLong = false;
        private bool orderFilledShort = false;

        private double currentSL = 0;
        private double currentTP = 0;
        private bool initialSetupTP = false;
        private bool initialSetupSL = false;
        private bool initialSetupEntry = false;
        private double startingTP = 0;
        private double startingSL = 0;
        private double startingEntry = 0;

        private List<TradeExecutionDetailsStrategy> tradeExecutionDetails;

        // Engulfing-specific variables
        private bool engulfingDetected = false;
        private bool engulfingLong = false;
        private double engulfingBodySize = 0;
        private double engulfingHigh = 0;
        private double engulfingLow = 0;
        private int engulfingBar = 0;
        private bool orderPlaced = false;
        private int executionCount = 0;

        // Instrument mode tracking
        private bool MiniMode = false;
        private double instrumentPointValue = 0;
        private double dailyRiskLimit = 0;
        #endregion

        #region Initialization
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = productName + @" - Engulfing Pattern Strategy";
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

                StartTime = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                EndTime = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);

                tradeExecutionDetails = new List<TradeExecutionDetailsStrategy>();
            }
            else if (State == State.DataLoaded)
            {
                ClearOutputWindow();
                if (!DisableGraphics)
                {

                }

                // Verify timeframe (should be 30-minute for engulfing strategy)
                if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 30)
                {
                    string expectedTimeframe = BarsPeriodType.Minute.ToString() + " " + 30.ToString();
                    Draw.TextFixed(this, "TimeframeWarning", $"WARNING: This strategy is designed for {expectedTimeframe} charts only!", TextPosition.Center);
                    Print($"WARNING: {productName} strategy is designed to run on {expectedTimeframe} timeframe only. Current timeframe: "
                        + BarsPeriod.BarsPeriodType.ToString() + " " + BarsPeriod.Value.ToString());
                }

                // Apply instrument-specific settings based on Mode
                if (Mode == EngulfingScalperSettings.NQ)
                {
                    // NQ-specific settings
                    MaxStopLossPoints = 5;

                    MondayMode = DayTradingMode.Both;
                    TuesdayMode = DayTradingMode.Both;
                    WednesdayMode = DayTradingMode.Both;
                    ThursdayMode = DayTradingMode.Both;
                    FridayMode = DayTradingMode.Both;

                    // Day-specific settings for NQ
                    MondayStopLossPercent = 100;
                    MondayTakeProfitPercent = 75;
                    TuesdayStopLossPercent = 100;
                    TuesdayTakeProfitPercent = 75;
                    WednesdayStopLossPercent = 100;
                    WednesdayTakeProfitPercent = 75;
                    ThursdayStopLossPercent = 100;
                    ThursdayTakeProfitPercent = 75;
                    FridayStopLossPercent = 100;
                    FridayTakeProfitPercent = 75;

                    // NQ-specific time settings
                    StartTime = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                    EndTime = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
                    EnableTradingTimes = true;

                    // NQ-specific risk settings
                    EnableTargetRiskMode = false;
                    TargetRisk = 500;
                    dailyRiskLimit = 2000;

                    // Verify instrument compatibility
                    if (Instrument.MasterInstrument.Name != "NQ" && Instrument.MasterInstrument.Name != "MNQ")
                    {
                        Draw.TextFixed(this, "InstrumentWarning", $"WARNING: NQ mode is designed for NQ/MNQ only!", TextPosition.Center);
                        Print($"WARNING: NQ mode is designed for NQ/MNQ only. Current instrument: "
                            + Instrument.MasterInstrument.Name);
                    }
                }
                else if (Mode == EngulfingScalperSettings.GC)
                {
                    // GC-specific settings
                    MaxStopLossPoints = 5;

                    MondayMode = DayTradingMode.Both;
                    TuesdayMode = DayTradingMode.Both;
                    WednesdayMode = DayTradingMode.Both;
                    ThursdayMode = DayTradingMode.Long;
                    FridayMode = DayTradingMode.Both;

                    // Day-specific settings for GC
                    MondayStopLossPercent = 100;
                    MondayTakeProfitPercent = 85;
                    TuesdayStopLossPercent = 100;
                    TuesdayTakeProfitPercent = 75;
                    WednesdayStopLossPercent = 100;
                    WednesdayTakeProfitPercent = 85;
                    ThursdayStopLossPercent = 100;
                    ThursdayTakeProfitPercent = 60;
                    FridayStopLossPercent = 100;
                    FridayTakeProfitPercent = 100;

                    // Day-specific settings for GC
                    MondayMinEngulfingPercent = 0;
                    MondayMaxEngulfingPercent = 2;
                    TuesdayMinEngulfingPercent = 0;
                    TuesdayMaxEngulfingPercent = 2;
                    WednesdayMinEngulfingPercent = 0;
                    WednesdayMaxEngulfingPercent = 2;
                    ThursdayMinEngulfingPercent = 0;
                    ThursdayMaxEngulfingPercent = 2;
                    FridayMinEngulfingPercent = 0;
                    FridayMaxEngulfingPercent = 2;

                    // GC-specific time settings
                    StartTime = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                    EndTime = DateTime.Parse("12:45", System.Globalization.CultureInfo.InvariantCulture);
                    EnableTradingTimes = true;

                    // GC-specific risk settings
                    EnableTargetRiskMode = false;
                    TargetRisk = 500;
                    dailyRiskLimit = 1000;

                    // Verify instrument compatibility
                    if (Instrument.MasterInstrument.Name != "GC" && Instrument.MasterInstrument.Name != "MGC")
                    {
                        Draw.TextFixed(this, "InstrumentWarning", $"WARNING: GC mode is designed for GC/MGC only!", TextPosition.Center);
                        Print($"WARNING: GC mode is designed for GC/MGC only. Current instrument: "
                            + Instrument.MasterInstrument.Name);
                    }
                }
                else if (Mode == EngulfingScalperSettings.Custom)
                {
                    // Custom mode - use user-defined settings
                    // No automatic overrides, user controls all parameters
                }

                // Legacy instrument warning for backward compatibility
                if (Mode == EngulfingScalperSettings.Custom &&
                    Instrument.MasterInstrument.Name != "NQ" && Instrument.MasterInstrument.Name != "MNQ")
                {
                    Draw.TextFixed(this, "InstrumentWarning", $"WARNING: This strategy is designed for NQ/MNQ only!", TextPosition.Center);
                    Print($"WARNING: {productName} strategy is designed to run on NQ/MNQ only. Current instrument: "
                        + Instrument.MasterInstrument.Name);
                }

                // Set mini mode flag based on instrument
                if (Instrument.MasterInstrument.Name == "MNQ" || Instrument.MasterInstrument.Name == "MGC")
                {
                    MiniMode = false; // Mini contracts
                }
                else if (Instrument.MasterInstrument.Name == "NQ" || Instrument.MasterInstrument.Name == "GC")
                {
                    MiniMode = true; // Full-size contracts
                }

                // Set instrument point value for calculations
                instrumentPointValue = Bars.Instrument.MasterInstrument.PointValue;
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
                RemoveDrawObject("TargetLevel" + "Engulfing High");
                RemoveDrawObject("TargetLevel" + "Engulfing Low");
                RemoveDrawObject("Label" + "Engulfing High");
                RemoveDrawObject("Label" + "Engulfing Low");
                #endregion

                lastTimeSession = 0;
                tradeCompleteSLPoints = 0;
                tradeCompleteTPPoints = 0;
                initialSetupTP = false;
                initialSetupSL = false;
                initialSetupEntry = false;
                engulfingDetected = false;
                orderPlaced = false;
            }
            #endregion

            #region Core Engine
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                // Reset engulfing state when flat
                engulfingDetected = false;
                orderPlaced = false;
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
                        if (detail.TradeExecName == "Long")
                            tradeDistance = " | Missed Points: " + (High[0] - detail.TradeExecPrice);
                        else if (detail.TradeExecName == "Short")
                            tradeDistance = " | Missed Points: " + (detail.TradeExecPrice - Low[0]);
                    }
                    else if (detail.TradeExecuteType == "Stop loss")
                    {
                        tradeCloseType = " - SL";
                        if (detail.TradeExecName == "Long")
                        {
                            tradeDistance = " | Loss Averted: " + (detail.TradeExecPrice - Low[0]);
                        }
                        else if (detail.TradeExecName == "Short")
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
                        if (detail.TradeExecName == "Long")
                        {
                            tradeInfo = "Long CLOSED (SL) at: ";
                        }
                        else if (detail.TradeExecName == "Short")
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
                tradeCompleteSLPoints = RoundToNearestTick(currentTradePnL / partialTradeQty / Bars.Instrument.MasterInstrument.PointValue);

                Print(Time[0] + " [PNL UPDATE] COMPLETED TRADE PnL: $" + currentTradePnL + " | Total PnL: $" + currentPnL);
                currentTradePnL = 0;
                newTradeCalculated = false;
                barsInTrade = 0;
            }

            if (Position.MarketPosition != MarketPosition.Flat)
            {
                barsInTrade++;
            }

            double realtimPnL = Math.Round(currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]), 1);
            #endregion
            #endregion

            #region Engulfing Logic
            BuyTradeEnabled = false;
            SellTradeEnabled = false;

            if (IsTradingDayEnabled() && IsWithinTradingHours() && !IsDailyRiskLimitExceeded() && Position.MarketPosition == MarketPosition.Flat && !orderPlaced)
            {
                // Check for engulfing pattern (current candle engulfs previous candle)
                if (CurrentBar >= 1)
                {
                    double prevOpen = Open[1];
                    double prevClose = Close[1];
                    double prevBodySize = Math.Abs(prevClose - prevOpen);

                    double currOpen = Open[0];
                    double currClose = Close[0];
                    double currBodySize = Math.Abs(currClose - currOpen);

                    // Check if current candle body fully encompasses previous candle body
                    if (currBodySize > prevBodySize)
                    {
                        bool isEngulfing = false;

                        if (currClose > currOpen) // Current candle is bullish (green)
                        {
                            // Bullish engulfing: current high/low encompasses previous high/low
                            if (currOpen <= prevClose && currClose >= prevOpen && prevClose < prevOpen)
                            {
                                isEngulfing = true;
                                engulfingLong = true;
                            }
                        }
                        else // Current candle is bearish (red)
                        {
                            // Bearish engulfing: current high/low encompasses previous high/low
                            if (currOpen >= prevClose && currClose <= prevOpen && prevClose > prevOpen)
                            {
                                isEngulfing = true;
                                engulfingLong = false;
                            }
                        }

                        if (isEngulfing)
                        {
                            // Check if engulfing body size is within allowed range for current day
                            double currentDayMinEngulfingPercent = GetCurrentDayMinEngulfingPercent();
                            double currentDayMaxEngulfingPercent = GetCurrentDayMaxEngulfingPercent();
                            double currentMarketPrice = Close[0];
                            double engulfingBodySizePercent = (currBodySize / currentMarketPrice) * 100;

                            // Validate engulfing size is within allowed range
                            if (engulfingBodySizePercent >= currentDayMinEngulfingPercent && engulfingBodySizePercent <= currentDayMaxEngulfingPercent)
                            {
                                engulfingDetected = true;
                                engulfingBodySize = currBodySize;
                                engulfingHigh = Math.Max(currOpen, currClose);
                                engulfingLow = Math.Min(currOpen, currClose);
                                engulfingBar = CurrentBar;
                                executionCount++;

                                Print(Time[0] + " [ENGULFING DETECTED] " + (engulfingLong ? "BULLISH" : "BEARISH") +
                                    " | Body Size: " + engulfingBodySize + " | Body Size %: " + engulfingBodySizePercent.ToString("F2") + "%" +
                                    " | High: " + engulfingHigh + " | Low: " + engulfingLow);

                                if (!DisableGraphics)
                                {
                                    Draw.Line(this, "Engulfing High" + executionCount, CurrentBar - engulfingBar, engulfingHigh, 0, engulfingHigh, Brushes.Green);
                                    Draw.Line(this, "Engulfing Low" + executionCount, CurrentBar - engulfingBar, engulfingLow, 0, engulfingLow, Brushes.Red);
                                }

                                // Enable trading based on direction and day settings
                                if (engulfingLong && IsDirectionEnabled(Time[0].DayOfWeek, true))
                                {
                                    BuyTradeEnabled = true;
                                    orderPlaced = true;
                                    Print(Time[0] + " [ENGULFING TRADE] LONG TRADE ENABLED - Bullish engulfing detected");
                                }
                                else if (!engulfingLong && IsDirectionEnabled(Time[0].DayOfWeek, false))
                                {
                                    SellTradeEnabled = true;
                                    orderPlaced = true;
                                    Print(Time[0] + " [ENGULFING TRADE] SHORT TRADE ENABLED - Bearish engulfing detected");
                                }
                            }
                            else
                            {
                                Print(Time[0] + " [ENGULFING REJECTED] Body size " + engulfingBodySizePercent.ToString("F2") +
                                    "% is outside allowed range [" + currentDayMinEngulfingPercent + "%, " + currentDayMaxEngulfingPercent + "%] for " +
                                    ConvertBarTimeToEST(Time[0]).DayOfWeek);
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
                // Set TP/SL based on engulfing levels
                if (BuyTradeEnabled || SellTradeEnabled)
                {
                    double stopLossPoints = 0;
                    double takeProfitPoints = 0;
                    double currentDaySLPercent = GetCurrentDayStopLossPercent();
                    double currentDayTPPercent = GetCurrentDayTakeProfitPercent();

                    if (BuyTradeEnabled)
                    {
                        // Long trade: SL and TP based on engulfing candle body size
                        stopLossPoints = Math.Min(engulfingBodySize * (currentDaySLPercent / 100), MaxStopLossPoints);
                        takeProfitPoints = engulfingBodySize * (currentDayTPPercent / 100);
                    }
                    else if (SellTradeEnabled)
                    {
                        // Short trade: SL and TP based on engulfing candle body size
                        stopLossPoints = Math.Min(engulfingBodySize * (currentDaySLPercent / 100), MaxStopLossPoints);
                        takeProfitPoints = engulfingBodySize * (currentDayTPPercent / 100);
                    }

                    // Calculate position size based on mode
                    int calculatedQuantity = CalculatePositionSize(stopLossPoints);

                    // Set stop loss and take profit
                    SetStopLoss("Long", CalculationMode.Price, Close[0] - stopLossPoints, false);
                    SetStopLoss("Short", CalculationMode.Price, Close[0] + stopLossPoints, false);

                    SetProfitTarget("Long", CalculationMode.Price, Close[0] + takeProfitPoints, false);
                    SetProfitTarget("Short", CalculationMode.Price, Close[0] - takeProfitPoints, false);
                }
            }
            #endregion

            #region Close/Trim Trades
            string tradeCloseReason = "";
            CloseBuyTrade = false;
            CloseSellTrade = false;

            if (CloseBuyTrade)
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
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (BuyTradeEnabled)
                {
                    double currentDaySLPercent = GetCurrentDayStopLossPercent();
                    double stopLossPoints = Math.Min(engulfingBodySize * (currentDaySLPercent / 100), MaxStopLossPoints);
                    int calculatedQuantity = CalculatePositionSize(stopLossPoints);

                    Print(Time[0] + " [ENGULFING TRADE] LONG ENTRY at: " + Close[0] + " | Quantity: " + calculatedQuantity +
                        " | SL: " + (Close[0] - stopLossPoints) + " | Mode: " + (EnableTargetRiskMode ? "Risk" : "Fixed"));
                    EnterLong(calculatedQuantity, "Long");
                }
                else if (SellTradeEnabled)
                {
                    double currentDaySLPercent = GetCurrentDayStopLossPercent();
                    double stopLossPoints = Math.Min(engulfingBodySize * (currentDaySLPercent / 100), MaxStopLossPoints);
                    int calculatedQuantity = CalculatePositionSize(stopLossPoints);

                    Print(Time[0] + " [ENGULFING TRADE] SHORT ENTRY at: " + Close[0] + " | Quantity: " + calculatedQuantity +
                        " | SL: " + (Close[0] + stopLossPoints) + " | Mode: " + (EnableTargetRiskMode ? "Risk" : "Fixed"));
                    EnterShort(calculatedQuantity, "Short");
                }
            }
            #endregion

            #endregion
            #region Dashboard
            string dashBoard =
                $"PnL: $"
                + realtimPnL.ToString()
                + " | Trading: "
                + (IsTradingDayEnabled() ? "Active" : "Off");

            string engulfingStatus = engulfingDetected ? (engulfingLong ? "Bullish Engulfing" : "Bearish Engulfing") : "No Pattern";
            dashBoard += $"\n Status: {engulfingStatus}";

            // Show position sizing mode
            string positionMode = EnableTargetRiskMode ? $"Risk Mode: ${TargetRisk}" : $"Fixed: {FixedContractCount} contracts";
            dashBoard += $"\n Position: {positionMode}";

            if (engulfingBodySize > 0)
            {
                dashBoard += $"\n Engulfing Size: {engulfingBodySize:F2}";
                double currentDaySLPercent = GetCurrentDayStopLossPercent();
                double currentDayTPPercent = GetCurrentDayTakeProfitPercent();
                dashBoard += $" | SL: {currentDaySLPercent}% | TP: {currentDayTPPercent}%";
            }

            // Show current day's engulfing size parameters
            double dashboardMinEngulfingPercent = GetCurrentDayMinEngulfingPercent();
            double dashboardMaxEngulfingPercent = GetCurrentDayMaxEngulfingPercent();
            dashBoard += $"\n Engulfing Size Range: {dashboardMinEngulfingPercent}% - {dashboardMaxEngulfingPercent}%";

            if (Position.MarketPosition != MarketPosition.Flat)
            {
                double riskAmount = (Position.MarketPosition == MarketPosition.Long ?
                    (Position.AveragePrice - currentSL) : (currentSL - Position.AveragePrice)) *
                    Position.Quantity * Bars.Instrument.MasterInstrument.PointValue;
                dashBoard += $" | Risk: ${Math.Round(riskAmount, 1)}";
            }

            // Show daily risk limit status
            if (dailyRiskLimit > 0)
            {
                double riskRemaining = dailyRiskLimit - Math.Abs(currentPnL);
                dashBoard += $"\n Daily Risk: ${Math.Round(currentPnL, 1)} / ${dailyRiskLimit} (${Math.Round(riskRemaining, 1)} remaining)";

                if (IsDailyRiskLimitExceeded())
                {
                    dashBoard += " [LIMIT EXCEEDED]";
                }
            }

            if (!DisableGraphics)
            {
                Draw.TextFixed(this, "Dashboard", dashBoard, TextPosition.BottomRight);
                Draw.TextFixed(this, "Mode", Mode.ToString() + " | " + (MiniMode ? "Full Size" : "Mini"), TextPosition.BottomLeft);
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

            if (entryOrderShort != null && entryOrderShort.IsBacktestOrder && State == State.Realtime)
                entryOrderShort = GetRealtimeOrder(entryOrderShort);

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

        #region Day Functions
        private bool IsTradingDayEnabled()
        {
            DayOfWeek currentDay = ConvertBarTimeToEST(Time[0]).DayOfWeek;

            switch (currentDay)
            {
                case DayOfWeek.Monday:
                    return MondayMode != DayTradingMode.Disabled;
                case DayOfWeek.Tuesday:
                    return TuesdayMode != DayTradingMode.Disabled;
                case DayOfWeek.Wednesday:
                    return WednesdayMode != DayTradingMode.Disabled;
                case DayOfWeek.Thursday:
                    return ThursdayMode != DayTradingMode.Disabled;
                case DayOfWeek.Friday:
                    return FridayMode != DayTradingMode.Disabled;
                default:
                    return false; // No trading on weekends
            }
        }

        private bool IsDirectionEnabled(DayOfWeek day, bool isBuy)
        {
            DayTradingMode mode = DayTradingMode.Disabled;

            switch (day)
            {
                case DayOfWeek.Monday:
                    mode = MondayMode;
                    break;
                case DayOfWeek.Tuesday:
                    mode = TuesdayMode;
                    break;
                case DayOfWeek.Wednesday:
                    mode = WednesdayMode;
                    break;
                case DayOfWeek.Thursday:
                    mode = ThursdayMode;
                    break;
                case DayOfWeek.Friday:
                    mode = FridayMode;
                    break;
            }

            if (isBuy)
                return mode == DayTradingMode.Long || mode == DayTradingMode.Both;
            else
                return mode == DayTradingMode.Short || mode == DayTradingMode.Both;
        }

        private double GetCurrentDayStopLossPercent()
        {
            DayOfWeek currentDay = ConvertBarTimeToEST(Time[0]).DayOfWeek;

            switch (currentDay)
            {
                case DayOfWeek.Monday:
                    return MondayStopLossPercent;
                case DayOfWeek.Tuesday:
                    return TuesdayStopLossPercent;
                case DayOfWeek.Wednesday:
                    return WednesdayStopLossPercent;
                case DayOfWeek.Thursday:
                    return ThursdayStopLossPercent;
                case DayOfWeek.Friday:
                    return FridayStopLossPercent;
                default:
                    return 100; // Use default if weekend
            }
        }

        private double GetCurrentDayTakeProfitPercent()
        {
            DayOfWeek currentDay = ConvertBarTimeToEST(Time[0]).DayOfWeek;

            switch (currentDay)
            {
                case DayOfWeek.Monday:
                    return MondayTakeProfitPercent;
                case DayOfWeek.Tuesday:
                    return TuesdayTakeProfitPercent;
                case DayOfWeek.Wednesday:
                    return WednesdayTakeProfitPercent;
                case DayOfWeek.Thursday:
                    return ThursdayTakeProfitPercent;
                case DayOfWeek.Friday:
                    return FridayTakeProfitPercent;
                default:
                    return 75; // Use default if weekend
            }
        }

        private double GetCurrentDayMinEngulfingPercent()
        {
            DayOfWeek currentDay = ConvertBarTimeToEST(Time[0]).DayOfWeek;

            switch (currentDay)
            {
                case DayOfWeek.Monday:
                    return MondayMinEngulfingPercent;
                case DayOfWeek.Tuesday:
                    return TuesdayMinEngulfingPercent;
                case DayOfWeek.Wednesday:
                    return WednesdayMinEngulfingPercent;
                case DayOfWeek.Thursday:
                    return ThursdayMinEngulfingPercent;
                case DayOfWeek.Friday:
                    return FridayMinEngulfingPercent;
                default:
                    return 0; // Use default if weekend
            }
        }

        private double GetCurrentDayMaxEngulfingPercent()
        {
            DayOfWeek currentDay = ConvertBarTimeToEST(Time[0]).DayOfWeek;

            switch (currentDay)
            {
                case DayOfWeek.Monday:
                    return MondayMaxEngulfingPercent;
                case DayOfWeek.Tuesday:
                    return TuesdayMaxEngulfingPercent;
                case DayOfWeek.Wednesday:
                    return WednesdayMaxEngulfingPercent;
                case DayOfWeek.Thursday:
                    return ThursdayMaxEngulfingPercent;
                case DayOfWeek.Friday:
                    return FridayMaxEngulfingPercent;
                default:
                    return 2; // Use default if weekend
            }
        }

        private DateTime ConvertBarTimeToEST(DateTime barTime)
        {
            // Define the Eastern Standard Time zone
            TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            // Convert the bar time to EST
            DateTime barTimeInEST = TimeZoneInfo.ConvertTime(barTime, estTimeZone);

            return barTimeInEST;
        }

        private bool IsWithinTradingHours()
        {
            if (!EnableTradingTimes)
                return true;

            DateTime barTime = ConvertBarTimeToEST(Time[0]);
            TimeSpan barTimeOfDay = barTime.TimeOfDay;
            TimeSpan start = StartTime.TimeOfDay;
            TimeSpan end = EndTime.TimeOfDay;

            bool isWithinHours;
            if (start < end)
            {
                // Session does not cross midnight
                isWithinHours = barTimeOfDay >= start && barTimeOfDay <= end;
            }
            else
            {
                // Session crosses midnight
                isWithinHours = barTimeOfDay >= start || barTimeOfDay <= end;
            }

            return isWithinHours;
        }

        private bool IsDailyRiskLimitExceeded()
        {
            if (dailyRiskLimit <= 0)
                return false;

            return Math.Abs(currentPnL) >= dailyRiskLimit;
        }
        #endregion

        #region Position Sizing Functions
        private int CalculatePositionSize(double stopLossPoints)
        {
            if (!EnableTargetRiskMode)
            {
                // Fixed contract count mode
                return FixedContractCount;
            }

            // Target risk mode - Calculate position size based on target risk
            double pointValue = instrumentPointValue > 0 ? instrumentPointValue : Bars.Instrument.MasterInstrument.PointValue;
            double riskPerContract = stopLossPoints * pointValue;

            if (riskPerContract <= 0)
                return 1;

            int quantity = (int)(TargetRisk / riskPerContract);

            // Ensure minimum quantity of 1
            if (quantity < 1)
                quantity = 1;

            return quantity;
        }
        #endregion

        #region General Functions
        private double RoundToNearestTick(double price)
        {
            double tickSize = Instrument.MasterInstrument.TickSize;
            return Math.Round(price / tickSize) * tickSize;
        }
        #endregion
        #endregion
    }

    #region Enums
    public enum EngulfingScalperSettings
    {
        NQ = 1,
        GC = 2,
        Custom = 3,
    }
    #endregion
}
