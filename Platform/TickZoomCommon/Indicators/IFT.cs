#region Copyright
/*
 * Software: TickZoom Trading Platform
 * Copyright 2009 M. Wayne Walter
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <http://www.tickzoom.org/wiki/Licenses>
 * or write to Free Software Foundation, Inc., 51 Franklin Street,
 * Fifth Floor, Boston, MA  02110-1301, USA.
 * 
 */
#endregion

using System;
using System.Drawing;
using TickZoom.Api;


namespace TickZoom.Common
{
	public class IFT : IndicatorCommon
	{
		int period = 5;
		IndicatorCommon value1;
		IndicatorCommon value2;
		RSI rsi;
		WMA wma;
		
		public IFT(object anyPrice, int period) {
			AnyInput = anyPrice;
			StartValue = 0;
			this.period = period;
		}
		
		public override void OnInitialize()
		{
			Name = "IFT";
			Drawing.PaneType = PaneType.Secondary;
			Drawing.IsVisible = true;
			
			value1 = Formula.Indicator();
			value2 = Formula.Indicator();
			rsi = Formula.RSI(Input, period);
			wma = Formula.WMA(value1, period);
			Formula.Line(0.5, Color.LightGreen);
			Formula.Line(-0.5, Color.Red);
		}
		
		public override void Update()
		{
			Drawing.Color = this[1] < this[0] ? Color.Green : this[1] > this[0] ? Color.Red : Color.Black;
			value1[0] = 0.1 * (rsi[0] - 50);
			value2[0] = 2 * wma[0];
			this[0] = (Math.Exp(value2[0]) - 1)/(Math.Exp(value2[0]) + 1);
		}
	}
}
