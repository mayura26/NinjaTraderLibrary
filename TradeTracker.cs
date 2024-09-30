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
using System.Net;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TradeTracker : Indicator
	{
		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "AccountName", Order = 1, GroupName = "1.Main")]
		public string AccountName
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "EnableOutputToDiscord", Description = "Enable output to Discord", Order = 2, GroupName = "1.Main")]
		public bool EnableOutputToDiscord
		{ get; set; }
		#endregion
		private Account myAccount;
		private MarketPosition currentPosition;
		private int currentQuantity;

		private bool positionChanged = false;

		private string webhookUrl = "https://discord.com/api/webhooks/1286486933744390154/yPFqGjlstvxVpQar5n6vfwsmIzVFWvh0mt0z2z_op7LxdyUzZQi2apPwNllW4wxxdJov"; // 8020 server
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Track trade to discord";
				Name = "TradeTracker";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive = true;

				// Properties
				AccountName = "Playback101";
				EnableOutputToDiscord = true;
			}
			else if (State == State.Configure)
			{
				// List all available accounts
				Print(" ************************************************** ");
				Print("Available accounts:");

				foreach (Account account in Account.All)
				{
					Print(account.Name);
				}
				Print(" ************************************************** ");

				lock (Account.All)
					myAccount = Account.All.FirstOrDefault(a => a.Name == AccountName);

				if (myAccount == null)
				{
					Print("Account is null");
				}
				else
				{
					Print($"Account found called: {myAccount.Name}");
					myAccount.PositionUpdate += OnPositionUpdate;
				}
			}
			else if (State == State.Terminated)
			{
				if (myAccount != null)
				{
					// Unsubscribe to events
					myAccount.PositionUpdate -= OnPositionUpdate;
					Print($"Unsubscribed to events for {myAccount.Name}");
				}
			}
		}

		private int tickCounter = 0;
		private int tickInterval = 40; // Check every 10 ticks, adjust as needed

		protected override void OnBarUpdate()
		{
			tickCounter++;

			if (tickCounter >= tickInterval)
			{
				tickCounter = 0;

				if (positionChanged)
				{
					if (currentPosition == MarketPosition.Long && currentQuantity > 0)
					{
						SendDiscordMessage($"LONG ({currentQuantity} contracts)");
						Print($"LONG ({currentQuantity} contracts)");
					}
					else if (currentPosition == MarketPosition.Short && currentQuantity > 0)
					{
						SendDiscordMessage($"SHORT ({currentQuantity} contracts)");
						Print($"SHORT ({currentQuantity} contracts)");
					}
					positionChanged = false;
				}
			}
		}

		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			// Store the current position state
			currentPosition = e.Position.MarketPosition;
			currentQuantity = e.Position.Quantity;

			if (currentPosition == MarketPosition.Flat || currentQuantity == 0)
			{
				SendDiscordMessage($"FLAT");
				Print($"FLAT");
			}

			positionChanged = true;
		}

		private void SendDiscordMessage(string message)
		{
			if (EnableOutputToDiscord && State == State.Realtime)
			{
				string jsonContent = $"{{\"content\": \"{message}\"}}";

				try
				{
					using (WebClient client = new WebClient())
					{
						client.Headers[HttpRequestHeader.ContentType] = "application/json";
						string response = client.UploadString(webhookUrl, "POST", jsonContent);
					}
				}
				catch (Exception e)
				{
					Print("Error sending to Discord: " + e.Message);
				}
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TradeTracker[] cacheTradeTracker;
		public TradeTracker TradeTracker(string accountName, bool enableOutputToDiscord)
		{
			return TradeTracker(Input, accountName, enableOutputToDiscord);
		}

		public TradeTracker TradeTracker(ISeries<double> input, string accountName, bool enableOutputToDiscord)
		{
			if (cacheTradeTracker != null)
				for (int idx = 0; idx < cacheTradeTracker.Length; idx++)
					if (cacheTradeTracker[idx] != null && cacheTradeTracker[idx].AccountName == accountName && cacheTradeTracker[idx].EnableOutputToDiscord == enableOutputToDiscord && cacheTradeTracker[idx].EqualsInput(input))
						return cacheTradeTracker[idx];
			return CacheIndicator<TradeTracker>(new TradeTracker(){ AccountName = accountName, EnableOutputToDiscord = enableOutputToDiscord }, input, ref cacheTradeTracker);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TradeTracker TradeTracker(string accountName, bool enableOutputToDiscord)
		{
			return indicator.TradeTracker(Input, accountName, enableOutputToDiscord);
		}

		public Indicators.TradeTracker TradeTracker(ISeries<double> input , string accountName, bool enableOutputToDiscord)
		{
			return indicator.TradeTracker(input, accountName, enableOutputToDiscord);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TradeTracker TradeTracker(string accountName, bool enableOutputToDiscord)
		{
			return indicator.TradeTracker(Input, accountName, enableOutputToDiscord);
		}

		public Indicators.TradeTracker TradeTracker(ISeries<double> input , string accountName, bool enableOutputToDiscord)
		{
			return indicator.TradeTracker(input, accountName, enableOutputToDiscord);
		}
	}
}

#endregion
