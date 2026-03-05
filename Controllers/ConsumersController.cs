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
     
      // Iterate and call API
     foreach (var record in records)
      {
          
          await Task.Delay(10);
       }

      // Clean up cache after processing
      _cache.Remove(VALUE_CARD_LIST);

       return Json(new { success = true, message = $"Successfully processed {records.Count} records." });
  }



  public IActionResult Topup(string id, [FromServices] IConsumerRepository repository)
      {
      if (id == null)
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
        return NotFound();

      return View(item);
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
        throw;
      }
    }
  }
}
