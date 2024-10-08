// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System;
namespace MyUplinkConnector.Client.Models
{
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    #pragma warning disable CS1591
    public partial class ZoneResponse : IParsable
    #pragma warning restore CS1591
    {
        /// <summary>If true, zone is command-only and no temperature readingsare available. Setpoint will be an unspecified value(not degrees).</summary>
        public bool? CommandOnly { get; set; }
        /// <summary>Indoor CO2 levels (0-40000ppm)</summary>
        public int? IndoorCo2 { get; set; }
        /// <summary>Indoor humidity (0-100%RH)</summary>
        public double? IndoorHumidity { get; set; }
        /// <summary>Specified temperature unit in haystack.If &quot;isCelsius&quot; is false then all temperatures are in Fahrenheit. Otherwies it is in Celsius.</summary>
        public bool? IsCelsius { get; set; }
        /// <summary>Sh-zone&apos;s current mode.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Mode { get; set; }
#nullable restore
#else
        public string Mode { get; set; }
#endif
        /// <summary>sh-zone&apos;s parameter name.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Name { get; set; }
#nullable restore
#else
        public string Name { get; set; }
#endif
        /// <summary>Target temperature in target unit.</summary>
        public double? Setpoint { get; set; }
        /// <summary>Cooling setpoint current value.</summary>
        public double? SetpointCool { get; set; }
        /// <summary>Heating setpoint current value.</summary>
        public double? SetpointHeat { get; set; }
        /// <summary>Maximum temperature range.</summary>
        public int? SetpointRangeMax { get; set; }
        /// <summary>Minimum temperature range.</summary>
        public int? SetpointRangeMin { get; set; }
        /// <summary>sh-zone&apos;s supported modes.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? SupportedModes { get; set; }
#nullable restore
#else
        public string SupportedModes { get; set; }
#endif
        /// <summary>Current temperature in target unit.</summary>
        public double? Temperature { get; set; }
        /// <summary>sh-zone&apos;s parameter id.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ZoneId { get; set; }
#nullable restore
#else
        public string ZoneId { get; set; }
#endif
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.Models.ZoneResponse"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::MyUplinkConnector.Client.Models.ZoneResponse CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::MyUplinkConnector.Client.Models.ZoneResponse();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "commandOnly", n => { CommandOnly = n.GetBoolValue(); } },
                { "indoorCo2", n => { IndoorCo2 = n.GetIntValue(); } },
                { "indoorHumidity", n => { IndoorHumidity = n.GetDoubleValue(); } },
                { "isCelsius", n => { IsCelsius = n.GetBoolValue(); } },
                { "mode", n => { Mode = n.GetStringValue(); } },
                { "name", n => { Name = n.GetStringValue(); } },
                { "setpoint", n => { Setpoint = n.GetDoubleValue(); } },
                { "setpointCool", n => { SetpointCool = n.GetDoubleValue(); } },
                { "setpointHeat", n => { SetpointHeat = n.GetDoubleValue(); } },
                { "setpointRangeMax", n => { SetpointRangeMax = n.GetIntValue(); } },
                { "setpointRangeMin", n => { SetpointRangeMin = n.GetIntValue(); } },
                { "supportedModes", n => { SupportedModes = n.GetStringValue(); } },
                { "temperature", n => { Temperature = n.GetDoubleValue(); } },
                { "zoneId", n => { ZoneId = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteBoolValue("commandOnly", CommandOnly);
            writer.WriteIntValue("indoorCo2", IndoorCo2);
            writer.WriteDoubleValue("indoorHumidity", IndoorHumidity);
            writer.WriteBoolValue("isCelsius", IsCelsius);
            writer.WriteStringValue("mode", Mode);
            writer.WriteStringValue("name", Name);
            writer.WriteDoubleValue("setpoint", Setpoint);
            writer.WriteDoubleValue("setpointCool", SetpointCool);
            writer.WriteDoubleValue("setpointHeat", SetpointHeat);
            writer.WriteIntValue("setpointRangeMax", SetpointRangeMax);
            writer.WriteIntValue("setpointRangeMin", SetpointRangeMin);
            writer.WriteStringValue("supportedModes", SupportedModes);
            writer.WriteDoubleValue("temperature", Temperature);
            writer.WriteStringValue("zoneId", ZoneId);
        }
    }
}
