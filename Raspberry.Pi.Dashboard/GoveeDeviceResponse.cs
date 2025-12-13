using System.Text.Json.Serialization;

namespace Raspberry.Pi.Dashboard;

public class GoveeDevicesResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<DeviceData> Data { get; set; } = [];
}


public class DeviceData
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("capabilities")]
    public List<Capability> Capabilities { get; set; } = [];
}

public class Capability
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("instance")]
    public string Instance { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public CapabilityParameters Parameters { get; set; } = new();
}

public class CapabilityParameters
{
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    // ENUM options
    [JsonPropertyName("options")]
    public List<EnumOption> Options { get; set; } = [];

    // INTEGER or STRUCT ranges
    [JsonPropertyName("range")]
    public Range Range { get; set; } = new();

    // STRUCT fields
    [JsonPropertyName("fields")]
    public List<StructField> Fields { get; set; } = [];

    // For unit fields (some objects include "unit")
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
}

public class EnumOption
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class Range
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("precision")]
    public int Precision { get; set; }
}

public class StructField
{
    [JsonPropertyName("fieldName")]
    public string FieldName { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    // For ENUM inside struct
    [JsonPropertyName("options")]
    public List<EnumOption> Options { get; set; } = [];

    // For INTEGER range inside struct
    [JsonPropertyName("range")]
    public Range Range { get; set; } = new();

    // For array size constraint
    [JsonPropertyName("size")]
    public SizeConstraint Size { get; set; } = new();

    // For element ranges of arrays
    [JsonPropertyName("elementRange")]
    public Range ElementRange { get; set; } = new();

    [JsonPropertyName("elementType")]
    public string ElementType { get; set; } = string.Empty;

    // Some fields also include "unit"
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
}

public class SizeConstraint
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }
}
