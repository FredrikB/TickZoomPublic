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
using System.Diagnostics;
using System.Drawing;

using TickZoom.Api;


namespace TickZoom.Common
{
	/// <summary>
	/// Description of StrategySupport.
	/// </summary>
	public class ExitCommon : StrategySupport
	{
		public class InternalOrders {
			public LogicalOrder buyMarket;
			public LogicalOrder sellMarket;
			public LogicalOrder buyStop;
			public LogicalOrder sellStop;
			public LogicalOrder buyLimit;
			public LogicalOrder sellLimit;
		}
		private PositionInterface position;
		private InternalOrders orders = new InternalOrders();
		
		private bool enableWrongSideOrders = false;
		private bool isNextBar = false;
		
		public ExitCommon(Strategy strategy) : base(strategy) {
		}
		
		public override void OnInitialize()
		{
			if( IsTrace) Log.Trace(FullName+".Initialize()");
			Drawing.Color = Color.Black;
			orders.buyMarket = Data.CreateOrder(this);
			orders.buyMarket.Type = OrderType.BuyMarket;
			orders.sellMarket = Data.CreateOrder(this);
			orders.sellMarket.Type = OrderType.SellMarket;
			orders.buyStop = Data.CreateOrder(this);
			orders.buyStop.Type = OrderType.BuyStop;
			orders.buyStop.TradeDirection = TradeDirection.Exit;
			orders.sellStop = Data.CreateOrder(this);
			orders.sellStop.Type = OrderType.SellStop;
			orders.sellStop.TradeDirection = TradeDirection.Exit;
			orders.buyLimit = Data.CreateOrder(this);
			orders.buyLimit.Type = OrderType.BuyLimit;
			orders.buyLimit.TradeDirection = TradeDirection.Exit;
			orders.sellLimit = Data.CreateOrder(this);
			orders.sellLimit.Type = OrderType.SellLimit;
			orders.sellLimit.TradeDirection = TradeDirection.Exit;
			Strategy.OrderManager.Add( orders.buyMarket);
			Strategy.OrderManager.Add( orders.sellMarket);
			Strategy.OrderManager.Add( orders.buyStop);
			Strategy.OrderManager.Add( orders.sellStop);
			Strategy.OrderManager.Add( orders.buyLimit);
			Strategy.OrderManager.Add( orders.sellLimit);
			position = Strategy.Position;
		}

		public void OnProcessOrders(Tick tick) {
			if( IsTrace) Log.Trace("OnProcessOrders() Model.Signal="+Strategy.Position.Signal);
			if( position.IsLong) {
				orders.buyStop.IsActive = false;
				orders.buyLimit.IsActive = false;
			}
			if( position.IsShort) {
				orders.sellLimit.IsActive = false;
				orders.sellStop.IsActive = false;
			}
			if( position.HasPosition ) {
				// copy signal in case of increased position size
				if( orders.buyStop.IsActive ) ProcessBuyStop(tick);
				if( orders.sellStop.IsActive ) ProcessSellStop(tick);
				if( orders.buyLimit.IsActive ) ProcessBuyLimit(tick);
				if( orders.sellLimit.IsActive ) ProcessSellLimit(tick);
			}
		}
		
		private void FlattenSignal() {
			Strategy.Position.Signal = 0;
			CancelOrders();
		}
	
		public void CancelOrders() {
			orders.buyStop.IsActive = false;
			orders.sellStop.IsActive = false;
			orders.buyLimit.IsActive = false;
			orders.sellLimit.IsActive = false;
		}
		
		private void ProcessBuyStop(Tick tick) {
			if( orders.buyStop.IsActive &&
			    Strategy.Position.IsShort &&
			    tick.Ask >= orders.buyStop.Price) {
				LogExit("Buy Stop Exit at " + tick);
				FlattenSignal();
				if( Strategy.Performance.GraphTrades) {
	                Strategy.Chart.DrawTrade(orders.buyStop,tick.Ask,Strategy.Position.Signal);
				}
				CancelOrders();
			} 
		}
		
		private void ProcessBuyLimit(Tick tick) {
			if( orders.buyLimit.IsActive && 
			    Strategy.Position.IsShort)
            {
                if (tick.Ask <= orders.buyLimit.Price || 
				    (tick.IsTrade && tick.Price < orders.buyLimit.Price))
                {
                    LogExit("Buy Limit Exit at " + tick);
                    FlattenSignal();
                    if (Strategy.Performance.GraphTrades)
                    {
                        Strategy.Chart.DrawTrade(orders.buyLimit, tick.Ask, Strategy.Position.Signal);
                    }
                    CancelOrders();
                }
			} 
		}
		
		private void ProcessSellStop(Tick tick) {
			if( orders.sellStop.IsActive &&
			    Strategy.Position.IsLong &&
			    tick.Bid <= orders.sellStop.Price) {
				LogExit("Sell Stop Exit at " + tick);
				FlattenSignal();
				if( Strategy.Performance.GraphTrades) {
	                Strategy.Chart.DrawTrade(orders.sellStop,tick.Ask,Strategy.Position.Signal);
				}
				CancelOrders();
			}
		}
		
