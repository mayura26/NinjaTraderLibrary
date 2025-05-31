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
using NinjaTrader.NinjaScript.Indicators;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class SRZones : Indicator
	{
		#region User-defined Properties
		[NinjaScriptProperty]
		[Range(4, 30)]
		[Display(Name = "Pivot Period", Order = 1, GroupName = "Settings")]
		public int Prd { get; set; } = 10;

		[NinjaScriptProperty]
		[Range(1, 8)]
		[Display(Name = "Maximum Channel Width %", Order = 3, GroupName = "Settings")]
		public int ChannelW { get; set; } = 2;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Strength", Order = 4, GroupName = "Settings")]
		public int MinStrength { get; set; } = 3;

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Maximum Number of S/R", Order = 5, GroupName = "Settings")]
		public int MaxNumSR { get; set; } = 4;

		[NinjaScriptProperty]
		[Range(100, 400)]
		[Display(Name = "Loopback Period", Order = 6, GroupName = "Settings")]
		public int Loopback { get; set; } = 290;

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Resistance Color", Order = 7, GroupName = "Colors")]
		public Brush ResCol { get; set; } = Brushes.Red;
		[Browsable(false)]
		public string ResColSerializable { get { return Serialize.BrushToString(ResCol); } set { ResCol = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Support Color", Order = 8, GroupName = "Colors")]
		public Brush SupCol { get; set; } = Brushes.Lime;
		[Browsable(false)]
		public string SupColSerializable { get { return Serialize.BrushToString(SupCol); } set { SupCol = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "In Channel Color", Order = 9, GroupName = "Colors")]
		public Brush InchCol { get; set; } = Brushes.Gray;
		[Browsable(false)]
		public string InchColSerializable { get { return Serialize.BrushToString(InchCol); } set { InchCol = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show SR Levels", Order = 10, GroupName = "Settings")]
		public bool ShowSRLevels { get; set; } = true;
		#endregion

		// Returns the pivot high value at the given bar if it is a pivot, otherwise null
		static public double? PivotHigh(ISeries<double> series, int bar, int leftBars, int rightBars)
		{
			if (series == null || leftBars <= 0 || rightBars <= 0)
				return null;
			int pivotRange = leftBars + rightBars;
			if (bar < rightBars || bar > series.Count - leftBars - 1)
				return null;

			// Build the window
			int windowStart = bar - rightBars;
			int windowEnd = bar + leftBars;
			double max = double.MinValue;
			int maxIdx = -1;
			for (int i = windowStart; i <= windowEnd; i++)
			{
				if (series[i] > max)
				{
					max = series[i];
					maxIdx = i;
				}
			}
			// The pivot is valid if the max is at the 'bar' position
			if (maxIdx == bar)
				return series[bar];
			return null;
		}

		// Returns the pivot low value at the given bar if it is a pivot, otherwise null
		static public double? PivotLow(ISeries<double> series, int bar, int leftBars, int rightBars)
		{
			if (series == null || leftBars <= 0 || rightBars <= 0)
				return null;
			int pivotRange = leftBars + rightBars;
			if (bar < rightBars || bar > series.Count - leftBars - 1)
				return null;

			// Build the window
			int windowStart = bar - rightBars;
			int windowEnd = bar + leftBars;
			double min = double.MaxValue;
			int minIdx = -1;
			for (int i = windowStart; i <= windowEnd; i++)
			{
				if (series[i] < min)
				{
					min = series[i];
					minIdx = i;
				}
			}
			// The pivot is valid if the min is at the 'bar' position
			if (minIdx == bar)
				return series[bar];
			return null;
		}

		// --- SR Channel Logic Implementation ---
		private List<double> pivotVals = new List<double>();
		private List<int> pivotLocs = new List<int>();
		private List<string> srRectTags = new List<string>();

		// Public class for SR levels
		public class SRLevel
		{
			public double Hi { get; set; }
			public double Lo { get; set; }
			public int Strength { get; set; }
			public SRLevel(double hi, double lo, int strength)
			{
				Hi = hi;
				Lo = lo;
				Strength = strength;
			}
		}

		// Public list of SR levels
		private List<SRLevel> srLevels = new List<SRLevel>();

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Support/Resistance Zones";
				Name = "SRZones";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Math.Max(Loopback, 300) + Prd)
				return;

			// 2. Get pivots at the correct offset (Prd bars ago)
			int pivotOffset = Prd; // This is the offset from the current bar
			double? ph = null, pl = null;
			if (CurrentBar >= (Loopback + Prd))
			{
				ph = PivotHigh(High, pivotOffset, Prd, Prd);
				pl = PivotLow(Low, pivotOffset, Prd, Prd);
			}

			// 3. Store pivots and their bar indices, remove old ones
			if (ph.HasValue || pl.HasValue)
			{
				pivotVals.Insert(0, ph.HasValue ? ph.Value : pl.Value);
				pivotLocs.Insert(0, CurrentBar - pivotOffset);
				// Remove old pivots
				for (int x = pivotVals.Count - 1; x >= 0; x--)
				{
					if (CurrentBar - pivotLocs[x] > Loopback)
					{
						pivotVals.RemoveAt(x);
						pivotLocs.RemoveAt(x);
					}
					else break;
				}
			}

			// 4. Calculate channel width using MAX/MIN NinjaScript methods
			double prdHighest = MAX(High, 300)[0];
			double prdLowest = MIN(Low, 300)[0];
			double cwidth = (prdHighest - prdLowest) * ChannelW / 100.0;

			// 5. Find SR channels for each pivot
			List<(double hi, double lo, int numpp)> srCandidates = new List<(double, double, int)>();
			for (int i = 0; i < pivotVals.Count; i++)
			{
				double lo = pivotVals[i];
				double hi = lo;
				int numpp = 0;
				for (int y = 0; y < pivotVals.Count; y++)
				{
					double cpp = pivotVals[y];
					double wdth = Math.Abs(cpp <= hi ? hi - cpp : cpp - lo);
					if (wdth <= cwidth)
					{
						if (cpp <= hi) lo = Math.Min(lo, cpp);
						else hi = Math.Max(hi, cpp);
						numpp += 20;
					}
				}
				srCandidates.Add((hi, lo, numpp));
			}

			// 6. Add strength by counting bars in channel
			List<int> strengths = new List<int>(new int[pivotVals.Count]);
			for (int i = 0; i < pivotVals.Count; i++)
			{
				double h = srCandidates[i].hi;
				double l = srCandidates[i].lo;
				int s = 0;
				for (int y = 0; y < Loopback && y < CurrentBar; y++)
				{
					if ((High[y] <= h && High[y] >= l) || (Low[y] <= h && Low[y] >= l))
						s++;
				}
				strengths[i] = srCandidates[i].numpp + s;
			}

			// 7. Select strongest SRs
			srLevels.Clear();
			var used = new bool[pivotVals.Count];
			for (int src = 0; src < Math.Min(10, pivotVals.Count); src++)
			{
				int stl = -1;
				int stv = -1;
				for (int y = 0; y < pivotVals.Count; y++)
				{
					if (!used[y] && strengths[y] > stv && strengths[y] >= MinStrength * 20)
					{
						stv = strengths[y];
						stl = y;
					}
				}
				if (stl >= 0)
				{
					double hh = srCandidates[stl].hi;
					double ll = srCandidates[stl].lo;
					srLevels.Add(new SRLevel(hh, ll, strengths[stl]));
					// Mark included pivots as used
					for (int y = 0; y < pivotVals.Count; y++)
					{
						if ((srCandidates[y].hi <= hh && srCandidates[y].hi >= ll) || (srCandidates[y].lo <= hh && srCandidates[y].lo >= ll))
							used[y] = true;
					}
				}
				else break;
				if (srLevels.Count >= MaxNumSR)
					break;
			}

			// 8. Sort by strength descending
			srLevels = srLevels.OrderByDescending(x => x.Strength).ToList();

			// 9. Draw rectangles for each channel
			if (ShowSRLevels)
			{
				foreach (var tag in srRectTags)
				{
					RemoveDrawObject(tag);
				}
				srRectTags.Clear();
				for (int i = 0; i < srLevels.Count; i++)
				{
					var sr = srLevels[i];
					Brush color = (sr.Hi > Close[0] && sr.Lo > Close[0]) ? ResCol : (sr.Hi < Close[0] && sr.Lo < Close[0]) ? SupCol : InchCol;
					string tag = $"SRZone_{i}_{CurrentBar}";
					Draw.Rectangle(this, tag, false, Loopback, sr.Hi, 0, sr.Lo, color, color, 10);
					srRectTags.Add(tag);
				}
			}
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public List<SRLevel> SRLevels
		{
			get { return srLevels; }
		}		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SRZones[] cacheSRZones;
		public SRZones SRZones(int prd, int channelW, int minStrength, int maxNumSR, int loopback, Brush resCol, Brush supCol, Brush inchCol, bool showSRLevels)
		{
			return SRZones(Input, prd, channelW, minStrength, maxNumSR, loopback, resCol, supCol, inchCol, showSRLevels);
		}

		public SRZones SRZones(ISeries<double> input, int prd, int channelW, int minStrength, int maxNumSR, int loopback, Brush resCol, Brush supCol, Brush inchCol, bool showSRLevels)
		{
			if (cacheSRZones != null)
				for (int idx = 0; idx < cacheSRZones.Length; idx++)
					if (cacheSRZones[idx] != null && cacheSRZones[idx].Prd == prd && cacheSRZones[idx].ChannelW == channelW && cacheSRZones[idx].MinStrength == minStrength && cacheSRZones[idx].MaxNumSR == maxNumSR && cacheSRZones[idx].Loopback == loopback && cacheSRZones[idx].ResCol == resCol && cacheSRZones[idx].SupCol == supCol && cacheSRZones[idx].InchCol == inchCol && cacheSRZones[idx].ShowSRLevels == showSRLevels && cacheSRZones[idx].EqualsInput(input))
						return cacheSRZones[idx];
			return CacheIndicator<SRZones>(new SRZones(){ Prd = prd, ChannelW = channelW, MinStrength = minStrength, MaxNumSR = maxNumSR, Loopback = loopback, ResCol = resCol, SupCol = supCol, InchCol = inchCol, ShowSRLevels = showSRLevels }, input, ref cacheSRZones);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SRZones SRZones(int prd, int channelW, int minStrength, int maxNumSR, int loopback, Brush resCol, Brush supCol, Brush inchCol, bool showSRLevels)
		{
			return indicator.SRZones(Input, prd, channelW, minStrength, maxNumSR, loopback, resCol, supCol, inchCol, showSRLevels);
		}

		public Indicators.SRZones SRZones(ISeries<double> input , int prd, int channelW, int minStrength, int maxNumSR, int loopback, Brush resCol, Brush supCol, Brush inchCol, bool showSRLevels)
		{
			return indicator.SRZones(input, prd, channelW, minStrength, maxNumSR, loopback, resCol, supCol, inchCol, showSRLevels);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SRZones SRZones(int prd, int channelW, int minStrength, int maxNumSR, int loopback, Brush resCol, Brush supCol, Brush inchCol, bool showSRLevels)
		{
			return indicator.SRZones(Input, prd, channelW, minStrength, maxNumSR, loopback, resCol, supCol, inchCol, showSRLevels);
		}

		public Indicators.SRZones SRZones(ISeries<double> input , int prd, int channelW, int minStrength, int maxNumSR, int loopback, Brush resCol, Brush supCol, Brush inchCol, bool showSRLevels)
		{
			return indicator.SRZones(input, prd, channelW, minStrength, maxNumSR, loopback, resCol, supCol, inchCol, showSRLevels);
		}
	}
}

#endregion
