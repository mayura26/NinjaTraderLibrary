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
    public class ORBxx : Strategy
    {
        private string productName = "ORBxx";
        #region Properties
        #region Main Parameters
        [NinjaScriptProperty]
        [Display(Name = "Mode", Description = "Mode to trade", Order = 1, GroupName = "1. Main Parameters")]
        public ORBMode Mode
        { get; set; } = ORBMode.FifteenMinutes;

        [NinjaScriptProperty]
        [Display(Name = "Enable Target Risk Mode", Description = "Enable position sizing based on target risk, otherwise use fixed contract count", Order = 2, GroupName = "1. Main Parameters")]
        public bool EnableTargetRiskMode
        { get; set; } = true;

        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Target Risk", Description = "Target risk per trade in dollars", Order = 3, GroupName = "1. Main Parameters")]
        public double TargetRisk
        { get; set; } = 500;

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Fixed Contract Count", Description = "Number of contracts to trade when target risk mode is disabled", Order = 4, GroupName = "1. Main Parameters")]
        public int FixedContractCount
        { get; set; } = 5;
        #endregion

        #region ORB Parameters
        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Max Stop Loss (Points)", Description = "Maximum stop loss size in points", Order = 1, GroupName = "2. ORB Parameters")]
        public double MaxStopLossPoints
        { get; set; } = 50;

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Take Profit % of ORB", Description = "Take profit as percentage of ORB size", Order = 2, GroupName = "2. ORB Parameters")]
        public double TakeProfitPercent
        { get; set; } = 55;

        [NinjaScriptProperty]
        [Display(Name = "Max Gain", Description = "Maximum gain per trade in dollars", Order = 3, GroupName = "2. ORB Parameters")]
        public double MaxGain
        { get; set; } = 500;
        #endregion

        #region Day Selection Parameters
        [NinjaScriptProperty]
        [Display(Name = "Monday", Description = "Trading mode for Monday", Order = 1, GroupName = "3. Day Selection")]
        public DayTradingMode MondayMode
        { get; set; } = DayTradingMode.Long;

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
        { get; set; } = DayTradingMode.Long;

        [NinjaScriptProperty]
        [Display(Name = "Friday", Description = "Trading mode for Friday", Order = 5, GroupName = "3. Day Selection")]
        public DayTradingMode FridayMode
        { get; set; } = DayTradingMode.Both;

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Monday Max ORB %", Description = "Maximum ORB size as percentage of price for Monday (0 = disabled)", Order = 6, GroupName = "3. Day Selection")]
        public double MondayMaxORBPercent
        { get; set; } = 3.0;

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Tuesday Max ORB %", Description = "Maximum ORB size as percentage of price for Tuesday (0 = disabled)", Order = 7, GroupName = "3. Day Selection")]
        public double TuesdayMaxORBPercent
        { get; set; } = 2.0;

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Wednesday Max ORB %", Description = "Maximum ORB size as percentage of price for Wednesday (0 = disabled)", Order = 8, GroupName = "3. Day Selection")]
        public double WednesdayMaxORBPercent
        { get; set; } = 1.0;

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Thursday Max ORB %", Description = "Maximum ORB size as percentage of price for Thursday (0 = disabled)", Order = 9, GroupName = "3. Day Selection")]
        public double ThursdayMaxORBPercent
        { get; set; } = 1.0;

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Friday Max ORB %", Description = "Maximum ORB size as percentage of price for Friday (0 = disabled)", Order = 10, GroupName = "3. Day Selection")]
        public double FridayMaxORBPercent
        { get; set; } = 0.5;
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
        private bool MiniMode = false;

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

        // ORB-specific variables
        private double ORBHigh;
        private double ORBLow;
        private int ORBFirstBar;
        private int PostORBFirstBar;
        private bool ORBStarted = false;
        private bool PostORBStarted = false;
        private bool ORBTradeLong = false;
        private bool ORBTradeShort = false;
        private bool OrderPlaced = false;
        private int executionCount = 0;
        private DateTime ORBStartTime;
        private DateTime ORBEndTime;
        private DateTime SessionEndTime = DateTime.Parse("16:45", System.Globalization.CultureInfo.InvariantCulture);
        private double currentORBSize = 0;
        private double currentDayMaxORBPercent = 0;
        #endregion

        #region Initialization
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = productName + @" - ORB Breakout Strategy";
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

                // Verify instrument based on mode
                string expectedInstrument = "";
                int expectedTimeframe = 1;
                if (Mode == ORBMode.FiveMinutes)
                {
                    ORBStartTime = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                    ORBEndTime = DateTime.Parse("09:45", System.Globalization.CultureInfo.InvariantCulture);
                    expectedTimeframe = 5;
                }
                else if (Mode == ORBMode.FifteenMinutes)
                {
                    ORBStartTime = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                    ORBEndTime = DateTime.Parse("09:45", System.Globalization.CultureInfo.InvariantCulture);
                    expectedTimeframe = 5;
                }
                else if (Mode == ORBMode.ThirtyMinutes)
                {
                    ORBStartTime = DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
                    ORBEndTime = DateTime.Parse("09:45", System.Globalization.CultureInfo.InvariantCulture);
                    expectedTimeframe = 5;
                }

                // Verify timeframe (should be 1-minute for ORB strategy)
                if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != expectedTimeframe)
                {
                    string expectedTimeframeString = BarsPeriodType.Minute.ToString() + " " + expectedTimeframe.ToString();
                    Draw.TextFixed(this, "TimeframeWarning", $"WARNING: This strategy is designed for {expectedTimeframeString} charts only!", TextPosition.Center);
                    Print($"WARNING: {productName} strategy is designed to run on {expectedTimeframeString} timeframe only. Current timeframe: "
                        + BarsPeriod.BarsPeriodType.ToString() + " " + BarsPeriod.Value.ToString());
                }

                if (Instrument.MasterInstrument.Name != "NQ" && Instrument.MasterInstrument.Name != "MNQ")
                {
                    Draw.TextFixed(this, "InstrumentWarning", $"WARNING: This strategy is designed for {expectedInstrument} only!", TextPosition.Center);
                    Print($"WARNING: {productName} strategy is designed to run on {expectedInstrument} only. Current instrument: "
                        + Instrument.MasterInstrument.Name);
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

                lastTimeSession = 0;
                tradeCompleteSLPoints = 0;
                tradeCompleteTPPoints = 0;
                initialSetupTP = false;
                initialSetupSL = false;
                initialSetupEntry = false;
                currentORBSize = 0;
                currentDayMaxORBPercent = 0;
            }
            #endregion

            #region Core Engine
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                // Reset ORB state when flat
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

            #region ORB Logic
            BuyTradeEnabled = false;
            SellTradeEnabled = false;

            if (IsTradingDayEnabled())
            {
                if (IsORBPeriod())
                {
                    if (!ORBStarted)
                    {
                        ORBStarted = true;
                        ORBFirstBar = CurrentBar;
                        ORBHigh = High[0];
                        ORBLow = Low[0];
                    }

                    // Update ORB high and low
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
                    // Reset ORB state before ORB period
                    OrderPlaced = false;
                    ORBStarted = false;
                    PostORBStarted = false;
                    ORBTradeLong = false;
                    ORBTradeShort = false;
                    initialSetupTP = false;
                    initialSetupSL = false;
                    initialSetupEntry = false;
                    currentORBSize = 0;
                    currentDayMaxORBPercent = 0;

                    ORBHigh = double.MinValue;
                    ORBLow = double.MaxValue;
                }
                else if (IsPostORBPeriod())
                {
                    if (!PostORBStarted)
                    {
                        executionCount++;
                        PostORBStarted = true;
                        PostORBFirstBar = CurrentBar;
                        
                        // Calculate ORB size and check if it exceeds maximum allowed percentage
                        currentORBSize = ORBHigh - ORBLow;
                        currentDayMaxORBPercent = GetCurrentDayMaxORBPercent();
                        if (currentDayMaxORBPercent > 0)
                        {
                            double maxORBSize = ORBHigh * (currentDayMaxORBPercent / 100);
                            if (currentORBSize > maxORBSize)
                            {
                                Print(Time[0] + " [ORB SIZE CHECK] ORB size " + currentORBSize + " exceeds maximum allowed " + maxORBSize + " (" + currentDayMaxORBPercent + "% of " + ORBHigh + ") - No trades allowed");
                                OrderPlaced = true; // Prevent any trades
                                return;
                            }
                        }
                    }

                    if (!OrderPlaced)
                    {
                        // Check for breakout entries
                        if (Close[0] > ORBHigh)
                        {
                            // Long breakout
                            if (IsDirectionEnabled(Time[0].DayOfWeek, true))
                            {
                                BuyTradeEnabled = true;
                                OrderPlaced = true;
                                ORBTradeLong = true;
                                Print(Time[0] + " [ORB BREAKOUT] LONG BREAKOUT DETECTED above ORB High: " + ORBHigh + " | ORB Size: " + currentORBSize);
                            }
                        }
                        else if (Close[0] < ORBLow)
                        {
                            // Short breakout
                            if (IsDirectionEnabled(Time[0].DayOfWeek, false))
                            {
                                SellTradeEnabled = true;
                                OrderPlaced = true;
                                ORBTradeShort = true;
                                Print(Time[0] + " [ORB BREAKOUT] SHORT BREAKOUT DETECTED below ORB Low: " + ORBLow + " | ORB Size: " + currentORBSize);
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
                // Set TP/SL based on ORB levels
                if (BuyTradeEnabled || SellTradeEnabled)
                {
                    double stopLossPoints = 0;
                    double takeProfitPoints = 0;

                    if (BuyTradeEnabled)
                    {
                        // Long trade: SL at ORB low, TP at 55% of ORB size
                        stopLossPoints = Math.Min(Close[0] - ORBLow, MaxStopLossPoints);
                        double orBasedTP = (ORBHigh - ORBLow) * (TakeProfitPercent / 100);
                        
                        // Calculate max gain in points based on MaxGain and position size
                        int tempQuantity = CalculatePositionSize(stopLossPoints);
                        double maxGainPoints = MaxGain / (tempQuantity * Bars.Instrument.MasterInstrument.PointValue);
                        
                        // Use the smaller of the two: ORB-based TP or max gain TP
                        takeProfitPoints = Math.Min(orBasedTP, maxGainPoints);
                    }
                    else if (SellTradeEnabled)
                    {
                        // Short trade: SL at ORB high, TP at 55% of ORB size
                        stopLossPoints = Math.Min(ORBHigh - Close[0], MaxStopLossPoints);
                        double orBasedTP = (ORBHigh - ORBLow) * (TakeProfitPercent / 100);
                        
                        // Calculate max gain in points based on MaxGain and position size
                        int tempQuantity = CalculatePositionSize(stopLossPoints);
                        double maxGainPoints = MaxGain / (tempQuantity * Bars.Instrument.MasterInstrument.PointValue);
                        
                        // Use the smaller of the two: ORB-based TP or max gain TP
                        takeProfitPoints = Math.Min(orBasedTP, maxGainPoints);
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
                    double stopLossPoints = Math.Min(Close[0] - ORBLow, MaxStopLossPoints);
                    int calculatedQuantity = CalculatePositionSize(stopLossPoints);

                    Print(Time[0] + " [ORB BREAKOUT] LONG ENTRY at: " + Close[0] + " | Quantity: " + calculatedQuantity + " | SL: " + (Close[0] - stopLossPoints) + " | Mode: " + (EnableTargetRiskMode ? "Risk" : "Fixed"));
                    EnterLong(calculatedQuantity, "Long");
                }
                else if (SellTradeEnabled)
                {
                    double stopLossPoints = Math.Min(ORBHigh - Close[0], MaxStopLossPoints);
                    int calculatedQuantity = CalculatePositionSize(stopLossPoints);

                    Print(Time[0] + " [ORB BREAKOUT] SHORT ENTRY at: " + Close[0] + " | Quantity: " + calculatedQuantity + " | SL: " + (Close[0] + stopLossPoints) + " | Mode: " + (EnableTargetRiskMode ? "Risk" : "Fixed"));
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

            string orbStatus = IsPreORBPeriod() ? "Pre ORB" : IsORBPeriod() ? "ORB Active" : IsPostORBPeriod() ? "Post ORB" : "Flat";
            dashBoard += $"\n Status: {orbStatus}";

            // Show position sizing mode
            string positionMode = EnableTargetRiskMode ? $"Risk Mode: ${TargetRisk}" : $"Fixed: {FixedContractCount} contracts";
            dashBoard += $"\n Position: {positionMode}";

            if (currentORBSize > 0)
            {
                dashBoard += $"\n ORB Size: {currentORBSize:F2}";
                if (currentDayMaxORBPercent > 0)
                {
                    double maxORBSize = ORBHigh * (currentDayMaxORBPercent / 100);
                    dashBoard += $" | Max: {maxORBSize:F2} ({currentDayMaxORBPercent}%)";
                }
            }

            if (Position.MarketPosition != MarketPosition.Flat)
            {
                double riskAmount = (Position.MarketPosition == MarketPosition.Long ?
                    (Position.AveragePrice - currentSL) : (currentSL - Position.AveragePrice)) *
                    Position.Quantity * Bars.Instrument.MasterInstrument.PointValue;
                dashBoard += $" | Risk: ${Math.Round(riskAmount, 1)}";
            }

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

        #region Time Functions
        private bool IsORBPeriod()
        {
            TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;
            return barTime >= ORBStartTime.TimeOfDay && barTime <= ORBEndTime.TimeOfDay;
        }

        private bool IsPreORBPeriod()
        {
            TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;
            return barTime < ORBStartTime.TimeOfDay;
        }

        private bool IsPostORBPeriod()
        {
            TimeSpan barTime = ConvertBarTimeToEST(Time[0]).TimeOfDay;
            return barTime > ORBEndTime.TimeOfDay && barTime < SessionEndTime.TimeOfDay;
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

        #region ORB Functions
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

        private double GetCurrentDayMaxORBPercent()
        {
            DayOfWeek currentDay = ConvertBarTimeToEST(Time[0]).DayOfWeek;

            switch (currentDay)
            {
                case DayOfWeek.Monday:
                    return MondayMaxORBPercent;
                case DayOfWeek.Tuesday:
                    return TuesdayMaxORBPercent;
                case DayOfWeek.Wednesday:
                    return WednesdayMaxORBPercent;
                case DayOfWeek.Thursday:
                    return ThursdayMaxORBPercent;
                case DayOfWeek.Friday:
                    return FridayMaxORBPercent;
                default:
                    return 0.0; // No trading on weekends
            }
        }

        private int CalculatePositionSize(double stopLossPoints)
        {
            if (!EnableTargetRiskMode)
            {
                // Fixed contract count mode
                return FixedContractCount;
            }

            // Target risk mode - Calculate position size based on target risk
            double pointValue = Bars.Instrument.MasterInstrument.PointValue;
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
    public enum ORBMode
    {
        FiveMinutes = 1,
        FifteenMinutes = 2,
        ThirtyMinutes = 3
    }

    public enum DayTradingMode
    {
        Disabled = 0,
        Long = 1,
        Short = 2,
        Both = 3,
    }
    #endregion
}
