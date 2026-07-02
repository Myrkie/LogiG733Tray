using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Graphics;
using Android.Views;
using LogiTrayAndroid.Services;
using LogiTrayAndroid.Services.Models;

namespace LogiTrayAndroid
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation 
                               | Android.Content.PM.ConfigChanges.ScreenSize
    )]
    public class MainActivity : Activity
    {
        private bool _mediaSessionEnabled;
        
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
                         _tvNextRefresh = null!,
                         _tvMediaSession = null!;

        private EditText _etAutoPowerOff = null!,
                         _etIpAddress = null!,
                         _etApiKey = null!;

        private LinearLayout _mediaControls = null!;
        private LinearLayout _overlayAsleep = null!;
        private ProgressBar _pbBattery = null!;

        private readonly HttpService _httpService = new();

        private const int RefreshIntervalMs = 5000;
        private CancellationTokenSource? _refreshCts;
        private int _nextRefreshSeconds;

        private TaskCompletionSource<(int[] Color, LightMode Mode)>? _colorPickerTcs;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(ResourceConstant.Layout.activity_main);

            _overlayAsleep = FindViewById<LinearLayout>(ResourceConstant.Id.overlayAsleep)!;
            _tvDevice = FindViewById<TextView>(ResourceConstant.Id.tvDevice)!;
            _tvNextRefresh = FindViewById<TextView>(ResourceConstant.Id.tvNextRefresh)!;

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

            
            _btnPrevMedia.Click += async (_, _) =>
            {
                await SafeCall(async () =>
                {
                    await _httpService.GetMediaPreviousAsync();
                    await RefreshMediaUi();
                });
            };

            _btnNextMedia.Click += async (_, _) =>
            {
                await SafeCall(async () =>
                {
                    await _httpService.GetMediaNextAsync();
                    await RefreshMediaUi();
                });
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
                    Toast.MakeText(this, $"Auto Power-Off set to {minutes} minutes", ToastLength.Short)?.Show();
                }
                else
                {
                    Toast.MakeText(this, "Enter a valid number", ToastLength.Short)?.Show();
                }
            };

            _btnGetAutoPowerOff.Click += async (_, _) =>
            {
                int minutes = 0;
                await SafeCall(async () => { minutes = await _httpService.GetAutoPowerOffAsync(); });
                RunOnUiThread(() => _etAutoPowerOff.Text = minutes.ToString());
            };

            _btnSetConnection.Click += (_, _) =>
            {
                var address = _etIpAddress.Text?.Trim();
                var apiKey = _etApiKey.Text?.Trim();

                try
                {
                    _httpService.SetConnection(address, apiKey);

                    var prefs = GetSharedPreferences("LogiTrayPrefs", FileCreationMode.Private);
                    prefs?.Edit()?.PutString("MachineAddress", address)?.PutString("ApiKey", apiKey)?.Apply();

                    Toast.MakeText(this, "Connection set!", ToastLength.Short)?.Show();

                    StartRefreshLoop();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long)?.Show();
                }
            };

            var savedPrefs = GetSharedPreferences("LogiTrayPrefs", FileCreationMode.Private);
            var savedIp = savedPrefs?.GetString("MachineAddress", null);
            var savedApiKey = savedPrefs?.GetString("ApiKey", null);

            if (string.IsNullOrWhiteSpace(savedIp) || string.IsNullOrWhiteSpace(savedApiKey)) return;
            _etIpAddress.Text = savedIp;
            _etApiKey.Text = savedApiKey;
            _httpService.SetConnection(savedIp, savedApiKey);

            StartRefreshLoop();
        }

        protected override void OnPause()
        {
            base.OnPause();
            StopRefreshLoop();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (_httpService.IsConnected)
                StartRefreshLoop();
        }

        private async Task SafeCall(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Logger.Error(ex.ToString());
                    Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long)?.Show();
                });
            }
        }

        private void UpdateHeadsetState(bool isAsleep)
        {
            RunOnUiThread(() =>
            {
                _overlayAsleep.Visibility = isAsleep ? ViewStates.Visible : ViewStates.Gone;
            });
        }

        private void StartRefreshLoop()
        {
            StopRefreshLoop();
            _refreshCts = new CancellationTokenSource();
            var token = _refreshCts.Token;

            _nextRefreshSeconds = RefreshIntervalMs / 1000;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    RunOnUiThread(() =>
                    {
                        _tvNextRefresh.Text = _httpService.IsConnected
                            ? $"Next refresh in: {_nextRefreshSeconds}s"
                            : "Next refresh: -s";
                    });

                    await Task.Delay(1000, token);

                    if (_nextRefreshSeconds > 0)
                    {
                        _nextRefreshSeconds--;
                        continue;
                    }

                    bool success = false;
                    try
                    {
                        success = await RefreshStatus();
                    }
                    catch (HttpService.UnauthorizedException)
                    {
                        Logger.Error("Unauthorized API key, stopping refresh loop.");
                        StopRefreshLoop();
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Android.OS.OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Refresh error: {ex}");
                    }

                    _nextRefreshSeconds = success ? RefreshIntervalMs / 1000 : 1;
                }
            }, token);
        }


        private void StopRefreshLoop()
        {
            _refreshCts?.Cancel();
            _refreshCts = null;

            RunOnUiThread(() =>
            {
                if (_httpService.IsConnected) return;
                _tvDevice.Text = "Device: Not Detected";
                _tvNextRefresh.Text = "Next refresh: -s";
            });
        }

        private async Task<bool> RefreshStatus()
        {
            if (!_httpService.IsConnected) return false;

            try
            {
                var status = await _httpService.GetStatusAsync();
                await RefreshSessionState();
                await RefreshMediaUi();
                RunOnUiThread(() =>
                {
                    _tvDevice.Text = $"Device: {status!.Device}";
                    _pbBattery.Progress = status.Battery!.Level;
                    _tvBatteryPercent.Text = $"Battery: {status.Battery.Level}% ({status.Battery.Status})";
                    _tvBatteryVoltage.Text = $"Voltage: {status.Battery.VoltageMv} mV";

                    UpdateHeadsetState(status.State == DeviceConnectionState.HeadsetSleeping);
                });

                return true;
            }
            catch (HttpService.UnauthorizedException)
            {
                Logger.Error("Unauthorized API key, stopping refresh loop.");
                StopRefreshLoop();
                return false;
            }
            catch (HttpRequestException ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, $"Failed to connect to {ex.Message}, check that LogiG733Tray is running",
                        ToastLength.Short)?.Show();
                });
                StopRefreshLoop();
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return false;
            }
        }
        
        private async Task RefreshSessionState()
        {
            if (!_httpService.IsConnected) return;

            try
            {
                var state = await _httpService.GetMediaPowerStateAsync();
                bool newState = state?.SessionPwr ?? false;

                bool becameEnabled = newState && !_mediaSessionEnabled;

                _mediaSessionEnabled = newState;

                RunOnUiThread(() =>
                {
                    _mediaControls.Visibility = _mediaSessionEnabled
                        ? ViewStates.Visible
                        : ViewStates.Gone;
                });

                if (becameEnabled)
                {
                    await RefreshMediaUi();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }
        
        private async Task RefreshMediaUi()
        {
            if (!_httpService.IsConnected || !_mediaSessionEnabled) return;

            try
            {
                var media = await _httpService.GetMediaStatusAsync();

                RunOnUiThread(() =>
                {
                    _tvMediaSession.Text =
                        string.IsNullOrWhiteSpace(media?.Media)
                            ? "No media session"
                            : $"{media.Session} - {media.Media}";
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private async Task PickColorAndSetLight(string target)
        {
            _colorPickerTcs = new TaskCompletionSource<(int[], LightMode)>();

            RunOnUiThread(() =>
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetTitle("Pick Color and Mode");

                var layout = new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical,
                    DividerPadding = 20,
                };

                var modeSpinner = new Spinner(this);

                var modes = Enum.GetValues(typeof(LightMode))
                    .Cast<LightMode>()
                    .ToList();

                var adapter = new ArrayAdapter(
                    this,
                    Android.Resource.Layout.SimpleSpinnerItem,
                    modes.Select(m => m.ToString()).ToList()
                );
                
                adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                modeSpinner.Adapter = adapter;

                var selectedMode = LightMode.Static;
                modeSpinner.SetSelection(modes.IndexOf(selectedMode));

                layout.AddView(modeSpinner);

                var preview = new View(this)
                {
                    LayoutParameters = new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent, 100)
                };
                layout.AddView(preview);

                SeekBar sbR = new SeekBar(this) { Max = 255 };
                SeekBar sbG = new SeekBar(this) { Max = 255 };
                SeekBar sbB = new SeekBar(this) { Max = 255 };

                TextView tvR = new TextView(this) { Text = "R: 0" };
                TextView tvG = new TextView(this) { Text = "G: 0" };
                TextView tvB = new TextView(this) { Text = "B: 0" };

                layout.AddView(tvR);
                layout.AddView(sbR);
                layout.AddView(tvG);
                layout.AddView(sbG);
                layout.AddView(tvB);
                layout.AddView(sbB);

                void SetColorControlsEnabled(bool enabled)
                {
                    sbR.Enabled = sbG.Enabled = sbB.Enabled = enabled;
                    tvR.Enabled = tvG.Enabled = tvB.Enabled = enabled;
                    preview.Alpha = enabled ? 1f : 0.3f;
                }

                void UpdatePreview()
                {
                    if (!sbR.Enabled) return;

                    int r = sbR.Progress;
                    int g = sbG.Progress;
                    int b = sbB.Progress;

                    preview.SetBackgroundColor(Color.Argb(255, r, g, b));
                    tvR.Text = $"R: {r}";
                    tvG.Text = $"G: {g}";
                    tvB.Text = $"B: {b}";
                }

                sbR.ProgressChanged += (_, e) => { if (e.FromUser) UpdatePreview(); };
                sbG.ProgressChanged += (_, e) => { if (e.FromUser) UpdatePreview(); };
                sbB.ProgressChanged += (_, e) => { if (e.FromUser) UpdatePreview(); };
                modeSpinner.ItemSelected += (_, e) =>
                {
                    selectedMode = modes[e.Position];

                    SetColorControlsEnabled(selectedMode != LightMode.Cycle);
                };

                builder.SetView(layout);

                builder.SetPositiveButton("OK", (_, _) =>
                {
                    _colorPickerTcs.TrySetResult(([sbR.Progress, sbG.Progress, sbB.Progress], selectedMode));
                });

                builder.SetNegativeButton("Cancel", (_, _) => { });

                
                builder.Create()?.Show();
            });

            var selected = await _colorPickerTcs.Task;
            await SafeCall(() => _httpService.SetLightAsync(target, selected.Color[0], selected.Color[1], selected.Color[2], selected.Mode));
        }
    }
}