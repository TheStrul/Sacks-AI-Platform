using System.Text.Json.Serialization;

namespace SacksAIPlatform.DataLayer.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Country
{
    Unknown = -1, // Unknown concentration
    None = 0,    // No Country specified
    Canada               = 1,
    France               = 2,
    Germany              = 3,
    Italy                = 4,
    Japan                = 5,
    Lebanon              = 6,
    Netherlands          = 7,
    Oman                 = 8,
    SaudiArabia          = 9,
    Spain                = 10,
    Sweden               = 11,
    Switzerland          = 12,
    Turkey               = 13,
    UnitedArabEmirates   = 14,
    UnitedKingdom        = 15,
    USA                  = 16,
    Australia           = 17,
}
