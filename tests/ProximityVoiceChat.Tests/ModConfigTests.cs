namespace ProximityVoiceChat.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using ProximityVoiceChat.Config;
using Xunit;

public class ModConfigTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Defaults_MatchDocumentedValues()
    {
        ModConfig config = new();

        Assert.Equal("", config.InputDeviceName);
        Assert.Equal("", config.OutputDeviceName);
        Assert.Equal(100, config.PlaybackVolume);
        Assert.True(config.WallOcclusion);
        Assert.Equal(10, config.ProximityRadius);
        Assert.Equal(ActivationMode.VoiceActivity, config.ActivationMode);
        Assert.Equal(50, config.VoiceSensitivity);
        Assert.False(config.StartMicrophoneEnabled);
        Assert.Equal("V", config.PushToTalkKey);
        Assert.Equal("F7", config.MicrophoneToggleKey);
        Assert.Equal("F9", config.MicrophoneTestKey);
        Assert.False(config.MicrophoneTestMonitor);
        Assert.Equal("F8", config.CycleRadiusKey);
        Assert.True(config.ShowIndicator);
    }

    [Fact]
    public void Serialization_RoundTripPreservesEveryField()
    {
        ModConfig original = new()
        {
            InputDeviceName = "Microphone (USB)",
            OutputDeviceName = "Speakers (USB)",
            PlaybackVolume = 65,
            WallOcclusion = false,
            ProximityRadius = 20,
            ActivationMode = ActivationMode.PushToTalk,
            VoiceSensitivity = 15,
            StartMicrophoneEnabled = true,
            PushToTalkKey = "LeftControl",
            MicrophoneToggleKey = "F8",
            MicrophoneTestKey = "F10",
            MicrophoneTestMonitor = true,
            CycleRadiusKey = "F9",
            ShowIndicator = false
        };

        string json = JsonSerializer.Serialize(original, Options);
        ModConfig? copy = JsonSerializer.Deserialize<ModConfig>(json, Options);

        Assert.NotNull(copy);
        Assert.Equal(original.InputDeviceName, copy!.InputDeviceName);
        Assert.Equal(original.OutputDeviceName, copy.OutputDeviceName);
        Assert.Equal(original.PlaybackVolume, copy.PlaybackVolume);
        Assert.Equal(original.WallOcclusion, copy.WallOcclusion);
        Assert.Equal(original.ProximityRadius, copy.ProximityRadius);
        Assert.Equal(original.ActivationMode, copy.ActivationMode);
        Assert.Equal(original.VoiceSensitivity, copy.VoiceSensitivity);
        Assert.Equal(original.StartMicrophoneEnabled, copy.StartMicrophoneEnabled);
        Assert.Equal(original.PushToTalkKey, copy.PushToTalkKey);
        Assert.Equal(original.MicrophoneToggleKey, copy.MicrophoneToggleKey);
        Assert.Equal(original.MicrophoneTestKey, copy.MicrophoneTestKey);
        Assert.Equal(original.MicrophoneTestMonitor, copy.MicrophoneTestMonitor);
        Assert.Equal(original.CycleRadiusKey, copy.CycleRadiusKey);
        Assert.Equal(original.ShowIndicator, copy.ShowIndicator);
    }

    [Fact]
    public void Deserialize_MissingStartMicrophoneFieldDefaultsToFalse()
    {
        const string json = """
        {
          "InputDeviceName": "",
          "OutputDeviceName": "",
          "PlaybackVolume": 100,
          "ProximityRadius": 10,
          "ActivationMode": "VoiceActivity",
          "VoiceSensitivity": 50,
          "PushToTalkKey": "V",
          "MicrophoneToggleKey": "F7",
          "CycleRadiusKey": "F8",
          "ShowIndicator": true
        }
        """;

        ModConfig? config = JsonSerializer.Deserialize<ModConfig>(json, Options);

        Assert.NotNull(config);
        Assert.False(config!.StartMicrophoneEnabled);
        Assert.True(config.WallOcclusion);
        Assert.Equal("F9", config.MicrophoneTestKey);
        Assert.False(config.MicrophoneTestMonitor);
    }

    [Fact]
    public void ResetToDefaults_DisablesStartMicrophone()
    {
        ModConfig config = new() { StartMicrophoneEnabled = true, MicrophoneTestMonitor = true, WallOcclusion = false };

        config.ResetToDefaults();

        Assert.False(config.StartMicrophoneEnabled);
        Assert.True(config.WallOcclusion);
        Assert.Equal("F9", config.MicrophoneTestKey);
        Assert.False(config.MicrophoneTestMonitor);
    }

    [Fact]
    public void Normalize_DoesNotChangeStartMicrophone()
    {
        ModConfig config = new() { StartMicrophoneEnabled = true };

        config.Normalize();

        Assert.True(config.StartMicrophoneEnabled);
    }

    [Fact]
    public void Normalize_RepairsOutOfRangeValues()
    {
        ModConfig config = new()
        {
            InputDeviceName = null!,
            OutputDeviceName = null!,
            PlaybackVolume = 900,
            ProximityRadius = 4,
            VoiceSensitivity = -20,
            PushToTalkKey = "",
            MicrophoneToggleKey = "None",
            CycleRadiusKey = "not-a-key"
        };

        config.Normalize();

        Assert.Equal("", config.InputDeviceName);
        Assert.Equal("", config.OutputDeviceName);
        Assert.Equal(100, config.PlaybackVolume);
        Assert.True(config.WallOcclusion);
        Assert.Equal(10, config.ProximityRadius);
        Assert.Equal(0, config.VoiceSensitivity);
        Assert.Equal("V", config.PushToTalkKey);
        Assert.Equal("F7", config.MicrophoneToggleKey);
        Assert.Equal("not-a-key", config.CycleRadiusKey); // parsed with a fallback at runtime
    }

    [Fact]
    public void Normalize_MovesTestKeyWhenItCollidesWithMicrophoneToggle()
    {
        ModConfig config = new()
        {
            MicrophoneToggleKey = "F9",
            MicrophoneTestKey = "F9",
            CycleRadiusKey = "F10"
        };

        config.Normalize();

        Assert.Equal("F9", config.MicrophoneToggleKey);
        Assert.Equal("F11", config.MicrophoneTestKey);
    }

    [Fact]
    public void Normalize_KeepsTestKeyWhenItIsUnique()
    {
        ModConfig config = new();

        config.Normalize();

        Assert.Equal("F9", config.MicrophoneTestKey);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(15)]
    public void Normalize_MapsLegacyRadiiToDefault(int legacyRadius)
    {
        ModConfig config = new() { ProximityRadius = legacyRadius };

        config.Normalize();

        Assert.Equal(ModConstants.DefaultProximityRadius, config.ProximityRadius);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(15)]
    public void Deserialize_LegacyRadiusNormalizesToDefault(int legacyRadius)
    {
        string json = $$"""{"ProximityRadius": {{legacyRadius}}}""";

        ModConfig? config = JsonSerializer.Deserialize<ModConfig>(json, Options);

        Assert.NotNull(config);
        config!.Normalize();
        Assert.Equal(ModConstants.DefaultProximityRadius, config.ProximityRadius);
    }

    [Fact]
    public void Deserialize_Radius20RemainsValid()
    {
        const string json = """{"ProximityRadius": 20}""";

        ModConfig? config = JsonSerializer.Deserialize<ModConfig>(json, Options);

        Assert.NotNull(config);
        config!.Normalize();
        Assert.Equal(20, config.ProximityRadius);
    }

    [Fact]
    public void NextRadius_CyclesThroughOfferedSet()
    {
        Assert.Equal(10, ModConfig.NextRadius(3));
        Assert.Equal(20, ModConfig.NextRadius(10));
        Assert.Equal(3, ModConfig.NextRadius(20));
        Assert.Equal(10, ModConfig.NextRadius(999));
    }

    [Fact]
    public void OfferedSet_IsClosedAndAscending()
    {
        Assert.Equal(new[] { 3, 10, 20 }, ModConstants.ProximityRadii);
        for (int i = 1; i < ModConstants.ProximityRadii.Length; i++)
            Assert.True(ModConstants.ProximityRadii[i] > ModConstants.ProximityRadii[i - 1]);

        Assert.True(ModConfig.IsOfferedRadius(ModConstants.DefaultProximityRadius));
        Assert.False(ModConfig.IsOfferedRadius(ModConstants.DefaultProximityRadius + 1));
        Assert.False(ModConfig.IsOfferedRadius(6));
        Assert.False(ModConfig.IsOfferedRadius(15));
    }
}
