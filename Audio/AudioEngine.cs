using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PortAudioSharp;
using StardewModdingAPI;
using PaStream = PortAudioSharp.Stream;

namespace ProximityVoiceChat.Audio;

/// <summary>Owns the PortAudio streams. All failures are reported and disable voice instead of crashing the game.</summary>
public sealed class AudioEngine : IDisposable
{
    private readonly IMonitor _monitor;
    private readonly List<DeviceEntry> _inputDevices = new();
    private readonly List<DeviceEntry> _outputDevices = new();
    private readonly float[] _captureBuffer = new float[8192];
    private readonly float[] _mixBuffer = new float[8192];
    private readonly float[] _outputBuffer = new float[8192 * 2];

    private readonly PaStream.Callback _captureCallback;
    private readonly PaStream.Callback _playbackCallback;

    private PaStream? _captureStream;
    private PaStream? _playbackStream;
    private Action<float[], int>? _captureHandler;
    private PlaybackMixer? _mixer;
    private string _requestedInput = "";
    private string _requestedOutput = "";
    private string _effectiveInput = "";
    private string _effectiveOutput = "";
    private int _playbackChannels = 2;
    private bool _initialized;
    private bool _available;
    private bool _disposed;

    public AudioEngine(IMonitor monitor)
    {
        this._monitor = monitor;
        this._captureCallback = this.OnCaptureCallback;
        this._playbackCallback = this.OnPlaybackCallback;
    }

    public bool IsAvailable => this._available;

    public bool IsCaptureRunning => this._captureStream is not null;

    public bool IsPlaybackRunning => this._playbackStream is not null;

    public string EffectiveInputDevice => this._effectiveInput;

    public string EffectiveOutputDevice => this._effectiveOutput;

    /// <summary>Initialize PortAudio and enumerate devices. Never throws.</summary>
    public bool Initialize()
    {
        if (this._initialized)
            return this._available;

        this._initialized = true;
        try
        {
            PortAudio.Initialize();
            this._available = true;
            this.RefreshDevices();
            this._monitor.Log(
                $"Audio backend ready (PortAudio {PortAudio.Version}; {this._inputDevices.Count} input, {this._outputDevices.Count} output device(s)).",
                LogLevel.Info);
        }
        catch (Exception ex)
        {
            this._available = false;
            this._monitor.Log($"Audio backend unavailable, voice chat will be disabled: {ex.Message}", LogLevel.Error);
        }

        return this._available;
    }

    /// <summary>Read the current device list without opening any stream.</summary>
    public void RefreshDevices()
    {
        this._inputDevices.Clear();
        this._outputDevices.Clear();

        if (!this._available)
            return;

        try
        {
            int defaultInput = PortAudio.DefaultInputDevice;
            int defaultOutput = PortAudio.DefaultOutputDevice;
            int count = PortAudio.DeviceCount;

            for (int index = 0; index < count; index++)
            {
                DeviceInfo info = PortAudio.GetDeviceInfo(index);
                if (info.maxInputChannels > 0)
                    AddDevice(this._inputDevices, index, info.name, index == defaultInput);

                if (info.maxOutputChannels > 0)
                    AddDevice(this._outputDevices, index, info.name, index == defaultOutput);
            }
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Could not enumerate audio devices: {ex.Message}", LogLevel.Warn);
        }
    }

    public IReadOnlyList<string> GetInputDevices()
    {
        return GetNames(this._inputDevices);
    }

    public IReadOnlyList<string> GetOutputDevices()
    {
        return GetNames(this._outputDevices);
    }

