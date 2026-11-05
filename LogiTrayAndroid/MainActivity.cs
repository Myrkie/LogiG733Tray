using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using LogiTrayAndroid.Services;
using LogiTrayAndroid.Services.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace LogiTrayAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize
    )]
    public class MainActivity : Activity
    {
        private bool _mediaSessionEnabled;
        private bool _connectionConfigured;
        private bool _isDestroyed;

        private Button _btnNextMedia = null!,
                       _btnPrevMedia = null!,
                       _btnUpperLight = null!,
                       _btnLowerLight = null!,
                       _btnBothLights = null!,
                       _btnLightsOff = null!,
                       _btnBestColor = null!,
                       _btnSetAutoPowerOff = null!,
                       _btnGetAutoPowerOff = null!,
                       _btnSetConnection = null!;

        private TextView _tvBatteryPercent = null!,
                         _tvBatteryVoltage = null!,
                         _tvDevice = null!,
                         _tvConnectionState = null!,
                         _tvMediaSession = null!;

        private EditText _etAutoPowerOff = null!,
                         _etIpAddress = null!,
                         _etApiKey = null!;

        private LinearLayout _mediaControls = null!;
        private LinearLayout _overlayAsleep = null!;
        private ProgressBar _pbBattery = null!;

        private readonly HttpService _httpService = new();

        private TaskCompletionSource<(int[] Color, LightMode Mode)>? _colorPickerTcs;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(ResourceConstant.Layout.activity_main);

            _overlayAsleep = FindViewById<LinearLayout>(ResourceConstant.Id.overlayAsleep)!;
            _tvDevice = FindViewById<TextView>(ResourceConstant.Id.tvDevice)!;
            _tvConnectionState = FindViewById<TextView>(ResourceConstant.Id.tvConnectionState)!;
            _mediaControls = FindViewById<LinearLayout>(ResourceConstant.Id.mediaControls)!;
            _btnPrevMedia = FindViewById<Button>(ResourceConstant.Id.btnPrevMedia)!;
            _btnNextMedia = FindViewById<Button>(ResourceConstant.Id.btnNextMedia)!;
            _tvMediaSession = FindViewById<TextView>(ResourceConstant.Id.tvMediaSession)!;

            _btnUpperLight = FindViewById<Button>(ResourceConstant.Id.btnUpperLight)!;
            _btnLowerLight = FindViewById<Button>(ResourceConstant.Id.btnLowerLight)!;
            _btnBothLights = FindViewById<Button>(ResourceConstant.Id.btnBothLights)!;
            _btnLightsOff = FindViewById<Button>(ResourceConstant.Id.btnLightsOff)!;
            _btnBestColor = FindViewById<Button>(ResourceConstant.Id.btnBestColor)!;
            _etAutoPowerOff = FindViewById<EditText>(ResourceConstant.Id.etAutoPowerOff)!;
            _btnSetAutoPowerOff = FindViewById<Button>(ResourceConstant.Id.btnSetAutoPowerOff)!;
            _btnGetAutoPowerOff = FindViewById<Button>(ResourceConstant.Id.btnGetAutoPowerOff)!;
            _pbBattery = FindViewById<ProgressBar>(ResourceConstant.Id.pbBattery)!;
            _tvBatteryPercent = FindViewById<TextView>(ResourceConstant.Id.tvBatteryPercent)!;
            _tvBatteryVoltage = FindViewById<TextView>(ResourceConstant.Id.tvBatteryVoltage)!;
            _etIpAddress = FindViewById<EditText>(ResourceConstant.Id.etIpAddress)!;
            _etApiKey = FindViewById<EditText>(ResourceConstant.Id.etApiKey)!;
            _btnSetConnection = FindViewById<Button>(ResourceConstant.Id.btnSetConnection)!;

            _httpService.StatusUpdated += OnStatusUpdated;
            _httpService.MediaUpdated += OnMediaUpdated;
            _httpService.MediaPowerStateUpdated += OnMediaPowerStateUpdated;
            _httpService.ConnectionStateChanged += OnConnectionStateChanged;

            SetSignalRButtonsEnabled(false);

            _btnPrevMedia.Click += async (_, _) =>
            {
                await SafeCall(() => _httpService.GetMediaPreviousAsync());
            };

            _btnNextMedia.Click += async (_, _) =>
            {
                await SafeCall(() => _httpService.GetMediaNextAsync());
            };

            _btnUpperLight.Click += async (_, _) => await PickColorAndSetLight("upper");
            _btnLowerLight.Click += async (_, _) => await PickColorAndSetLight("lower");
            _btnBothLights.Click += async (_, _) => await PickColorAndSetLight("both");
            _btnLightsOff.Click += async (_, _) => await SafeCall(() => _httpService.TurnOffLightsAsync());
            _btnBestColor.Click += async (_, _) => await SafeCall(() => _httpService.SetLightAsync("both", 255, 0, 255));
            _btnSetAutoPowerOff.Click += async (_, _) =>
            {
                if (int.TryParse(_etAutoPowerOff.Text, out int minutes))
                {
                    await SafeCall(() => _httpService.SetAutoPowerOffAsync(minutes));

                    if (_httpService.IsConnected)
                    {
                        Toast.MakeText(this, $"Auto Power-Off set to {minutes} minutes", ToastLength.Short)?.Show();
                    }
                }
                else
                {
                    Toast.MakeText(this, "Enter a valid number", ToastLength.Short)?.Show();
                }
            };

            _btnGetAutoPowerOff.Click += async (_, _) =>
            {
                int? minutes = null;

                await SafeCall(async () =>
                {
                    minutes = await _httpService.GetAutoPowerOffAsync();
                });

                if (minutes.HasValue && !_isDestroyed)
                    RunOnUiThread(() => _etAutoPowerOff.Text = minutes.Value.ToString());
            };

            _btnSetConnection.Click += async (_, _) =>
            {
                var address = _etIpAddress.Text?.Trim();
                var apiKey = _etApiKey.Text?.Trim();

                try
                {
                    SetButtonState(_btnSetConnection, false);
                    _btnSetConnection.Text = "Connecting...";

                    await _httpService.SetConnectionAsync(address, apiKey);

                    var prefs = GetSharedPreferences("LogiTrayPrefs", FileCreationMode.Private);

                    prefs?.Edit()?.PutString("MachineAddress", address)?.PutString("ApiKey", apiKey)?.Apply();

                    _connectionConfigured = true;

                    await StartSignalRConnectionAsync();

                    if (!_isDestroyed && _httpService.IsConnected)
                    {
                        Toast.MakeText(this, "Connection set!", ToastLength.Short)?.Show();
                    }
                }
                catch (Exception ex)
                {
                    if (!_isDestroyed)
                    {
                        Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long)?.Show();
                    }
                }
                finally
                {
                    if (!_isDestroyed)
                    {
                        SetButtonState(_btnSetConnection, true);
                        _btnSetConnection.Text = "Apply and Connect";
                    }
                }
            };

            var savedPrefs = GetSharedPreferences("LogiTrayPrefs", FileCreationMode.Private);

            var savedIp = savedPrefs?.GetString("MachineAddress", null);
            var savedApiKey = savedPrefs?.GetString("ApiKey", null);

            if (string.IsNullOrWhiteSpace(savedIp) || string.IsNullOrWhiteSpace(savedApiKey))
            {
                return;
            }

            _etIpAddress.Text = savedIp;
            _etApiKey.Text = savedApiKey;

            _httpService.SetConnection(savedIp, savedApiKey);
            _connectionConfigured = true;
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (_connectionConfigured)
                _ = HandleResumeAsync();
        }

        private async Task HandleResumeAsync()
        {
            try
            {
                await StartSignalRConnectionAsync();
            }
            catch (OperationCanceledException)
            {
                // expected cancellation.
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(nameof(OnResume), ex.ToString());
            }
        }

        protected override void OnDestroy()
        {
            _isDestroyed = true;

            _httpService.StatusUpdated -= OnStatusUpdated;
            _httpService.MediaUpdated -= OnMediaUpdated;
            _httpService.MediaPowerStateUpdated -= OnMediaPowerStateUpdated;
            _httpService.ConnectionStateChanged -= OnConnectionStateChanged;

            _colorPickerTcs?.TrySetCanceled();

            base.OnDestroy();

            _ = HandleDestroyAsync();
        }

        private async Task HandleDestroyAsync()
        {
            try
            {
                await _httpService.DisposeAsync();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(nameof(OnDestroy), ex.ToString());
            }
        }

        private async Task StartSignalRConnectionAsync()
        {
            try
            {
                if (_isDestroyed)
                    return;

                RunOnUiThread(() =>
                {
                    if (_isDestroyed)
                        return;

                    _tvConnectionState.Text = "Connecting...";
                    SetSignalRButtonsEnabled(false);
                });

                await _httpService.StartAsync();

                if (!_isDestroyed && _httpService.IsConnected)
                {
                    RunOnUiThread(() =>
                    {
                        if (_isDestroyed)
                            return;

                        _tvConnectionState.Text = "Connected";
                        SetSignalRButtonsEnabled(true);
                    });
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Error("SignalR connection attempt was canceled.");
            }
            catch (Exception ex)
            {
                Logger.Error($"SignalR connection error: {ex}");

                if (!_isDestroyed)
                {
                    RunOnUiThread(() =>
                    {
                        if (_isDestroyed)
                            return;

                        _tvConnectionState.Text = "Disconnected";
                        SetSignalRButtonsEnabled(false);

                        Toast.MakeText(this, $"Connection failed: {ex.Message}", ToastLength.Long)?.Show();
                    });
                }
            }
        }

        private void OnConnectionStateChanged(HubConnectionState state)
        {
            if (_isDestroyed)
                return;

            RunOnUiThread(() =>
            {
                if (_isDestroyed)
                    return;

                _tvConnectionState.Text = state switch
                {
                    HubConnectionState.Connected => "Connected",
                    HubConnectionState.Connecting => "Connecting...",
                    HubConnectionState.Reconnecting => "Reconnecting...",
                    _ => "Disconnected"
                };

                SetSignalRButtonsEnabled(state == HubConnectionState.Connected);

                if (state != HubConnectionState.Disconnected) return;
                _tvDevice.Text = "Device: Not Detected";
                _mediaControls.Visibility = ViewStates.Gone;
                _mediaSessionEnabled = false;
            });
        }

        private void SetSignalRButtonsEnabled(bool enabled)
        {
            SetButtonState(_btnPrevMedia, enabled && _mediaSessionEnabled);
            SetButtonState(_btnNextMedia, enabled && _mediaSessionEnabled);

            SetButtonState(_btnUpperLight, enabled);
            SetButtonState(_btnLowerLight, enabled);
            SetButtonState(_btnBothLights, enabled);
            SetButtonState(_btnLightsOff, enabled);
            SetButtonState(_btnBestColor, enabled);

            SetButtonState(_btnSetAutoPowerOff, enabled);
            SetButtonState(_btnGetAutoPowerOff, enabled);
        }

        private static void SetButtonState(Button button, bool enabled)
        {
            button.Enabled = enabled;
            button.Alpha = enabled ? 1.0f : 0.4f;

            button.SetTextColor(Android.Content.Res.ColorStateList.ValueOf(enabled ? Color.White : Color.ParseColor("#888888")));

            button.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(enabled ? Color.ParseColor("#333333") : Color.ParseColor("#202020"));
        }

        private void OnStatusUpdated(StatusResponse status)
        {
            if (_isDestroyed)
                return;

            RunOnUiThread(() =>
            {
                if (_isDestroyed)
                    return;

                _tvDevice.Text = $"Device: {status.Device}";

                if (status.Battery != null)
                {
                    _pbBattery.Progress = status.Battery.Level;
                    _tvBatteryPercent.Text = $"Battery: {status.Battery.Level}% ({status.Battery.Status})";

                    _tvBatteryVoltage.Text = $"Voltage: {status.Battery.VoltageMv} mV";
                }

                UpdateHeadsetState(status.State == DeviceConnectionState.HeadsetSleeping);
            });
        }

        private void OnMediaPowerStateUpdated(SessionPowerStateResponse state)
        {
            bool newState = state.SessionPwr;
            _mediaSessionEnabled = newState;

            if (_isDestroyed)
                return;

            RunOnUiThread(() =>
            {
                if (_isDestroyed)
                    return;

                _mediaControls.Visibility = _mediaSessionEnabled
                    ? ViewStates.Visible
                    : ViewStates.Gone;

                SetButtonState(_btnPrevMedia, _httpService.IsConnected && _mediaSessionEnabled);
                SetButtonState(_btnNextMedia, _httpService.IsConnected && _mediaSessionEnabled);

                if (!_mediaSessionEnabled)
                    _tvMediaSession.Text = "No media session";
            });
        }

        private void OnMediaUpdated(MediaResponse media)
        {
            if (!_mediaSessionEnabled || _isDestroyed)
                return;

            RunOnUiThread(() =>
            {
                if (_isDestroyed)
                    return;

                _tvMediaSession.Text = string.IsNullOrWhiteSpace(media.Media)
                        ? "No media session" : $"{media.Session} - {media.Media}";
            });
        }

        private async Task SafeCall(Func<Task> action)
        {
            if (!_httpService.IsConnected)
            {
                if (!_isDestroyed)
                {
                    Toast.MakeText(this, "Not connected to the server.", ToastLength.Short)?.Show();
                }

                return;
            }

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                if (!_isDestroyed)
                {
                    RunOnUiThread(() =>
                    {
                        Logger.Error(ex.ToString());

                        Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long)?.Show();
                    });
                }
            }
        }

        private void UpdateHeadsetState(bool isAsleep)
        {
            if (_isDestroyed)
                return;

            RunOnUiThread(() =>
            {
                if (_isDestroyed)
                    return;

                _overlayAsleep.Visibility = isAsleep ? ViewStates.Visible : ViewStates.Gone;
            });
        }

        private async Task PickColorAndSetLight(string target)
        {
            if (!_httpService.IsConnected)
            {
                Toast.MakeText(this, "Not connected to the server.", ToastLength.Short)?.Show();

                return;
            }

            _colorPickerTcs = new TaskCompletionSource<(int[] Color, LightMode Mode)>();

            RunOnUiThread(() =>
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetTitle("Pick Color and Mode");

                var layout = new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical
                };

                layout.SetPadding(40, 30, 40, 20);
                layout.SetBackgroundColor(Color.ParseColor("#202124"));

                var modeSpinner = new Spinner(this);
                var modes = Enum.GetValues<LightMode>().ToList();

                var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, 
                    modes.Select(m => m.ToString()).ToList());

                adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

                modeSpinner.Adapter = new DarkSpinnerAdapter(adapter);

                var selectedMode = LightMode.Static;
                modeSpinner.SetSelection(modes.IndexOf(selectedMode));
                layout.AddView(modeSpinner);

                var preview = new View(this)
                {
                    LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 100)
                };

                var previewDrawable = new GradientDrawable();
                previewDrawable.SetCornerRadius(30f);
                previewDrawable.SetColor(Color.White);
                preview.Background = previewDrawable;

                var previewParams = (LinearLayout.LayoutParams)preview.LayoutParameters;

                previewParams.TopMargin = 25;
                previewParams.BottomMargin = 25;
                layout.AddView(preview);

                TextView tvR = new TextView(this);
                TextView tvG = new TextView(this);
                TextView tvB = new TextView(this);

                tvR.SetTextColor(Color.White);
                tvG.SetTextColor(Color.White);
                tvB.SetTextColor(Color.White);

                tvR.Text = "R: 0";
                tvG.Text = "G: 0";
                tvB.Text = "B: 0";

                SeekBar sbR = new SeekBar(this) { Max = 255 };
                SeekBar sbG = new SeekBar(this) { Max = 255 };
                SeekBar sbB = new SeekBar(this) { Max = 255 };

                layout.AddView(tvR);
                layout.AddView(sbR);
                layout.AddView(tvG);
                layout.AddView(sbG);
                layout.AddView(tvB);
                layout.AddView(sbB);

                sbR.ProgressChanged += (_, e) =>
                {
                    if (e.FromUser)
                        UpdatePreview();
                };

                sbG.ProgressChanged += (_, e) =>
                {
                    if (e.FromUser)
                        UpdatePreview();
                };

                sbB.ProgressChanged += (_, e) =>
                {
                    if (e.FromUser)
                        UpdatePreview();
                };

                modeSpinner.ItemSelected += (_, e) =>
                {
                    selectedMode = modes[e.Position];
                    SetColorControlsEnabled(selectedMode != LightMode.Cycle);
                };

                builder.SetView(layout);

                builder.SetPositiveButton("OK", (_, _) =>
                {
                    _colorPickerTcs.TrySetResult((
                        [sbR.Progress, sbG.Progress, sbB.Progress],
                        selectedMode));
                });

                builder.SetNegativeButton("Cancel", (_, _) =>
                {
                    _colorPickerTcs.TrySetCanceled();
                });

                var dialog = builder.Create();
                dialog?.Show();

                dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.ParseColor("#202124")));

                dialog?.GetButton((int)DialogButtonType.Positive)?.SetTextColor(Color.ParseColor("#8AB4F8"));

                dialog?.GetButton((int)DialogButtonType.Negative)?.SetTextColor(Color.ParseColor("#8AB4F8"));

                if (Resources != null)
                {
                    int titleId = Resources.GetIdentifier("alertTitle", "id", "android");

                    var title = dialog?.FindViewById<TextView>(titleId);
                    title?.SetTextColor(Color.White);
                }

                UpdatePreview();
                return;

                void UpdatePreview()
                {
                    if (!sbR.Enabled)
                        return;

                    int r = sbR.Progress;
                    int g = sbG.Progress;
                    int b = sbB.Progress;

                    previewDrawable.SetColor(Color.Rgb(r, g, b));

                    tvR.Text = $"R: {r}";
                    tvG.Text = $"G: {g}";
                    tvB.Text = $"B: {b}";
                }

                void SetColorControlsEnabled(bool enabled)
                {
                    sbR.Enabled = enabled;
                    sbG.Enabled = enabled;
                    sbB.Enabled = enabled;

                    tvR.Enabled = enabled;
                    tvG.Enabled = enabled;
                    tvB.Enabled = enabled;

                    preview.Alpha = enabled ? 1f : 0.35f;
                }
            });

            try
            {
                var selected = await _colorPickerTcs.Task;

                if (_isDestroyed)
                    return;

                await SafeCall(() => _httpService.SetLightAsync(target,
                    selected.Color[0], selected.Color[1], selected.Color[2], selected.Mode));
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}