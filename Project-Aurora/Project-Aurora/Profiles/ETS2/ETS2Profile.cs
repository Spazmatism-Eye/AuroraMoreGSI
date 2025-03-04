﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using AuroraRgb.EffectsEngine;
using AuroraRgb.Profiles.ETS2.Layers;
using AuroraRgb.Settings;
using AuroraRgb.Settings.Layers;
using AuroraRgb.Settings.Overrides.Logic;
using AuroraRgb.Settings.Overrides.Logic.Boolean;
using Common.Devices;

namespace AuroraRgb.Profiles.ETS2;

public enum ETS2_BeaconStyle {
    [Description("Simple Flashing")]
    Simple_Flash,
    [Description("Half Alternating")]
    Half_Alternating,
    [Description("Fancy Flashing")]
    Fancy_Flash,
    [Description("Side-to-Side")]
    Side_To_Side
}

public class ETS2Profile : ApplicationProfile {

    public override void Reset() {
        base.Reset();

        var brightColor = Color.FromArgb(0, 255, 255);
        var dimColor = Color.FromArgb(128, 0, 0, 255);
        var hazardColor = Color.FromArgb(255, 128, 0);

        Layers =
        [
            new Layer("Ignition Key", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = hazardColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.E })
                    }
                }, new OverrideLogicBuilder()
                    .SetDynamicBoolean("_Enabled", new BooleanNot(new BooleanGSIBoolean("Truck/engineOn")))
            ),

            // This layer will hide all the other buttons (giving the impression the dashboard is turned off) when the ignition isn't on.

            new Layer("Dashboard Off Mask", new SolidFillLayerHandler
                {
                    Properties = new SolidFillLayerHandlerProperties
                    {
                        _PrimaryColor = Color.Black,
                        _Sequence = new KeySequence(new FreeFormObject(0, 0, 830, 225))
                    }
                }, new OverrideLogicBuilder()
                    .SetDynamicBoolean("_Enabled", new BooleanNot(new BooleanGSIBoolean("Truck/electricOn")))
            ),
            new Layer("Throttle Key", new PercentGradientLayerHandler
            {
                Properties = new PercentGradientLayerHandlerProperties
                {
                    Gradient = new EffectBrush(new ColorSpectrum(Color.FromArgb(0, 255, 255), Color.FromArgb(0, 255, 0))),
                    _Sequence = new KeySequence(new[] { DeviceKeys.W }),
                    VariablePath = new VariablePath("Truck/gameThrottle"),
                    MaxVariablePath = new VariablePath("1"),
                    PercentType = PercentEffectType.AllAtOnce
                }
            }),
            new Layer("Brake Key", new PercentGradientLayerHandler
            {
                Properties = new PercentGradientLayerHandlerProperties
                {
                    Gradient = new EffectBrush(new ColorSpectrum(Color.FromArgb(0, 255, 255), Color.FromArgb(255, 0, 0))),
                    _Sequence = new KeySequence(new[] { DeviceKeys.S }),
                    VariablePath = new VariablePath("Truck/gameBrake"),
                    MaxVariablePath = new VariablePath("1"),
                    PercentType = PercentEffectType.AllAtOnce
                }
            }),
            new Layer("Bright Keys", new SolidColorLayerHandler
            {
                Properties = new LayerHandlerProperties
                {
                    _PrimaryColor = brightColor,
                    _Sequence = new KeySequence(new[]
                    {
                        DeviceKeys.A, DeviceKeys.D, // Steering
                        DeviceKeys.LEFT_SHIFT, DeviceKeys.LEFT_CONTROL, // Gear up/down
                        DeviceKeys.F, // Hazard light
                        DeviceKeys.H // Horn
                    })
                }
            }),
            new Layer("RPM", new PercentGradientLayerHandler
            {
                Properties = new PercentGradientLayerHandlerProperties
                {
                    Gradient = new EffectBrush().SetColorGradients(new SortedDictionary<double, Color>
                    {
                        { 0, Color.FromArgb(65, 255, 0) },
                        { 0.65f, Color.FromArgb(67, 255, 0) },
                        { 0.75f, Color.FromArgb(0, 100, 255) },
                        { 0.85f, Color.FromArgb(255, 0, 0) },
                        { 1, Color.FromArgb(255, 0, 0) },
                    }),
                    _Sequence = new KeySequence(new[]
                    {
                        DeviceKeys.ONE, DeviceKeys.TWO, DeviceKeys.THREE, DeviceKeys.FOUR, DeviceKeys.FIVE,
                        DeviceKeys.SIX, DeviceKeys.SEVEN, DeviceKeys.EIGHT, DeviceKeys.NINE, DeviceKeys.ZERO
                    }),
                    VariablePath = new VariablePath("Truck/engineRpm"),
                    MaxVariablePath = new VariablePath("Truck/engineRpmMax")
                }
            }),
            new Layer("Parking Brake Key", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = brightColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.SPACE })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(Color.Red, new BooleanGSIBoolean("Truck/parkBrakeOn"))
                    )
            ),
            new Layer("Headlights (High Beam)", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = dimColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.K })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(Color.White, new BooleanGSIBoolean("Truck/lightsBeamHighOn"))
                    )
            ),
            new Layer("Headlights", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = dimColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.L })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                            .AddEntry(Color.White,
                                new BooleanGSIBoolean("Truck/lightsBeamLowOn")) // When low beams are on, light up full white
                            .AddEntry(Color.FromArgb(128, 255, 255, 255),
                                new BooleanGSIBoolean("Truck/lightsParkingOn")) // When parking lights are on, light up dim white
                    )
            ),
            new Layer("Left Blinkers", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = hazardColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.F1, DeviceKeys.F2, DeviceKeys.F3, DeviceKeys.F4 })
                    }
                }, new OverrideLogicBuilder()
                    .SetDynamicBoolean("_Enabled", new BooleanGSIBoolean("Truck/blinkerLeftOn"))
            ),
            new Layer("Right Blinkers", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = hazardColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.F9, DeviceKeys.F10, DeviceKeys.F11, DeviceKeys.F12 })
                    }
                }, new OverrideLogicBuilder()
                    .SetDynamicBoolean("_Enabled", new BooleanGSIBoolean("Truck/blinkerRightOn"))
            ),
            new Layer("Left Blinker Button", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = dimColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.OPEN_BRACKET })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(hazardColor, new BooleanGSIBoolean("Truck/blinkerLeftActive"))
                    )
            ),
            new Layer("Left Blinker Button", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = dimColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.CLOSE_BRACKET })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(hazardColor, new BooleanGSIBoolean("Truck/blinkerRightActive"))
                    )
            ),
            new Layer("Beacon", new ETS2BeaconLayerHandler
            {
                Properties = new ETS2BeaconLayerProperties
                {
                    _BeaconStyle = ETS2_BeaconStyle.Fancy_Flash,
                    _PrimaryColor = hazardColor,
                    _Sequence = new KeySequence(new[] { DeviceKeys.F5, DeviceKeys.F6, DeviceKeys.F7, DeviceKeys.F8 })
                }
            }),
            new Layer("Beacon Button", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = dimColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.O })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(hazardColor, new BooleanGSIBoolean("Truck/lightsBeaconOn"))
                    )
            ),
            new Layer("Trailer Button", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = Color.FromArgb(128, 0, 0, 255),
                        _Sequence = new KeySequence(new[] { DeviceKeys.T })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(Color.Lime, new BooleanGSIBoolean("Trailer/attached"))
                    )
            ),
            new Layer("Cruise Control Button", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = dimColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.C })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(brightColor, new BooleanGSIBoolean("Truck/cruiseControlOn"))
                    )
            ),
            new Layer("Fuel", new PercentGradientLayerHandler
            {
                Properties = new PercentGradientLayerHandlerProperties
                {
                    Gradient = new EffectBrush().SetColorGradients(
                        new SortedDictionary<double, Color>
                        {
                            { 0f, Color.FromArgb(255, 0, 0) },
                            { 0.25f, Color.FromArgb(255, 0, 0) },
                            { 0.375f, Color.FromArgb(255, 255, 0) },
                            { 0.5f, Color.FromArgb(0, 255, 0) },
                            { 1f, Color.FromArgb(0, 255, 0) }
                        }),
                    _Sequence = new KeySequence(new[]
                    {
                        DeviceKeys.NUM_ONE, DeviceKeys.NUM_FOUR, DeviceKeys.NUM_SEVEN, DeviceKeys.NUM_LOCK
                    }),
                    VariablePath = new VariablePath("Truck/fuel"),
                    MaxVariablePath = new VariablePath("Truck/fuelCapacity")
                }
            }),
            new Layer("Air Pressure", new PercentGradientLayerHandler
            {
                Properties = new PercentGradientLayerHandlerProperties
                {
                    Gradient = new EffectBrush().SetColorGradients(
                        new SortedDictionary<double, Color>
                        {
                            { 0f, Color.FromArgb(255, 0, 0) },
                            { 0.25f, Color.FromArgb(255, 0, 0) },
                            { 0.375f, Color.FromArgb(255, 255, 0) },
                            { 0.5f, Color.FromArgb(0, 255, 0) },
                            { 1f, Color.FromArgb(0, 255, 0) }
                        }),
                    _Sequence = new KeySequence(new[]
                    {
                        DeviceKeys.NUM_THREE, DeviceKeys.NUM_SIX, DeviceKeys.NUM_NINE, DeviceKeys.NUM_ASTERISK
                    }),
                    VariablePath = new VariablePath("Truck/airPressure"),
                    MaxVariablePath = new VariablePath("Truck/airPressureMax")
                }
            }),
            new Layer("Wipers Button", new SolidColorLayerHandler
                {
                    Properties = new LayerHandlerProperties
                    {
                        _PrimaryColor = dimColor,
                        _Sequence = new KeySequence(new[] { DeviceKeys.P })
                    }
                }, new OverrideLogicBuilder()
                    .SetLookupTable("_PrimaryColor", new OverrideLookupTableBuilder<Color>()
                        .AddEntry(brightColor, new BooleanGSIBoolean("Truck/wipersOn"))
                    )
            )
        ];
    }
}