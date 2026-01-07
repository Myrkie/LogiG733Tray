using LogiG733Tray.G733;

namespace LogiG733Tray
{
    public class LightControlForm : Form
    {
        private readonly G733Device _g733;
        private ColorDialog? _colorDialog;
        private Button? _btnUpper;
        private Button? _btnLower;
        private Button? _btnBoth;
        private Button? _btnOff;
        private Button? _btnBestColor;

        public LightControlForm(G733Device g733)
        {
            _g733 = g733;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "G733 Light Control";
            Size = new Size(300, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            _colorDialog = new ColorDialog { FullOpen = true, AnyColor = true };

            _btnUpper = new Button { Text = "Set Upper Bar", Location = new Point(20, 20), Width = 120 };
            _btnLower = new Button { Text = "Set Lower Bar", Location = new Point(150, 20), Width = 120 };
            _btnBoth  = new Button { Text = "Set Both Bars", Location = new Point(20, 60), Width = 250 };
            _btnOff   = new Button { Text = "Turn Off Lights", Location = new Point(20, 100), Width = 250 };
            _btnBestColor   = new Button { Text = "Best Color", Location = new Point(20, 125), Width = 250 };

            _btnUpper.Click += (_, _) => SetLight(G733HidClient.LightTarget.Upper);
            _btnLower.Click += (_, _) => SetLight(G733HidClient.LightTarget.Lower);
            _btnBoth.Click += (_, _) => SetBothLights();
            _btnOff.Click += (_, _) => _g733.DisableLights();
            _btnBestColor.Click += (_, _) => _g733.SetLights(G733HidClient.Colors.Purple,G733HidClient.Colors.Purple);

            Controls.AddRange(_btnUpper, _btnLower, _btnBoth, _btnOff, _btnBestColor);
        }

        private void SetLight(G733HidClient.LightTarget target)
        {
            if (_colorDialog != null && _colorDialog.ShowDialog() != DialogResult.OK) return;

            if (_colorDialog == null) return;
            var color = _colorDialog.Color;
            var rgb = new G733HidClient.RgbColor(color.R, color.G, color.B);

            switch (target)
            {
                case G733HidClient.LightTarget.Upper:
                    _g733.SetUpperLightBar(rgb);
                    break;
                case G733HidClient.LightTarget.Lower:
                    _g733.SetLowerLightBar(rgb);
                    break;
            }
        }

        private void SetBothLights()
        {
            if (_colorDialog != null && _colorDialog.ShowDialog() != DialogResult.OK) return;

            if (_colorDialog == null) return;
            var color = _colorDialog.Color;
            var rgb = new G733HidClient.RgbColor(color.R, color.G, color.B);

            _g733.SetLights(rgb, rgb);
        }
    }
}
