using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdRoom.Api;
using ColdRoom.Core;
using ColdRoom.Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class RiderScreen : ScreenView
    {
        public override string Route => "rider";
        public override string NavLabel => "Deliveries";
        public override string Badge => "RD";
        public override string Eyebrow => "Rider \u00B7 Lagos";
        public override string Title => "Deliveries";
        public override string Subtitle => "Your assigned runs and the barcode you present at the pickup station.";

        private readonly List<Order> _orders = new();
        private readonly List<PickupStation> _stations = new();

        private VisualElement _host;
        private bool _loading;
        private bool _loaded;
        private int _requestId;

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");
            _host = Ui.Col("delivery-host");
            _host.Add(LoadingState("Loading your deliveries\u2026"));
            main.Add(_host);
            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            var flow = Ui.Col("card");
            flow.Add(Ui.Text("RUN FLOW", "card-kicker"));
            flow.Add(Ui.Text("Station to customer", "card-title"));
            flow.Add(FlowLine("Assigned", "Operator assigns the run"));
            flow.Add(FlowLine("In transit", "You leave the station"));
            flow.Add(FlowLine("Arrived", "Customer confirms receipt"));
            flow.Add(Ui.Text("Transit actions arrive in step 8. The API already enforces the order.", "fine-print"));
            aside.Add(flow);

            var barcode = Ui.Col("card", "barcode-card");
            barcode.Add(Ui.Text("PICKUP BARCODE", "card-kicker"));
            barcode.Add(Ui.Text("Present at the station", "card-title"));
            barcode.Add(Ui.Text(
                "The customer's public order code is the pickup reference. Barcode rendering lands in step 9.",
                "card-copy"));
            aside.Add(barcode);

            layout.Add(aside);
            root.Add(layout);
        }

        public override void OnShow()
        {
            if (!_loaded && !_loading)
            {
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            int request = ++_requestId;
            _loading = true;
            _host.Clear();
            _host.Add(LoadingState("Loading your deliveries\u2026"));

            try
            {
                Task<IReadOnlyList<Order>> ordersTask = Shell.Api.GetOrders();
                Task<IReadOnlyList<PickupStation>> stationsTask = Shell.Api.GetStations();
                await Task.WhenAll(ordersTask, stationsTask);
                if (request != _requestId)
                {
                    return;
                }

                _orders.Clear();
                _orders.AddRange(ordersTask.Result);
                _stations.Clear();
                _stations.AddRange(stationsTask.Result);
                _loaded = true;
                Render();
            }
            catch (ApiException exception)
            {
                if (request == _requestId)
                {
                    ShowError(exception.Message);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Rider load failed: {exception.Message}");
                if (request == _requestId)
                {
                    ShowError("Your deliveries could not be loaded.");
                }
            }
            finally
            {
                if (request == _requestId)
                {
                    _loading = false;
                }
            }
        }

        private void ShowError(string message)
        {
            _host.Clear();
            _host.Add(ErrorState("Deliveries unavailable", message, () => _ = LoadAsync()));
        }

        private void Render()
        {
            _host.Clear();

            List<Order> active = _orders
                .Where(o => o.Status != OrderStatus.Received)
                .ToList();

            if (active.Count == 0)
            {
                _host.Add(EmptyState(
                    "RD",
                    "No deliveries assigned",
                    "When an operator assigns a packed order to you, the run appears here with its pickup code.",
                    ("Refresh deliveries", () => _ = LoadAsync())));
                return;
            }

            foreach (Order order in active.OrderBy(o => o.CreatedAt, StringComparer.Ordinal))
            {
                _host.Add(DeliveryCard(order));
            }
        }

        private VisualElement DeliveryCard(Order order)
        {
            var card = Ui.Col("card", "delivery-card");

            var top = Ui.Row("queue-top");
            top.Add(Ui.Text("ASSIGNED RUN", "card-kicker"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Pill(order.Urgency.ToString(), UrgencyClass(order.Urgency)));
            top.Add(Ui.Pill(order.Status.ToString(), "pill-soft"));
            card.Add(top);

            card.Add(Ui.Text(order.PublicCode, "delivery-code"));
            card.Add(Ui.Text(
                $"{StationName(order.StationId)} \u2192 {order.CustomerName ?? "Customer"}",
                "delivery-route-title"));

            var body = Ui.Row("queue-body", "wrap");
            body.Add(Ui.Field("Pickup", StationName(order.StationId), "queue-field"));
            body.Add(Ui.Field("Items", ItemCount(order) + " items", "queue-field"));
            body.Add(Ui.Field("Total", Money.Format(order.TotalKobo), "queue-field"));
            body.Add(Ui.Field("Placed", TimeText.Clock(order.CreatedAt), "queue-field"));
            card.Add(body);

            card.Add(Ui.Text("Transit and arrival actions arrive in step 8.", "fine-print"));
            return card;
        }

        private string StationName(string stationId)
        {
            return _stations.FirstOrDefault(s => s.Id == stationId)?.Name ?? "Station";
        }

        private static int ItemCount(Order order)
        {
            return order.Lines == null ? 0 : order.Lines.Sum(line => line.Qty);
        }

        private static string UrgencyClass(OrderUrgency urgency)
        {
            return urgency switch
            {
                OrderUrgency.Urgent => "pill-urgent",
                OrderUrgency.Priority => "pill-priority",
                _ => "pill-standard"
            };
        }

        private static VisualElement FlowLine(string action, string copy)
        {
            var row = Ui.Row("state-line");
            row.Add(Ui.Dot("state-dot"));
            var col = Ui.Col("state-copy");
            col.Add(Ui.Text(action, "state-name"));
            col.Add(Ui.Text(copy, "state-desc"));
            row.Add(col);
            return row;
        }
    }
}
