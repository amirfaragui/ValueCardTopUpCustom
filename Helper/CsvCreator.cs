using System;
using System.Collections.Generic;
using System.Text;
using ValueCards.Models;

public static class CsvCreator
{
 public static void WriteTransactions(string filePath, List<ValueCardModel> failed)
 {
  var sb = new StringBuilder();

  // Header
  sb.AppendLine("* EPAN,* Device,* Company,* Participant,Amount, Tariff Start Time, Tariff End Time");

  foreach (var item in failed)
  {
       sb.AppendLine($"{item.EPAN},{item.Device},{item.Company},{item.Participant},{item.Amount}, {item.TariffStartTime}, {item.TariffEndTime}");
  }
  try
  {
      System.IO.File.WriteAllText(filePath, sb.ToString());
  }
  catch (Exception  ex) 
  {
   throw ex;
  }
 }
}