using ControllerWarcraft.Core.Input;
using Xunit;

namespace ControllerWarcraft.Tests.Input;

/// <summary>
/// Test del lettore XInput condiviso, limitati alla parte <b>pura</b> e testabile senza hardware:
/// la normalizzazione <see cref="ControllerReading.FromRaw"/> (raw byte/short → valori normalizzati)
/// e la lettura di disconnessione. Il P/Invoke <c>XInputGetState</c> non è esercitato qui (richiede
/// un controller reale) — coerente con il perimetro headless della suite.
/// </summary>
public class ControllerReadingTests
{
    private static XInputGamepadRaw Raw(
        ushort buttons = 0, byte lt = 0, byte rt = 0,
        short lx = 0, short ly = 0, short rx = 0, short ry = 0)
        => new()
        {
            Buttons = buttons,
            LeftTrigger = lt,
            RightTrigger = rt,
            ThumbLX = lx,
            ThumbLY = ly,
            ThumbRX = rx,
            ThumbRY = ry,
        };

    [Fact]
    public void Disconnected_IsNotConnected_AndAllZero()
    {
        var r = ControllerReading.Disconnected;
        Assert.False(r.Connected);
        Assert.Equal(0, r.LeftX);
        Assert.False(r.A);
        Assert.Equal(0, r.LeftTrigger);
    }

    [Fact]
    public void FromRaw_MarksConnected()
    {
        Assert.True(ControllerReading.FromRaw(Raw()).Connected);
    }

    [Fact]
    public void FromRaw_Axes_NormalizeToPlusMinusOne()
    {
        var r = ControllerReading.FromRaw(Raw(lx: 32767, ly: -32768, rx: 0, ry: 16384));
        Assert.Equal(1.0, r.LeftX, 3);
        Assert.Equal(-1.0, r.LeftY, 3);     // -32768/32767 clampato a -1
        Assert.Equal(0.0, r.RightX, 3);
        Assert.Equal(0.5, r.RightY, 2);     // 16384/32767 ≈ 0.5
    }

    [Fact]
    public void FromRaw_Triggers_NormalizeToZeroOne()
    {
        var r = ControllerReading.FromRaw(Raw(lt: 255, rt: 128));
        Assert.Equal(1.0, r.LeftTrigger, 3);
        Assert.Equal(128 / 255.0, r.RightTrigger, 5);
    }

    [Fact]
    public void FromRaw_Buttons_DecodeFromBitmask()
    {
        ushort mask = (ushort)(GamepadButton.A | GamepadButton.Start | GamepadButton.DPadLeft
                               | GamepadButton.LeftThumb | GamepadButton.RightShoulder);
        var r = ControllerReading.FromRaw(Raw(buttons: mask));

        Assert.True(r.A);
        Assert.True(r.Start);
        Assert.True(r.DPadLeft);
        Assert.True(r.LeftThumbClick);
        Assert.True(r.RightShoulder);

        // Non impostati.
        Assert.False(r.B);
        Assert.False(r.X);
        Assert.False(r.Y);
        Assert.False(r.Back);
        Assert.False(r.RightThumbClick);
        Assert.False(r.LeftShoulder);
        Assert.False(r.DPadRight);
    }

    [Fact]
    public void FromRaw_NoButtons_AllFalse()
    {
        var r = ControllerReading.FromRaw(Raw());
        Assert.False(r.A);
        Assert.False(r.B);
        Assert.False(r.X);
        Assert.False(r.Y);
        Assert.False(r.LeftShoulder);
        Assert.False(r.RightShoulder);
        Assert.False(r.DPadUp);
        Assert.False(r.Start);
        Assert.False(r.Back);
    }
}
