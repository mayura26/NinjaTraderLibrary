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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

/*
		TODO: [1] Add max loss and max gain properties
*/

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TiltDetect : Indicator
	{
		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "AccountName", Order = 1, GroupName = "1.Main")]
		public string AccountName
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "EnableDoubleDown", Order = 1, GroupName = "2. Double Down")]
		public bool EnableDoubleDown
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "DoubleDownMax", Order = 2, GroupName = "2. Double Down")]
		public int DoubleDownMax
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "EnableReenter", Order = 3, GroupName = "3. Reenter")]
		public bool EnableReenter
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ReenterMax", Order = 4, GroupName = "3. Reenter")]
		public int ReenterMax
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EnableMaxContracts", Order = 5, GroupName = "4. Max Contracts")]
		public int EnableMaxContracts
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "MaxContracts", Order = 6, GroupName = "4. Max Contracts")]
		public int MaxContracts
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "TargetContracts", Order = 7, GroupName = "4. Max Contracts")]
		public int TargetContracts
		{ get; set; }
		#endregion

		#region Variables
		private Account myAccount;

		private bool maxContractsReached = false;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Detects user tilt";
				Name = "TiltDetect";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive = true;
				AccountName = "Playback101";
				EnableDoubleDown = true;
				DoubleDownMax = 1;
				EnableReenter = true;
				ReenterMax = 1;
				EnableMaxContracts = 1;
				MaxContracts = 10;
				TargetContracts = 1;
			}
			else if (State == State.Configure)
			{
				lock (Account.All)
					myAccount = Account.All.FirstOrDefault(a => a.Name == AccountName);

				if (myAccount == null)
				{
					Print("myAccount is null");
				}
				else
				{
					Print($"Account found called: {myAccount.Name}");
					myAccount.ExecutionUpdate += OnExecutionUpdate;
					myAccount.PositionUpdate += OnPositionUpdate;
				}
			}
			else if (State == State.Terminated)
			{
				if (myAccount != null)
				{
					// Unsubscribe to events
					myAccount.ExecutionUpdate -= OnExecutionUpdate;
					myAccount.PositionUpdate -= OnPositionUpdate;
					Print($"Unsubscribed to events for {myAccount.Name}");
				}
			}
		}

		protected override void OnBarUpdate()
		{
			string tiltText = "";
			if (maxContractsReached)
			{
				tiltText = "Max Contracts Reached";
			}
			
			Draw.TextFixed(this, "TiltDetect", tiltText, TextPosition.BottomRight);
		}

		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			// Do something with the execution update
			//Print($"Execution update: {e.Execution}");
		}

		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			//Print($"Position update: {e.Position}");
			// Check if the position size is greater than the max contracts
			if (e.Quantity > MaxContracts)
			{
				Print($"Position size is greater than the max contracts: {e.Quantity}");
				maxContractsReached = true;
			}
			else
			{
				maxContractsReached = false;
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TiltDetect[] cacheTiltDetect;
		public TiltDetect TiltDetect(string accountName, bool enableDoubleDown, int doubleDownMax, bool enableReenter, int reenterMax, int enableMaxContracts, int maxContracts, int targetContracts)
		{
			return TiltDetect(Input, accountName, enableDoubleDown, doubleDownMax, enableReenter, reenterMax, enableMaxContracts, maxContracts, targetContracts);
		}

		public TiltDetect TiltDetect(ISeries<double> input, string accountName, bool enableDoubleDown, int doubleDownMax, bool enableReenter, int reenterMax, int enableMaxContracts, int maxContracts, int targetContracts)
		{
			if (cacheTiltDetect != null)
				for (int idx = 0; idx < cacheTiltDetect.Length; idx++)
					if (cacheTiltDetect[idx] != null && cacheTiltDetect[idx].AccountName == accountName && cacheTiltDetect[idx].EnableDoubleDown == enableDoubleDown && cacheTiltDetect[idx].DoubleDownMax == doubleDownMax && cacheTiltDetect[idx].EnableReenter == enableReenter && cacheTiltDetect[idx].ReenterMax == reenterMax && cacheTiltDetect[idx].EnableMaxContracts == enableMaxContracts && cacheTiltDetect[idx].MaxContracts == maxContracts && cacheTiltDetect[idx].TargetContracts == targetContracts && cacheTiltDetect[idx].EqualsInput(input))
						return cacheTiltDetect[idx];
			return CacheIndicator<TiltDetect>(new TiltDetect(){ AccountName = accountName, EnableDoubleDown = enableDoubleDown, DoubleDownMax = doubleDownMax, EnableReenter = enableReenter, ReenterMax = reenterMax, EnableMaxContracts = enableMaxContracts, MaxContracts = maxContracts, TargetContracts = targetContracts }, input, ref cacheTiltDetect);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TiltDetect TiltDetect(string accountName, bool enableDoubleDown, int doubleDownMax, bool enableReenter, int reenterMax, int enableMaxContracts, int maxContracts, int targetContracts)
		{
			return indicator.TiltDetect(Input, accountName, enableDoubleDown, doubleDownMax, enableReenter, reenterMax, enableMaxContracts, maxContracts, targetContracts);
		}

		public Indicators.TiltDetect TiltDetect(ISeries<double> input , string accountName, bool enableDoubleDown, int doubleDownMax, bool enableReenter, int reenterMax, int enableMaxContracts, int maxContracts, int targetContracts)
		{
			return indicator.TiltDetect(input, accountName, enableDoubleDown, doubleDownMax, enableReenter, reenterMax, enableMaxContracts, maxContracts, targetContracts);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TiltDetect TiltDetect(string accountName, bool enableDoubleDown, int doubleDownMax, bool enableReenter, int reenterMax, int enableMaxContracts, int maxContracts, int targetContracts)
		{
			return indicator.TiltDetect(Input, accountName, enableDoubleDown, doubleDownMax, enableReenter, reenterMax, enableMaxContracts, maxContracts, targetContracts);
		}

		public Indicators.TiltDetect TiltDetect(ISeries<double> input , string accountName, bool enableDoubleDown, int doubleDownMax, bool enableReenter, int reenterMax, int enableMaxContracts, int maxContracts, int targetContracts)
		{
			return indicator.TiltDetect(input, accountName, enableDoubleDown, doubleDownMax, enableReenter, reenterMax, enableMaxContracts, maxContracts, targetContracts);
		}
	}
}

#endregion
