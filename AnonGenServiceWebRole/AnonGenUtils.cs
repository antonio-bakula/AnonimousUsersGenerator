using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Linq;
using System.IO;

namespace AnonGenServiceWebRole
{
  public class AnonGenUtils
  {
    private static readonly object _randomGeneratorLock = new object();
    private static Random _random = null;

    /// <summary>
    /// Simple random, usage:
    /// int random = CommonUtils.Random.Next(1, 10);
    /// </summary>
    public static Random Random
    {
      get
      {
        lock (_randomGeneratorLock)
        {
          if (_random == null)
          {
            byte[] randomBytes = new byte[4];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);
            int seed = BitConverter.ToInt32(randomBytes, 0);
            _random = new Random(seed);
          }
        }
        return _random;
      }
    }

    public static string SanatizeForUrl(string rawUrl)
    {
      string result = (!string.IsNullOrEmpty(rawUrl) ? rawUrl.Trim() : "").ToLower();
      while ((result.IndexOf(" ") > 0))
      {
        result = result.Replace(" ", "-");
      }

      while ((result.IndexOf("--") > 0))
      {
        result = result.Replace("--", "-");
      }

      result = result.Replace(" ", "-");

      result = Transliteration.ToLatin(result);

      string whiteList = "abcdefghijklmnopqrstuvwxyz-0123456789";
      string checkedResult = string.Empty;
      foreach (char urlChar in result.ToCharArray())
      {
        if (whiteList.Contains(urlChar.ToString()))
        {
          checkedResult += urlChar.ToString();
        }
      }

      if (checkedResult.StartsWith("-"))
      {
        checkedResult = checkedResult.Substring(1);
      }
      if (checkedResult.EndsWith("-"))
      {
        checkedResult = checkedResult.Substring(0, checkedResult.Length - 1);
      }
      return checkedResult;
    }
  }

  public static class Transliteration
  {
    public static List<TransliterationEntity> _languages;
    static Transliteration()
    {
      /// constructor treba uzeti XML i pretočiti u neki lakše upotrebljiviji oblik     

      var nodes = XDocument.Parse(GetTransliterationXml());

      List<TransliterationEntity> languages = new List<TransliterationEntity>();
      var languageNode = from node in nodes.Descendants("Language") select node;
      foreach (var element in languageNode)
      {
        var CultureNameNode = from node in element.Descendants("CultureName") select node.Value;
        var LangScriptNode = from node in element.Descendants("LangScript") select node.Value;
        var LatnScriptNode = from node in element.Descendants("LatnScript") select node.Value;

        TransliterationEntity language = new TransliterationEntity();
        language.CultureName = CultureNameNode.First();
        language.LangScript = LangScriptNode.First();
        language.LatnScript = LatnScriptNode.First();

        languages.Add(language);
      }

      _languages = languages;
    }

    public static string ToLatin(string text)
    {
      return ReplaceForAllLanguages(text);
    }

    private static string ReplaceForAllLanguages(string text)
    {
      foreach (TransliterationEntity language in _languages)
      {
        var oldChar = language.LangScript.Split(',');
        var newChar = language.LatnScript.Split(',');

        for (int xx = 0; xx < oldChar.Count(); xx++)
        {
          text = text.Replace(oldChar[xx].Trim(), newChar[xx].Trim());
        }
      }
      return text;
    }
    private static string GetTransliterationXml()
    {
      string appDataPath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
      string xmlFileName = Path.Combine(appDataPath, "Transliteration\\TransliterationTable.xml");
      return File.ReadAllText(xmlFileName);
    }
  }

  public class TransliterationEntity
  {
    public string CultureName;
    public string LangScript;
    public string LatnScript;

    public TransliterationEntity() { }
  }

}