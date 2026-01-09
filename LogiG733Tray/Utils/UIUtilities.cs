namespace LogiG733Tray.Utils
{
    public abstract class UiUtilities
    {
        internal static readonly Color Bg = Color.FromArgb(30, 30, 30);
        internal static readonly Color Card = Color.FromArgb(40, 40, 40);
        internal static readonly Color Accent = Color.FromArgb(0, 183, 194);
        internal static readonly Color Text = Color.White;
        internal static readonly Color Muted = Color.Gainsboro;

        internal static Button StyledButton(string text)
        {
            return new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Card,
                ForeColor = Text,
                Dock = DockStyle.Fill,
                Height = 32
            };
        }
        
        internal static Panel StyledTextBox(out TextBox textBox, string placeholder = "")
        {
            textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Bg,
                ForeColor = Text,
                Font = new Font("Segoe UI", 9F),
                Dock = DockStyle.Fill,
                PlaceholderText = placeholder
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 4, 6, 4),
                BackColor = Bg,
                BorderStyle = BorderStyle.FixedSingle,
                MinimumSize = new Size(0, 32),
                MaximumSize = new Size(0, 32)
            };

            panel.Controls.Add(textBox);
            return panel;
        }
    }
}