		private void ProcessSellLimit(Tick tick) {
			if( orders.sellLimit.IsActive &&
			    Strategy.Position.IsLong)
            {
                if (tick.Bid >= orders.sellLimit.Price || 
				    (tick.IsTrade && tick.Price > orders.sellLimit.Price))
                {
                    LogExit("Sell Stop Limit at " + tick);
                    FlattenSignal();
                    if (Strategy.Performance.GraphTrades)
                    {
                        Strategy.Chart.DrawTrade(orders.sellLimit, tick.Bid, Strategy.Position.Signal);
                    }
                    CancelOrders();
                }
			}
		}
		
		private void LogExit(string description) {
			if( Chart.IsDynamicUpdate) {
				Log.Notice("Bar="+Chart.DisplayBars.CurrentBar+", " + description);
			} else {
        		if( IsDebug) Log.Debug("Bar="+Chart.DisplayBars.CurrentBar+", " + description);
			}
		}

        #region Orders

        public void GoFlat() {
        	if( Strategy.Position.IsFlat) {
        		throw new TickZoomException("Strategy must have a position before attempting to go flat.");
        	}
        	if( Strategy.Position.IsLong) {
	        	orders.sellMarket.Price = 0;
	        	orders.sellMarket.Positions = Strategy.Position.Size;
	        	if( isNextBar) {
	    	    	orders.sellMarket.IsNextBar = true;
		       	} else {
		        	orders.sellMarket.IsActive = true;
	        	}
				if( Strategy.Performance.GraphTrades) {
	        		Strategy.Chart.DrawTrade(orders.sellMarket,Ticks[0].Bid,Strategy.Position.Signal);
				}
        	}
        	if( Strategy.Position.IsShort) {
	        	orders.buyMarket.Price = 0;
	        	orders.buyMarket.Positions = Strategy.Position.Size;
	        	if( isNextBar) {
	    	    	orders.buyMarket.IsNextBar = true;
		       	} else {
		        	orders.buyMarket.IsActive = true;
	        	}
				if( Strategy.Performance.GraphTrades) {
	        		Strategy.Chart.DrawTrade(orders.buyMarket,Ticks[0].Ask,Strategy.Position.Signal);
				}
        	}
        	LogExit("GoFlat");
        	FlattenSignal();
		}
	
        public void BuyStop(double price) {
        	if( Strategy.Position.IsLong) {
        		throw new TickZoomException("Strategy must be short or flat before setting a buy stop to exit.");
        	} else if( Strategy.Position.IsFlat) {
        		if(!Strategy.Orders.Enter.ActiveNow.HasSellOrder) {
        			throw new TickZoomException("When flat, a sell order must be active before creating a buy order to exit.");
        		}
			}
    		orders.buyStop.Price = price;
        	if( isNextBar) {
    	    	orders.buyStop.IsNextBar = true;
	       	} else {
	        	orders.buyStop.IsActive = true;
        	}
        }
	
        public void SellStop( double price) {
        	if( Strategy.Position.IsShort) {
        		throw new TickZoomException("Strategy must be long or flat before setting a sell stop to exit.");
        	} else if( Strategy.Position.IsFlat) {
        		if(!Strategy.Orders.Enter.ActiveNow.HasBuyOrder) {
        			throw new TickZoomException("When flat, a buy order must be active before creating a sell order to exit.");
        		}
        	}
			orders.sellStop.Price = price;
        	if( isNextBar) {
    	    	orders.sellStop.IsNextBar = true;
	       	} else {
	        	orders.sellStop.IsActive = true;
        	}
		}
        
        public void BuyLimit(double price) {
        	if( Strategy.Position.IsLong) {
        		throw new TickZoomException("Strategy must be short or flat before setting a buy limit to exit.");
        	} else if( Strategy.Position.IsFlat) {
        		if(!Strategy.Orders.Enter.ActiveNow.HasSellOrder) {
        			throw new TickZoomException("When flat, a sell order must be active before creating a buy order to exit.");
        		}
			}
    		orders.buyLimit.Price = price;
        	if( isNextBar) {
    	    	orders.buyLimit.IsNextBar = true;
	       	} else {
	        	orders.buyLimit.IsActive = true;
        	}
		}
	
        public void SellLimit( double price) {
        	if( Strategy.Position.IsShort) {
        		throw new TickZoomException("Strategy must be long or flat before setting a sell limit to exit.");
        	} else if( Strategy.Position.IsFlat) {
        		if(!Strategy.Orders.Enter.ActiveNow.HasBuyOrder) {
        			throw new TickZoomException("When flat, a buy order must be active before creating a sell order to exit.");
        		}
			}
			orders.sellLimit.Price = price;
        	if( isNextBar) {
    	    	orders.sellLimit.IsNextBar = true;
	       	} else {
	        	orders.sellLimit.IsActive = true;
        	}
		}
        
		#endregion

		
		public override string ToString()
		{
			return FullName;
		}
		
		// This just makes sure nothing uses PositionChange
		// here because only Strategy.PositionChange must be used.
		public new int Position {
			get { return 0; }
			set { /* ignore */ }
		}
		
		public bool EnableWrongSideOrders {
			get { return enableWrongSideOrders; }
			set { enableWrongSideOrders = value; }
		}
		
		internal bool IsNextBar {
			get { return isNextBar; }
			set { isNextBar = value; }
		}
		
		internal InternalOrders Orders {
			get { return orders; }
			set { orders = value; }
		}
	}
}
