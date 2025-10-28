using System.Xml.Serialization;

namespace ValueReaderService.Services;

[XmlRoot(ElementName = "Publication_MarketDocument", Namespace = "urn:iec62325.351:tc57wg16:451-3:publicationdocument:7:3")]
public class PublicationMarketDocument
{
    [XmlElement("mRID")]
    public string MRid { get; set; } = "";

    [XmlElement("revisionNumber")]
    public int RevisionNumber { get; set; }

    [XmlElement("type")]
    public string Type { get; set; } = "";

    [XmlElement("sender_MarketParticipant.mRID")]
    public MridWithScheme SenderMarketParticipantMrid { get; set; } = new();

    [XmlElement("sender_MarketParticipant.marketRole.type")]
    public string SenderMarketParticipantMarketRoleType { get; set; } = "";

    [XmlElement("receiver_MarketParticipant.mRID")]
    public MridWithScheme ReceiverMarketParticipantMrid { get; set; } = new();

    [XmlElement("receiver_MarketParticipant.marketRole.type")]
    public string ReceiverMarketParticipantMarketRoleType { get; set; } = "";

    [XmlElement("createdDateTime", DataType = "dateTime")]
    public DateTime CreatedDateTime { get; set; }

    [XmlElement("period.timeInterval")]
    public TimeInterval PeriodTimeInterval { get; set; } = new();

    [XmlElement("TimeSeries")]
    public List<TimeSeries> TimeSeries { get; set; } = new();
}

public class MridWithScheme
{
    [XmlAttribute("codingScheme")]
    public string? CodingScheme { get; set; }

    [XmlText]
    public string Value { get; set; } = "";
}

public class TimeInterval
{
    [XmlElement("start")]
    public string? Start { get; set; }

    [XmlElement("end")]
    public string? End { get; set; }
}

public class TimeSeries
{
    [XmlElement("mRID")]
    public string MRid { get; set; } = "";

    [XmlElement("auction.type")]
    public string AuctionType { get; set; } = "";

    [XmlElement("businessType")]
    public string BusinessType { get; set; } = "";

    [XmlElement("in_Domain.mRID")]
    public MridWithScheme InDomainMrid { get; set; } = new();

    [XmlElement("out_Domain.mRID")]
    public MridWithScheme OutDomainMrid { get; set; } = new();

    [XmlElement("contract_MarketAgreement.type")]
    public string ContractMarketAgreementType { get; set; } = "";

    [XmlElement("currency_Unit.name")]
    public string CurrencyUnitName { get; set; } = "";

    [XmlElement("price_Measure_Unit.name")]
    public string PriceMeasureUnitName { get; set; } = "";

    [XmlElement("curveType")]
    public string CurveType { get; set; } = "";

    [XmlElement("Period")]
    public Period Period { get; set; } = new();
}

public class Period
{
    [XmlElement("timeInterval")]
    public TimeInterval TimeInterval { get; set; } = new();

    [XmlElement("resolution")]
    public string? Resolution { get; set; }

    [XmlElement("Point")]
    public List<Point> Points { get; set; } = new();
}

public class Point
{
    [XmlElement("position")]
    public int Position { get; set; }

    [XmlElement("price.amount")]
    public decimal PriceAmount { get; set; }
}