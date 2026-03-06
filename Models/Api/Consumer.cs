using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ValueCards.Models
{
  public class Consumer
  {
    [JsonProperty("@href")]
    public string Href { get; set; }
    public string Id { get; set; }
    [JsonProperty("contractid")]
    public string ContractId { get; set; }
    public string Name { get; set; }
    [JsonProperty("xValidFrom")]
    public string ValidFrom { get; set; }
    [JsonProperty("xValidUntil")]
    public string ValidUntil { get; set; }
    public string FilialId { get; set; }
  }

  public class Person
  {
    public string FirstName { get; set; }
    public string Surname { get; set; }
  }

  public class UsageProfile
  {
    [JsonProperty("@href")]
    public string Href { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
  }

  public class Identification
  {
    public int PtcptType { get; set; }
    public int Cardclass { get; set; }
    [JsonProperty("cardno")]
    public string CardNumber { get; set; }
    public int IdentificationType { get; set; }
    public string ValidFrom { get; set; }
    public string ValidUntil { get; set; }
    public UsageProfile UsageProfile { get; set; }
    public string Admission { get; set; }
    public int IgnorePresence { get; set; }
    public bool Present { get; set; }
    public int Status { get; set; }
    public int PtcpGrpNo { get; set; }
    [JsonProperty("ChrgOvdrftAcct")]
    public int ChargeOverDraftAccount { get; set; }
  }

  public class CustomerAttributes
  {
    public string ProductionDate { get; set; }
    public int ProductionCount { get; set; }
    public int FlatFeeFirstCharge { get; set; }
    public int FlatFeeLastCharge { get; set; }
    public int FlatFeeCalc { get; set; }
    public string FlatFeeCalcUntil { get; set; }
    public int IndividualInvoicing { get; set; }
    public int FlatRateAmt { get; set; }
    public string FlatFeeTax { get; set; }
    public string InvoiceType { get; set; }
  }

  public class ConsumerDetail
  {
    public Consumer Consumer { get; set; }
    public Person Person { get; set; }
    public string FirstName { get; set; }
    public string Surname { get; set; }
    public Identification Identification { get; set; }
    [JsonProperty("CustomerAtributes")]
    public CustomerAttributes CustomerAttributes { get; set; }
    public string DisplayText { get; set; }
    public int Limit { get; set; }
    public int Status { get; set; }
    public int Delete { get; set; }
    public int IgnorePresence { get; set; }
    public decimal? Balance { get; set; }

    public string CardNumber
    {
      get
      {
        return Identification?.CardNumber ?? $"{Consumer.ContractId},{Consumer.Id}";
      }
    }
  }

  public class ConsumerModel
  {
    [Editable(false)]
    public string Id { get; set; }
    [Editable(false)] 
    public string FirstName { get; set; }
    [Editable(false)] 
    public string Surname { get; set; }
    [Editable(false)] 
    public string ValidUntil { get; set; }
    [Editable(false)] 
    public string CardNumber { get; set; }
    [Editable(false)] 
    public decimal? Balance { get; set; }
  }

public class ValueCardModel
    {
        [Name ("* EPAN") ]
        public string EPAN { get; set; }
        [Name("* Device")]
        public string Device { get; set; }
        [Name("Amount")]
        public string Amount { get; set; }
        [Name("* Company")]
        public string Company { get; set; }
        [Name("* Participant")]
        public string Participant { get; set; }
        [Name("Tariff Start Time")]
        public DateTime TariffStartTime { get; set; }
        [Name("Tariff End Time")]
        public DateTime TariffEndTime { get; set; }


 }

  class ConsumerList
  {
    [JsonProperty("consumer")]
    public Consumer[] Consumers { get; set; }

  }

  class SingleConsumer
  {  
    public Consumer Consumer { get; set; }

    public Consumer[] Consumers => new Consumer[] { Consumer };
  }

  class ConsumerListResponse
  {    
    public ConsumerList Consumers { get; set; }
  }

  class ConsumerDetailResponse
  {
    public ConsumerDetail ConsumerDetail { get; set; }
  }

  public class ConsumerTopupModel : ConsumerModel
  {
    [Required]
    [Range(-1000, 1000)]
    public decimal Amount { get; set; }

    public ConsumerTopupModel()
    {
      Amount = 1m;
    }
  }

  class BalanceResponse
  {
    public string Epan { get; set; }
    public decimal? MoneyValue { get; set; }
  }
}
