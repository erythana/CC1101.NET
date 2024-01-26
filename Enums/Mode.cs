using System.ComponentModel;

namespace CC1101.NET.Enums;

public enum Mode
{
    [Description("GFSK - 1.2kb")]
    GFSK__1_2kb,
    [Description("GFSK - 38.4kb")]
    GFSK__38_4kb,
    [Description("GFSK - 100kb")]
    GFSK__100kb,
    [Description("MSK - 250kb")]
    MSK__250kb,
    [Description("MSK - 500kb")]
    MSK__500kb,
    [Description("OOK - 4.8kb")]
    OOK__4_8kb
}