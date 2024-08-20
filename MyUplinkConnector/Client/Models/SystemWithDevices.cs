// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System;
namespace MyUplinkConnector.Client.Models
{
    /// <summary>
    /// Group.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class SystemWithDevices : IParsable
    {
        /// <summary>System country.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Country { get; set; }
#nullable restore
#else
        public string Country { get; set; }
#endif
        /// <summary>List of devices.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<global::MyUplinkConnector.Client.Models.SystemDevice>? Devices { get; set; }
#nullable restore
#else
        public List<global::MyUplinkConnector.Client.Models.SystemDevice> Devices { get; set; }
#endif
        /// <summary>Whether system currently has an active alarm.</summary>
        public bool? HasAlarm { get; set; }
        /// <summary>System name.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Name { get; set; }
#nullable restore
#else
        public string Name { get; set; }
#endif
        /// <summary>The securityLevel property</summary>
        public global::MyUplinkConnector.Client.Models.SecurityLevel? SecurityLevel { get; set; }
        /// <summary>System identifier.</summary>
        public Guid? SystemId { get; set; }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.Models.SystemWithDevices"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::MyUplinkConnector.Client.Models.SystemWithDevices CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::MyUplinkConnector.Client.Models.SystemWithDevices();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "country", n => { Country = n.GetStringValue(); } },
                { "devices", n => { Devices = n.GetCollectionOfObjectValues<global::MyUplinkConnector.Client.Models.SystemDevice>(global::MyUplinkConnector.Client.Models.SystemDevice.CreateFromDiscriminatorValue)?.AsList(); } },
                { "hasAlarm", n => { HasAlarm = n.GetBoolValue(); } },
                { "name", n => { Name = n.GetStringValue(); } },
                { "securityLevel", n => { SecurityLevel = n.GetEnumValue<global::MyUplinkConnector.Client.Models.SecurityLevel>(); } },
                { "systemId", n => { SystemId = n.GetGuidValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("country", Country);
            writer.WriteCollectionOfObjectValues<global::MyUplinkConnector.Client.Models.SystemDevice>("devices", Devices);
            writer.WriteBoolValue("hasAlarm", HasAlarm);
            writer.WriteStringValue("name", Name);
            writer.WriteEnumValue<global::MyUplinkConnector.Client.Models.SecurityLevel>("securityLevel", SecurityLevel);
            writer.WriteGuidValue("systemId", SystemId);
        }
    }
}