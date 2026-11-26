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
    public sealed class OperatorScreen : ScreenView
    {
        public override string Route => "operator";
        public override string NavLabel => "Queue";
        public override string Badge => "QU";
        public override string Eyebrow => "Operator \u00B7 Lagos";
        public override string Title => "Order queue";
        public override string Subtitle => "Urgent first, then Priority, then Standard. Oldest first within each level.";

        private readonly List<Order> _orders = new();
        private readonly List<PickupStation> _stations = new();

        private VisualElement _metricsHost;
        private VisualElement _queueHost;
        private Label _queueCount;
        private bool _loading;
        private bool _loaded;
        private int _requestId;

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            _metricsHost = Ui.Row("grid", "grid-3", "metric-grid");
            _metricsHost.Add(Metric("Urgent", "\u2014", "loading", "metric-urgent"));
            _metricsHost.Add(Metric("Priority", "\u2014", "loading", "metric-priority"));
            _metricsHost.Add(Metric("Standard", "\u2014", "loading", "metric-standard"));
            root.Add(_metricsHost);

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");

            var queueHead = Ui.Row("queue-head");
            queueHead.Add(Ui.Text("READY TO PROCESS", "card-kicker"));
            queueHead.Add(Ui.Spacer());
            var queuePill = Ui.Pill("Live queue", "pill-soft");
            _queueCount = queuePill.Q<Label>();
            queueHead.Add(queuePill);
            main.Add(queueHead);

            _queueHost = Ui.Col("queue-host");
            _queueHost.Add(LoadingState("Loading the order queue\u2026"));
            main.Add(_queueHost);

            var footer = Ui.Row("queue-footer");
            footer.Add(Ui.Text("Transitions are validated by the mock API.", "fine-print"));
            footer.Add(Ui.Spacer());
            footer.Add(Ui.Ghost("Refresh queue", () => _ = LoadAsync()));
            main.Add(footer);

            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            var rules = Ui.Col("card");
            rules.Add(Ui.Text("QUEUE ORDER", "card-kicker"));
            rules.Add(Ui.Text("How work is picked", "card-title"));
            rules.Add(RuleRow("01", "Urgent"));
            rules.Add(RuleRow("02", "Priority"));
            rules.Add(RuleRow("03", "Standard"));
            rules.Add(Ui.Divider());
            rules.Add(Ui.Text(
                "Within each level, orders are shown oldest first. The queue is enforced by the data layer, not just sort in the UI.",
                "card-copy"));
            aside.Add(rules);

            var flow = Ui.Col("card");
            flow.Add(Ui.Text("OPERATOR ACTIONS", "card-kicker"));
            flow.Add(Ui.Text("Move orders forward", "card-title"));
            flow.Add(FlowLine("Confirm", "Placed \u2192 Confirmed"));
            flow.Add(FlowLine("Pack", "Confirmed \u2192 Packed"));
            flow.Add(FlowLine("Assign", "Packed \u2192 AssignedToRider"));
            flow.Add(Ui.Text("Order actions arrive in step 7. The API already rejects illegal moves.", "fine-print"));
            aside.Add(flow);

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
            _queueHost.Clear();
            _queueHost.Add(LoadingState("Loading the order queue\u2026"));

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
                Debug.LogWarning($"[ColdRoom] Queue load failed: {exception.Message}");
                if (request == _requestId)
                {
                    ShowError("The order queue could not be loaded.");
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
            _queueHost.Clear();
            _queueHost.Add(ErrorState("Queue unavailable", message, () => _ = LoadAsync()));
        }

        private void Render()
        {
            List<Order> active = _orders
                .Where(o => o.Status != OrderStatus.Received)
                .ToList();

            _metricsHost.Clear();
            _metricsHost.Add(Metric("Urgent", Count(active, OrderUrgency.Urgent), Oldest(active, OrderUrgency.Urgent), "metric-urgent"));
            _metricsHost.Add(Metric("Priority", Count(active, OrderUrgency.Priority), Oldest(active, OrderUrgency.Priority), "metric-priority"));
            _metricsHost.Add(Metric("Standard", Count(active, OrderUrgency.Standard), Oldest(active, OrderUrgency.Standard), "metric-standard"));

            _queueCount.text = $"{active.Count} active";

            _queueHost.Clear();
            if (active.Count == 0)
            {
                _queueHost.Add(EmptyState(
                    "QU",
                    "Queue is clear",
                    "New customer orders appear here as soon as they are placed. Refresh to check again.",
                    ("Refresh queue", () => _ = LoadAsync())));
                return;
            }

            List<Order> queue = active
                .OrderBy(o => UrgencyRank(o.Urgency))
                .ThenBy(o => o.CreatedAt, StringComparer.Ordinal)
                .ToList();

            foreach (Order order in queue)
            {
                _queueHost.Add(QueueRow(order));
            }
        }

        private static string Count(List<Order> orders, OrderUrgency urgency)
        {
            return orders.Count(o => o.Urgency == urgency).ToString();
        }

        private static string Oldest(List<Order> orders, OrderUrgency urgency)
        {
            Order oldest = orders
                .Where(o => o.Urgency == urgency)
                .OrderBy(o => o.CreatedAt, StringComparer.Ordinal)
                .FirstOrDefault();

            return oldest == null ? "none waiting" : "oldest " + TimeText.Age(oldest.CreatedAt);
        }

        private VisualElement QueueRow(Order order)
        {
            var card = Ui.Col("card", "queue-row");

            var top = Ui.Row("queue-top");
            top.Add(Ui.Text(order.PublicCode, "queue-code"));
            top.Add(Ui.Pill(order.Urgency.ToString(), UrgencyClass(order.Urgency)));
            top.Add(Ui.Pill(order.Status.ToString(), StatusClass(order.Status)));
            top.Add(Ui.Spacer());
            top.Add(Ui.Text(TimeText.Age(order.CreatedAt) + " old", "queue-age"));
            card.Add(top);

            var body = Ui.Row("queue-body", "wrap");
            body.Add(Ui.Field("Customer", order.CustomerName ?? "Customer", "queue-field"));
            body.Add(Ui.Field("Pickup", StationName(order.StationId), "queue-field"));
            body.Add(Ui.Field("Items", ItemCount(order) + " items", "queue-field"));
            body.Add(Ui.Field("Total", Money.Format(order.TotalKobo), "queue-field"));
            card.Add(body);

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

        private static int UrgencyRank(OrderUrgency urgency)
        {
            return urgency switch
            {
                OrderUrgency.Urgent => 0,
                OrderUrgency.Priority => 1,
                _ => 2
            };
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

        private static string StatusClass(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Placed => "pill-accent",
                OrderStatus.AssignedToRider => "pill-priority",
                OrderStatus.InTransit => "pill-priority",
                OrderStatus.ArrivedAtPickup => "pill-standard",
                _ => "pill-soft"
            };
        }

        private static VisualElement Metric(string label, string value, string meta, string className)
        {
            var card = Ui.Col("card", "metric-card", className);
            card.Add(Ui.Text(label.ToUpperInvariant(), "metric-label"));
            card.Add(Ui.Text(value, "metric-value"));
            card.Add(Ui.Text(meta, "metric-meta"));
            return card;
        }

        private static VisualElement FlowLine(string action, string transition)
        {
            var row = Ui.Row("flow-line");
            row.Add(Ui.Text(action, "flow-line-action"));
            row.Add(Ui.Spacer());
            row.Add(Ui.Text(transition, "flow-line-status"));
            return row;
        }

        private static VisualElement RuleRow(string number, string name)
        {
            var row = Ui.Row("rule-row");
            row.Add(Ui.Text(number, "rule-number"));
            row.Add(Ui.Text(name, "rule-name"));
            return row;
        }
    }
}
