using System;
using UnityEngine.UIElements;

namespace ColdRoom.App
{
    /// <summary>Small, dependency-free helpers for building the UI Toolkit tree in C#.</summary>
    public static class Ui
    {
        public static VisualElement El(params string[] classes)
        {
            var element = new VisualElement();
            AddClasses(element, classes);
            return element;
        }

        public static VisualElement Row(params string[] classes)
        {
            var element = El(classes);
            element.AddToClassList("row");
            return element;
        }

        public static VisualElement Col(params string[] classes)
        {
            var element = El(classes);
            element.AddToClassList("col");
            return element;
        }

        public static Label Text(string text, params string[] classes)
        {
            var label = new Label(text);
            AddClasses(label, classes);
            return label;
        }

        public static Label Muted(string text, params string[] classes)
        {
            var label = Text(text, classes);
            label.AddToClassList("muted");
            return label;
        }

        public static Button Button(string text, Action onClick, params string[] classes)
        {
            var button = new Button(onClick) { text = text };
            AddClasses(button, classes);
            return button;
        }

        public static Button Primary(string text, Action onClick)
        {
            return Button(text, onClick, "btn", "btn-primary");
        }

        public static Button Secondary(string text, Action onClick)
        {
            return Button(text, onClick, "btn", "btn-secondary");
        }

        public static Button Ghost(string text, Action onClick)
        {
            return Button(text, onClick, "btn", "btn-ghost");
        }

        public static VisualElement Pill(string text, params string[] classes)
        {
            var pill = El("pill");
            AddClasses(pill, classes);
            pill.Add(Text(text, "pill-text"));
            return pill;
        }

        public static VisualElement Dot(string className = "live-dot")
        {
            return El(className);
        }

        public static VisualElement Divider(string className = "divider")
        {
            return El(className);
        }

        public static VisualElement Spacer()
        {
            return El("spacer");
        }

        public static VisualElement Monogram(string text, params string[] classes)
        {
            var box = El("monogram");
            AddClasses(box, classes);
            box.Add(Text(text, "monogram-text"));
            return box;
        }

        public static VisualElement Avatar(string initials, params string[] classes)
        {
            var avatar = El("avatar");
            AddClasses(avatar, classes);
            avatar.Add(Text(initials, "avatar-text"));
            return avatar;
        }

        public static VisualElement SectionHead(string title, string kicker = null, VisualElement trailing = null)
        {
            var head = Row("section-head");
            var copy = Col("section-head-copy");
            if (!string.IsNullOrEmpty(kicker))
            {
                copy.Add(Text(kicker.ToUpperInvariant(), "section-kicker"));
            }

            copy.Add(Text(title, "section-title"));
            head.Add(copy);
            head.Add(Spacer());
            if (trailing != null)
            {
                head.Add(trailing);
            }

            return head;
        }

        public static void AddClasses(VisualElement element, params string[] classes)
        {
            if (classes == null)
            {
                return;
            }

            foreach (string className in classes)
            {
                if (string.IsNullOrWhiteSpace(className))
                {
                    continue;
                }

                foreach (string part in className.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    element.AddToClassList(part);
                }
            }
        }

        public static VisualElement Clickable(VisualElement element, Action action)
        {
            element.focusable = true;
            element.pickingMode = PickingMode.Position;
            element.AddToClassList("clickable");
            element.RegisterCallback<ClickEvent>(_ => action?.Invoke());
            element.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.Space)
                {
                    action?.Invoke();
                    evt.StopPropagation();
                }
            });
            return element;
        }

        public static VisualElement Field(string label, string value, string className = null)
        {
            var field = Col("field");
            if (!string.IsNullOrEmpty(className))
            {
                field.AddToClassList(className);
            }

            field.Add(Text(label.ToUpperInvariant(), "field-label"));
            field.Add(Text(value, "field-value"));
            return field;
        }

        public static VisualElement TagRow(params string[] tags)
        {
            var row = Row("tag-row");
            foreach (string tag in tags)
            {
                row.Add(Pill(tag, "pill-soft"));
            }

            return row;
        }
    }
}
