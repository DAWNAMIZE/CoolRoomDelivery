using UnityEngine.UIElements;

namespace ColdRoom.App
{
    public abstract class ScreenView
    {
        private VisualElement _root;

        public abstract string Route { get; }
        public abstract string NavLabel { get; }
        public abstract string Badge { get; }
        public abstract string Eyebrow { get; }
        public abstract string Title { get; }
        public abstract string Subtitle { get; }

        protected AppShell Shell { get; private set; }

        public VisualElement GetView(AppShell shell)
        {
            if (_root != null)
            {
                return _root;
            }

            Shell = shell;
            _root = Ui.El("screen");
            Build(_root);
            return _root;
        }

        protected abstract void Build(VisualElement root);

        public virtual void OnShow()
        {
        }

        protected VisualElement PageHead()
        {
            var head = Ui.Col("page-head");
            head.Add(Ui.Text(Eyebrow.ToUpperInvariant(), "eyebrow"));
            head.Add(Ui.Text(Title, "page-title"));
            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                head.Add(Ui.Text(Subtitle, "page-sub"));
            }

            return head;
        }

        protected VisualElement EmptyState(string badge, string title, string copy, params (string Label, System.Action Action)[] actions)
        {
            var card = Ui.Col("card", "empty-state");
            card.Add(Ui.Monogram(badge, "monogram-lg"));
            card.Add(Ui.Text(title, "empty-title"));
            card.Add(Ui.Text(copy, "empty-copy"));
            if (actions.Length > 0)
            {
                var row = Ui.Row("button-row", "empty-actions");
                foreach (var action in actions)
                {
                    row.Add(Ui.Primary(action.Label, action.Action));
                }

                card.Add(row);
            }

            return card;
        }
    }
}
