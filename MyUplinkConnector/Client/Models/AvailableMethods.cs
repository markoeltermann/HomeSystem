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
    public partial class AvailableMethods : IParsable
    #pragma warning restore CS1591
    {
        /// <summary>The boostHotWater property</summary>
        public bool? BoostHotWater { get; set; }
        /// <summary>The boostVentilation property</summary>
        public bool? BoostVentilation { get; set; }
        /// <summary>The forcesync property</summary>
        public bool? Forcesync { get; set; }
        /// <summary>The forceUpdate property</summary>
        public bool? ForceUpdate { get; set; }
        /// <summary>The getGuideQuestion property</summary>
        public bool? GetGuideQuestion { get; set; }
        /// <summary>The getMenu property</summary>
        public bool? GetMenu { get; set; }
        /// <summary>The getMenuChain property</summary>
        public bool? GetMenuChain { get; set; }
        /// <summary>The getScheduleConfig property</summary>
        public bool? GetScheduleConfig { get; set; }
        /// <summary>The getScheduleModes property</summary>
        public bool? GetScheduleModes { get; set; }
        /// <summary>The getScheduleVacation property</summary>
        public bool? GetScheduleVacation { get; set; }
        /// <summary>The getScheduleWeekly property</summary>
        public bool? GetScheduleWeekly { get; set; }
        /// <summary>The getZones property</summary>
        public bool? GetZones { get; set; }
        /// <summary>The processIntent property</summary>
        public bool? ProcessIntent { get; set; }
        /// <summary>The reboot property</summary>
        public bool? Reboot { get; set; }
        /// <summary>The requestUpdate property</summary>
        public bool? RequestUpdate { get; set; }
        /// <summary>The resetAlarm property</summary>
        public bool? ResetAlarm { get; set; }
        /// <summary>The sendHaystack property</summary>
        public bool? SendHaystack { get; set; }
        /// <summary>The setAidMode property</summary>
        public bool? SetAidMode { get; set; }
        /// <summary>The setScheduleModes property</summary>
        public bool? SetScheduleModes { get; set; }
        /// <summary>The setScheduleOverride property</summary>
        public bool? SetScheduleOverride { get; set; }
        /// <summary>The setScheduleVacation property</summary>
        public bool? SetScheduleVacation { get; set; }
        /// <summary>The setScheduleWeekly property</summary>
        public bool? SetScheduleWeekly { get; set; }
        /// <summary>The setSmartMode property</summary>
        public bool? SetSmartMode { get; set; }
        /// <summary>The settings property</summary>
        public bool? Settings { get; set; }
        /// <summary>The setVentilationMode property</summary>
        public bool? SetVentilationMode { get; set; }
        /// <summary>The triggerEvent property</summary>
        public bool? TriggerEvent { get; set; }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.Models.AvailableMethods"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::MyUplinkConnector.Client.Models.AvailableMethods CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::MyUplinkConnector.Client.Models.AvailableMethods();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "boostHotWater", n => { BoostHotWater = n.GetBoolValue(); } },
                { "boostVentilation", n => { BoostVentilation = n.GetBoolValue(); } },
                { "forceUpdate", n => { ForceUpdate = n.GetBoolValue(); } },
                { "forcesync", n => { Forcesync = n.GetBoolValue(); } },
                { "getGuideQuestion", n => { GetGuideQuestion = n.GetBoolValue(); } },
                { "getMenu", n => { GetMenu = n.GetBoolValue(); } },
                { "getMenuChain", n => { GetMenuChain = n.GetBoolValue(); } },
                { "getScheduleConfig", n => { GetScheduleConfig = n.GetBoolValue(); } },
                { "getScheduleModes", n => { GetScheduleModes = n.GetBoolValue(); } },
                { "getScheduleVacation", n => { GetScheduleVacation = n.GetBoolValue(); } },
                { "getScheduleWeekly", n => { GetScheduleWeekly = n.GetBoolValue(); } },
                { "getZones", n => { GetZones = n.GetBoolValue(); } },
                { "processIntent", n => { ProcessIntent = n.GetBoolValue(); } },
                { "reboot", n => { Reboot = n.GetBoolValue(); } },
                { "requestUpdate", n => { RequestUpdate = n.GetBoolValue(); } },
                { "resetAlarm", n => { ResetAlarm = n.GetBoolValue(); } },
                { "sendHaystack", n => { SendHaystack = n.GetBoolValue(); } },
                { "setAidMode", n => { SetAidMode = n.GetBoolValue(); } },
                { "setScheduleModes", n => { SetScheduleModes = n.GetBoolValue(); } },
                { "setScheduleOverride", n => { SetScheduleOverride = n.GetBoolValue(); } },
                { "setScheduleVacation", n => { SetScheduleVacation = n.GetBoolValue(); } },
                { "setScheduleWeekly", n => { SetScheduleWeekly = n.GetBoolValue(); } },
                { "setSmartMode", n => { SetSmartMode = n.GetBoolValue(); } },
                { "setVentilationMode", n => { SetVentilationMode = n.GetBoolValue(); } },
                { "settings", n => { Settings = n.GetBoolValue(); } },
                { "triggerEvent", n => { TriggerEvent = n.GetBoolValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteBoolValue("boostHotWater", BoostHotWater);
            writer.WriteBoolValue("boostVentilation", BoostVentilation);
            writer.WriteBoolValue("forcesync", Forcesync);
            writer.WriteBoolValue("forceUpdate", ForceUpdate);
            writer.WriteBoolValue("getGuideQuestion", GetGuideQuestion);
            writer.WriteBoolValue("getMenu", GetMenu);
            writer.WriteBoolValue("getMenuChain", GetMenuChain);
            writer.WriteBoolValue("getScheduleConfig", GetScheduleConfig);
            writer.WriteBoolValue("getScheduleModes", GetScheduleModes);
            writer.WriteBoolValue("getScheduleVacation", GetScheduleVacation);
            writer.WriteBoolValue("getScheduleWeekly", GetScheduleWeekly);
            writer.WriteBoolValue("getZones", GetZones);
            writer.WriteBoolValue("processIntent", ProcessIntent);
            writer.WriteBoolValue("reboot", Reboot);
            writer.WriteBoolValue("requestUpdate", RequestUpdate);
            writer.WriteBoolValue("resetAlarm", ResetAlarm);
            writer.WriteBoolValue("sendHaystack", SendHaystack);
            writer.WriteBoolValue("setAidMode", SetAidMode);
            writer.WriteBoolValue("setScheduleModes", SetScheduleModes);
            writer.WriteBoolValue("setScheduleOverride", SetScheduleOverride);
            writer.WriteBoolValue("setScheduleVacation", SetScheduleVacation);
            writer.WriteBoolValue("setScheduleWeekly", SetScheduleWeekly);
            writer.WriteBoolValue("setSmartMode", SetSmartMode);
            writer.WriteBoolValue("settings", Settings);
            writer.WriteBoolValue("setVentilationMode", SetVentilationMode);
            writer.WriteBoolValue("triggerEvent", TriggerEvent);
        }
    }
}