    /// <summary>Open or move the microphone stream. Returns false and reports a soft failure.</summary>
    public bool StartCapture(string? requestedDevice, Action<float[], int> handler, out string error)
    {
        this.StopCapture();
        this._requestedInput = requestedDevice ?? "";

        if (!this._available)
        {
            error = "audio backend is unavailable";
            return false;
        }

        if (!this.TryResolveDevice(this._inputDevices, this._requestedInput, isInput: true, out DeviceEntry device, out string? substitution))
        {
            error = "no usable input device was found";
            this._monitor.Log($"Voice capture disabled: {error}.", LogLevel.Error);
            return false;
        }

        if (substitution is not null)
            this._monitor.Log(substitution, LogLevel.Warn);

        PaStream? stream = null;
        try
        {
            DeviceInfo info = PortAudio.GetDeviceInfo(device.Index);
            StreamParameters input = new()
            {
                device = device.Index,
                channelCount = 1,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = info.defaultLowInputLatency,
                hostApiSpecificStreamInfo = IntPtr.Zero
            };

            stream = new PaStream(
                input,
                null,
                ModConstants.SampleRate,
                ModConstants.FrameSamples,
                StreamFlags.NoFlag,
                this._captureCallback,
                null);

            this._captureHandler = handler;
            stream.Start();
            this._captureStream = stream;
            this._effectiveInput = device.Name;
            error = "";
            this._monitor.Log($"Microphone open on '{device.Name}'.", LogLevel.Info);
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                stream?.Dispose();
            }
            catch
            {
                // Ignore cleanup failures; the original error is the useful one.
            }

            this._captureHandler = null;
            this._effectiveInput = "";
            error = ex.Message;
            this._monitor.Log($"Could not open microphone '{device.Name}': {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    public void StopCapture()
    {
        PaStream? stream = this._captureStream;
        this._captureStream = null;
        this._captureHandler = null;
        this._effectiveInput = "";

        if (stream is null)
            return;

        try
        {
            stream.Stop();
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Error stopping the microphone stream: {ex.Message}", LogLevel.Trace);
        }

        try
        {
            stream.Dispose();
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Error closing the microphone stream: {ex.Message}", LogLevel.Trace);
        }
    }

    /// <summary>Open or move the voice output stream. Returns false and reports a soft failure.</summary>
    public bool StartPlayback(string? requestedDevice, PlaybackMixer mixer, out string error)
    {
        this.StopPlayback();
        this._requestedOutput = requestedDevice ?? "";

        if (!this._available)
        {
            error = "audio backend is unavailable";
            return false;
        }

        if (!this.TryResolveDevice(this._outputDevices, this._requestedOutput, isInput: false, out DeviceEntry device, out string? substitution))
        {
            error = "no usable output device was found";
            this._monitor.Log($"Voice playback disabled: {error}.", LogLevel.Error);
            return false;
        }

        if (substitution is not null)
            this._monitor.Log(substitution, LogLevel.Warn);

        PaStream? stream = null;
        try
        {
            DeviceInfo info = PortAudio.GetDeviceInfo(device.Index);
            int channels = Math.Clamp(info.maxOutputChannels, 1, 2);
            StreamParameters output = new()
            {
                device = device.Index,
                channelCount = channels,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = info.defaultLowOutputLatency,
                hostApiSpecificStreamInfo = IntPtr.Zero
            };

            stream = new PaStream(
                null,
                output,
                ModConstants.SampleRate,
                ModConstants.FrameSamples,
                StreamFlags.NoFlag,
                this._playbackCallback,
                null);

            this._mixer = mixer;
            this._playbackChannels = channels;
            stream.Start();
            this._playbackStream = stream;
            this._effectiveOutput = device.Name;
            error = "";
            this._monitor.Log($"Voice playback open on '{device.Name}' ({channels} channel(s)).", LogLevel.Info);
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                stream?.Dispose();
            }
            catch
            {
                // Ignore cleanup failures; the original error is the useful one.
            }

            this._mixer = null;
            this._effectiveOutput = "";
            error = ex.Message;
            this._monitor.Log($"Could not open voice output '{device.Name}': {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    public void StopPlayback()
    {
        PaStream? stream = this._playbackStream;
        this._playbackStream = null;
        this._mixer = null;
        this._effectiveOutput = "";

        if (stream is null)
            return;

        try
        {
            stream.Stop();
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Error stopping the voice output stream: {ex.Message}", LogLevel.Trace);
        }

        try
        {
            stream.Dispose();
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Error closing the voice output stream: {ex.Message}", LogLevel.Trace);
        }
    }

    /// <summary>Restart only the streams whose configured device changed.</summary>
    public void ApplyDevices(string inputDevice, string outputDevice)
    {
        string requestedInput = inputDevice ?? "";
        string requestedOutput = outputDevice ?? "";

        bool inputChanged = !string.Equals(requestedInput, this._requestedInput, StringComparison.Ordinal);
        bool outputChanged = !string.Equals(requestedOutput, this._requestedOutput, StringComparison.Ordinal);

        this._requestedInput = requestedInput;
        this._requestedOutput = requestedOutput;

        if (inputChanged && this._captureHandler is not null)
        {
            Action<float[], int> handler = this._captureHandler;
            if (!this.StartCapture(requestedInput, handler, out _))
                this._monitor.Log("Microphone restart failed; voice capture is disabled until the next attempt.", LogLevel.Warn);
        }

        if (outputChanged && this._mixer is not null)
        {
            PlaybackMixer mixer = this._mixer;
            if (!this.StartPlayback(requestedOutput, mixer, out _))
                this._monitor.Log("Voice playback restart failed.", LogLevel.Warn);
        }
    }

    /// <summary>Re-resolve devices periodically so a removed device falls back to the system default.</summary>
    public void EnsureDevices()
    {
        if (!this._available)
            return;

        this.RefreshDevices();

        if (this._captureStream is not null)
        {
            bool missing = this._requestedInput.Length > 0 && !ContainsDevice(this._inputDevices, this._requestedInput);
            if (missing || IsStreamStopped(this._captureStream))
            {
                Action<float[], int>? handler = this._captureHandler;
                if (handler is not null)
                {
                    this._monitor.Log("The selected microphone disappeared; falling back to the system default.", LogLevel.Warn);
                    this.StartCapture("", handler, out _);
                }
            }
        }

        if (this._playbackStream is not null)
        {
            bool missing = this._requestedOutput.Length > 0 && !ContainsDevice(this._outputDevices, this._requestedOutput);
            if (missing || IsStreamStopped(this._playbackStream))
            {
                PlaybackMixer? mixer = this._mixer;
                if (mixer is not null)
                {
                    this._monitor.Log("The selected output device disappeared; falling back to the system default.", LogLevel.Warn);
                    this.StartPlayback("", mixer, out _);
                }
            }
        }
    }

    public void Dispose()
    {
        if (this._disposed)
            return;

        this._disposed = true;
        this.StopCapture();
        this.StopPlayback();

        if (this._available)
        {
            try
            {
                PortAudio.Terminate();
            }
            catch (Exception ex)
            {
                this._monitor.Log($"Error shutting down PortAudio: {ex.Message}", LogLevel.Trace);
            }
        }
    }

    private StreamCallbackResult OnCaptureCallback(IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userDataPtr)
    {
        try
        {
            Action<float[], int>? handler = this._captureHandler;
            if (handler is null || frameCount == 0 || input == IntPtr.Zero)
                return StreamCallbackResult.Continue;

            int remaining = (int)frameCount;
            int offset = 0;
            while (remaining > 0)
            {
                int chunk = Math.Min(remaining, this._captureBuffer.Length);
                Marshal.Copy(IntPtr.Add(input, offset * sizeof(float)), this._captureBuffer, 0, chunk);
                handler(this._captureBuffer, chunk);
                offset += chunk;
                remaining -= chunk;
            }
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Microphone callback failed and dropped audio: {ex.Message}", LogLevel.Trace);
        }

        return StreamCallbackResult.Continue;
    }

    private StreamCallbackResult OnPlaybackCallback(IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userDataPtr)
    {
        try
        {
            PlaybackMixer? mixer = this._mixer;
            if (mixer is null || frameCount == 0 || output == IntPtr.Zero)
                return StreamCallbackResult.Continue;

            int remaining = (int)frameCount;
            int offset = 0;
            int channels = this._playbackChannels;

            while (remaining > 0)
            {
                int chunk = Math.Min(remaining, this._mixBuffer.Length);
                mixer.Mix(this._mixBuffer.AsSpan(0, chunk));

                if (channels == 1)
                {
                    Array.Copy(this._mixBuffer, this._outputBuffer, chunk);
                }
                else
                {
                    for (int i = 0; i < chunk; i++)
                    {
                        float sample = this._mixBuffer[i];
                        this._outputBuffer[i * 2] = sample;
                        this._outputBuffer[i * 2 + 1] = sample;
                    }
                }

                Marshal.Copy(this._outputBuffer, 0, IntPtr.Add(output, offset * channels * sizeof(float)), chunk * channels);
                offset += chunk;
                remaining -= chunk;
            }
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Voice output callback failed and dropped audio: {ex.Message}", LogLevel.Trace);
        }

        return StreamCallbackResult.Continue;
    }

    private bool TryResolveDevice(List<DeviceEntry> devices, string requested, bool isInput, out DeviceEntry device, out string? substitution)
    {
        substitution = null;
        device = default;

        if (devices.Count == 0)
            return false;

        if (requested.Length > 0)
        {
            foreach (DeviceEntry entry in devices)
            {
                if (string.Equals(entry.Name, requested, StringComparison.Ordinal))
                {
                    device = entry;
                    return true;
                }
            }
        }

        DeviceEntry? fallback = null;
        foreach (DeviceEntry entry in devices)
        {
            if (entry.IsDefault)
            {
                fallback = entry;
                break;
            }
        }

        fallback ??= devices[0];
        device = fallback.Value;

        if (requested.Length > 0)
        {
            substitution = $"Configured {(isInput ? "input" : "output")} device '{requested}' is unavailable; using system default '{device.Name}'.";
        }

        return true;
    }

    private static void AddDevice(List<DeviceEntry> devices, int index, string name, bool isDefault)
    {
        foreach (DeviceEntry existing in devices)
        {
            if (string.Equals(existing.Name, name, StringComparison.Ordinal))
                return;
        }

        devices.Add(new DeviceEntry(index, name, isDefault));
    }

    private static bool ContainsDevice(List<DeviceEntry> devices, string name)
    {
        foreach (DeviceEntry entry in devices)
        {
            if (string.Equals(entry.Name, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static IReadOnlyList<string> GetNames(List<DeviceEntry> devices)
    {
        string[] names = new string[devices.Count];
        for (int i = 0; i < devices.Count; i++)
            names[i] = devices[i].Name;

        return names;
    }

    private static bool IsStreamStopped(PaStream stream)
    {
        try
        {
            return stream.IsStopped;
        }
        catch
        {
            return true;
        }
    }

    private readonly struct DeviceEntry
    {
        public DeviceEntry(int index, string name, bool isDefault)
        {
            this.Index = index;
            this.Name = name;
            this.IsDefault = isDefault;
        }

        public int Index { get; }

        public string Name { get; }

        public bool IsDefault { get; }
    }
}
