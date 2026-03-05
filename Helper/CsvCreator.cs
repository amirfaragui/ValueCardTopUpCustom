using System;
using System.Collections.Generic;
using System.Text;
using ValueCards.Models;

public static class CsvCreator
{
 public static void WriteFailedTransactions(string filePath, List<ValueCardModel> failed)
 {
  var sb = new StringBuilder();

  // Header
  sb.AppendLine("Parcticipant,Amount");

  foreach (var item in failed)
  {
   sb.AppendLine($"{item.Participant},{item.Amount}");
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