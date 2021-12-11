﻿using System;
using System.Reactive.Linq;
using Binance.Client.Websocket.Client;
using Binance.Client.Websocket.Responses.Trades;
using Crypto.Websocket.Extensions.Core.Models;
using Crypto.Websocket.Extensions.Core.Trades.Models;
using Crypto.Websocket.Extensions.Core.Trades.Sources;
using Crypto.Websocket.Extensions.Core.Validations;
using Crypto.Websocket.Extensions.Logging;

namespace Crypto.Websocket.Extensions.Trades.Sources
{
    /// <summary>
    /// Binance trades source
    /// </summary>
    public class BinanceTradeSource : TradeSourceBase
    {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        IBinanceWebsocketClient _client;
        IDisposable _subscription;

        /// <inheritdoc />
        public BinanceTradeSource(IBinanceWebsocketClient client)
        {
            ChangeClient(client);
        }

        /// <inheritdoc />
        public override string ExchangeName => "binance";

        /// <summary>
        /// Change client and resubscribe to the new streams
        /// </summary>
        public void ChangeClient(IBinanceWebsocketClient client)
        {
            CryptoValidations.ValidateInput(client, nameof(client));

            _client = client;
            _subscription?.Dispose();
            Subscribe();
        }

        void Subscribe()
        {
            _subscription = _client.Streams.TradesStream
                .Where(x => x?.Data != null)
                .Subscribe(HandleTradeSafe);
        }

        void HandleTradeSafe(TradeResponse response)
        {
            try
            {
                HandleTrade(response);
            }
            catch (Exception e)
            {
                Log.Error(e, $"[Binance] Failed to handle trade info, error: '{e.Message}'");
            }
        }

        void HandleTrade(TradeResponse response)
        {
            TradesSubject.OnNext(new[] { ConvertTrade(response.Data) });
        }

        CryptoTrade ConvertTrade(Trade trade)
        {
            var data = new CryptoTrade()
            {
                Amount = trade.Quantity,
                AmountQuote = trade.Quantity * trade.Price,
                Side = ConvertSide(trade.Side),
                Id = trade.TradeId.ToString(),
                Price = trade.Price,
                Timestamp = trade.TradeTime,
                Pair = trade.Symbol,
                MakerOrderId = trade.IsBuyerMaker ? trade.BuyerOrderId.ToString() : trade.SellerOrderId.ToString(),
                TakerOrderId = trade.IsBuyerMaker ? trade.SellerOrderId.ToString() : trade.BuyerOrderId.ToString(),

                ExchangeName = ExchangeName,
                ServerTimestamp = trade.EventTime
            };
            return data;
        }

        static CryptoTradeSide ConvertSide(TradeSide tradeSide)
        {
            return tradeSide == TradeSide.Buy ? CryptoTradeSide.Buy : CryptoTradeSide.Sell;
        }
    }
}
