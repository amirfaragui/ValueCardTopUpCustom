using CsvHelper;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ValueCards.Hubs;
using ValueCards.Models;
using ValueCards.Services;

namespace ValueCards.Controllers
{
  [Authorize(Policy = CookieAuthenticationDefaults.AuthenticationScheme)]
  public class ConsumersController : Controller
  {
    private readonly IConsumerService _consumerService;
    private readonly ILogger<ConsumersController> _logger;
    private readonly IApiClient _apiClient;
    private readonly IMemoryCache _cache;
    private string VALUE_CARD_LIST = "valueCardList";
   

  public ConsumersController(IConsumerService consumerService,
                               
                               ILogger<ConsumersController> logger, IMemoryCache cache)
    {
      _consumerService = consumerService ?? throw new ArgumentNullException(nameof(consumerService));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _cache = cache ?? throw new ArgumentNullException(nameof(cache));
      
  }

    public IActionResult Index()
    {
      return View();
    }

    public async Task<IActionResult> Read([DataSourceRequest]DataSourceRequest request)
    {
      return Json(_consumerService.Read(request));
    }


    [HttpPost]
    public IActionResult UploadExcel(IFormFile file)
    {
            _cache.Remove(VALUE_CARD_LIST);
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // Maps CSV headers to Value card model properties automatically
                    var records = csv.GetRecords<ValueCardModel>().Where(c=> c.Company.Contains("70 -")).ToList();
                    // save to cache to retrieve later
                    _cache.Set(VALUE_CARD_LIST, records, TimeSpan.FromMinutes(30));

                    return Json(new { success = true, data = records });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
    }

  [HttpPost]
  public async Task<IActionResult> ProcessUploadedData()
  {
   
       if (!_cache.TryGetValue(VALUE_CARD_LIST, out List<ValueCardModel> records))
      {
         return Json(new { success = false, message = "Session expired. Please upload again." });
      }
        var failedTransactions = new List<ValueCardModel>();
        var successfulTransactions = new List<ValueCardModel>();

       // Iterate and call API
       foreach (var record in records)
      {
           ConsumerTopupModel topupModel = new ConsumerTopupModel();
           

           topupModel.Id = "70," + record.Participant.Substring(0, record.Participant.IndexOf("-"));
           var amount = Decimal.Parse(record.Amount.Replace("$", ""));
           if (amount == 0)
           {
               //calcuate charge manually  
               TimeSpan duration = record.TariffEndTime - record.TariffStartTime;
               if ((int)duration.TotalMinutes > 20)
                   amount = (decimal)CalculateRate(record);

           }
           // add the amount back to the record for saving
           record.Amount = "$" + amount.ToString();
           topupModel.Amount = -Math.Abs(amount);
           
          try
          {
            Transaction result = await _consumerService.PostPaymentAsync(topupModel);
            if (result != null && !String.IsNullOrEmpty(result.Id))
            {
                  successfulTransactions.Add(record);
            }
          }
          catch(Exception e)
          {
            _logger.LogError($"Error calling API: {e}");
             failedTransactions.Add(record);
          }
      }

        // Clean up cache after processing
         _cache.Remove(VALUE_CARD_LIST);
         //save failed transactions
       var directoryPath = @"C:/ValueCardReports";
       if (failedTransactions.Any())
       {
        var fileName = $"FailedTransactions_{DateTime.Now:yyyyMMddHHmmss}.csv";
        var path = Path.Combine(directoryPath, fileName);

          CsvCreator.WriteTransactions(path, failedTransactions);
        }
      //save successful transactions
      if (successfulTransactions.Any())
     {
        var fileName = $"SuccessfulTransactions_{DateTime.Now:yyyyMMddHHmmss}.csv";
        var path = Path.Combine(directoryPath, fileName);
        CsvCreator.WriteTransactions(path, successfulTransactions);
      }
       return Json(new { success = true, message = $"Processed {records.Count} records. [{successfulTransactions.Count}] successful transactions , [{failedTransactions.Count}] Failed Transactions" });
  }


  private double CalculateRate(ValueCardModel record)
  {
   double dayHours = 0;
   double nightHours = 0;
   double weekendHours = 0;

   TimeSpan dayStart = new TimeSpan(8, 30, 0);  // 08:30
   TimeSpan dayEnd = new TimeSpan(18, 0, 0);    // 18:00

   DateTime current = record.TariffStartTime;

   while (current < record.TariffEndTime)
   {
    DateTime next = current.AddMinutes(30);
    if (next > record.TariffEndTime)
     next = record.TariffEndTime;

    double hours = (next - current).TotalHours;

    if (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
    {
     weekendHours += hours;
    }
    else
    {
     TimeSpan time = current.TimeOfDay;

     if (time >= dayStart && time < dayEnd)
      dayHours += hours;
     else
      nightHours += hours;
    }

    current = next;
   }

   // Weekday charges
   double weekdayHours = dayHours + nightHours;
   double weekdayCharge = weekdayHours * 5;

   // Cap weekday charge to $16 per 24 hours
   if (weekdayHours >= 24)
    weekdayCharge = 16;
   else
    weekdayCharge = Math.Min(weekdayCharge, 16);

   // Weekend charges: $5/hour capped at $9 per 24h
   double weekendCharge = weekendHours * 5;
   if (weekendHours >= 24)
    weekendCharge = 9;
   else
    weekendCharge = Math.Min(weekendCharge, 9);

   return weekdayCharge + weekendCharge;

   
  }

  public IActionResult Topup(string id, [FromServices] IConsumerRepository repository)
      {
     /* if (id == null)
        throw new ArgumentNullException(nameof(id));

      var parts = id.Split(',');
      if (parts.Length != 2)
        return BadRequest();

      var item = repository.Consumers
        .Where(i => i.Consumer.ContractId == parts[0] && i.Consumer.Id == parts[1])
        .Select(i => new ConsumerTopupModel
        {
          Id = $"{i.Consumer.ContractId},{i.Consumer.Id}",
          FirstName = i.Person?.FirstName ?? i.FirstName,
          Surname = i.Person?.Surname ?? i.Surname,
          ValidUntil = i.Consumer.ValidUntil,
          CardNumber = i.Identification.CardNumber ?? $"{i.Consumer.ContractId},{i.Consumer.Id}",
          Balance = i.Balance,
        })
        .FirstOrDefault();

      if (item == null)
        return NotFound();*/

      return View();
    }

    [HttpPost]
    public async Task<IActionResult> Topup(string id, ConsumerTopupModel model)
    {
      if(model == null)
        throw new ArgumentNullException(nameof(model));

      if (string.IsNullOrEmpty(id))
        return BadRequest();

      var parts = id.Split(',');
      if (parts.Length != 2)
        return BadRequest();

      try
      {
        model.Id = id;
        await _consumerService.PostPaymentAsync(model);
        return Created("", model);
      }
      catch (ApiErrorException aex)
      {
        return new ObjectResult(new { message = aex.Message }) { StatusCode = (int)aex.StutusCode };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex.ToString());
         return new ObjectResult(new { message = ex.Message }) { StatusCode = 500 };
      }
    }
  }
}
