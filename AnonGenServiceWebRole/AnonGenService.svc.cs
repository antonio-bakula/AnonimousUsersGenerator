using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace AnonGenServiceWebRole
{
  public class AnonGenService : IAnonGenService
  {
    private static Dictionary<string, CultureSpecificGenerator> _generators = null;
    public List<string> GetSupportedCultures()
    {
      InitializeGenerators();
      return _generators.Select(kv => kv.Key).ToList();
    }

    public GenerateUsersResponse GenerateUsers(string culture, int count)
    {
      InitializeGenerators();

      var resp = CreateResponse(culture, count);
      try
      {
        if (resp.Success)
        {
          for (int i = 0; i < count; i++)
          {
            resp.Users.Add(GenerateUser(_generators[culture]));
          }
          resp.Messages.Add($"Sucsessfully generated {resp.Users.Count} users.");
        }
      }
      catch (Exception e)
      {
        resp.Success = false;
        resp.Messages.Add("Exception raised: " + e.Message);
      }
      return resp;
    }

    private GenerateUsersResponse CreateResponse(string culture, int count)
    {
      var resp = new GenerateUsersResponse();
      resp.Success = true;
      if (!_generators.ContainsKey(culture))
      {
        resp.Messages.Add("No generator for culture " + culture);
        resp.Success = false;
      }
      if (count < 1 || count > 100)
      {
        resp.Messages.Add("Invalid value for parameter 'count' must be in range 1-100.");
        resp.Success = false;
      }
      return resp;
    }

    private User GenerateUser(CultureSpecificGenerator gen)
    {
      var user = gen.CreateUser();
      var writeableProps = user.GetType().GetProperties().Where(p => p.CanWrite);
      var sortedProps = writeableProps.OrderBy(pi => gen.GetDependantPropertiesSorted().IndexOf(pi.Name));
      foreach (var pi in sortedProps)
      {
        if (gen.HaveFileStringList(pi.Name))
        {
          string value = gen.GetValueFromFileStringList(pi.Name);
          pi.SetValue(user, value);
        }
        else
        {
          var value = gen.ExecutePropertyMethodGenerator(pi.Name, user);
          if (value != null)
          {
            pi.SetValue(user, value);
          }
        }
      }
      return user;
    }

    private static readonly object _generatorsLock = new object ();

    private void InitializeGenerators()
    {
      lock (_generatorsLock)
      {
        if (_generators != null)
        {
          return;
        }
        _generators = new Dictionary<string, CultureSpecificGenerator>();
        var absGenType = typeof(CultureSpecificGenerator);
        var implementedGenerators = absGenType.Assembly.GetTypes().Where(t => t.IsSubclassOf(absGenType));
        foreach (var gentype in implementedGenerators)
        {
          var gen = (CultureSpecificGenerator)Activator.CreateInstance(gentype);
          _generators.Add(gen.GetCulture(), gen);
        }
      }
    }
  }

  public abstract class CultureSpecificGenerator
  {
    private static ConcurrentDictionary<string, List<string>> _fileStringLists = new ConcurrentDictionary<string, List<string>>();
    public abstract string GetCulture();

    public abstract List<string> GetDependantPropertiesSorted();

    public virtual User CreateUser()
    {
      return new User();
    }

    public string GetValueFromFileStringList(string propertyName)
    {
      InitializeFileStringListIfExists(propertyName);
      if (_fileStringLists.ContainsKey(propertyName))
      {
        var list = _fileStringLists[propertyName];
        int rand = AnonGenUtils.Random.Next(0, list.Count - 1);
        return list[rand];
      }
      return "";
    }

    public bool HaveFileStringList(string propertyName)
    {
      InitializeFileStringListIfExists(propertyName);
      return _fileStringLists.ContainsKey(propertyName);
    }
    
    public object ExecutePropertyMethodGenerator(string propertyName, User user)
    {
      var method = this.GetType().GetMethod(propertyName + "Generator");
      if (method != null)
      {
        return method.Invoke(this, new object[] { user });
      }
      return null;
    }

    private void InitializeFileStringListIfExists(string propertyName)
    {
      if (_fileStringLists.ContainsKey(propertyName))
      {
        return;
      }
      string appDataPath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
      string listFileName = Path.Combine(appDataPath, $"{GetCulture()}\\{propertyName}.txt");
      if (File.Exists(listFileName))
      {
        _fileStringLists.TryAdd(propertyName, File.ReadAllLines(listFileName).ToList());
      }
    }
  }

  public class CultureSpecificGeneratorHr : CultureSpecificGenerator
  {
    private static List<dynamic> _streets = null;

    public override string GetCulture()
    {
      return "hr-HR";
    }

    public override List<string> GetDependantPropertiesSorted()
    {
      return new List<string> { "EMail", "City" };
    }

    public string IdentificationNumberGenerator(User user)
    {
      string oib = "";
      int a = 10;
      for (int i = 0; i < 10; i++)
      {
        string digit = AnonGenUtils.Random.Next(0, 9).ToString();
        oib += digit;
        a = a + Convert.ToInt32(digit);
        a = a % 10;
        if (a == 0) a = 10;
        a *= 2;
        a = a % 11;
      }
      int check = 11 - a;
      if (check == 10) check = 0;
      return oib;
    }

    public string EMailGenerator(User user)
    {
      var mailHosts = new List<string> { "gmail.com", "yahoo.com", "outlook.com", "aol.com", "mail.com", "inbox.com" };
      var randomHost = mailHosts[AnonGenUtils.Random.Next(0, mailHosts.Count - 1)];
      return $"{AnonGenUtils.SanatizeForUrl(user.Firstname)}.{AnonGenUtils.SanatizeForUrl(user.Lastname)}@{randomHost}";
    }

    public string PhoneNumberGenerator(User user)
    {
      //there is no mobile network with 96 
      return "+385 96 " + AnonGenUtils.Random.Next(100, 999).ToString() + " " + AnonGenUtils.Random.Next(100, 999).ToString();
    }

    public string StreetNumberGenerator(User user)
    {
      string num = AnonGenUtils.Random.Next(1, 99).ToString();
      int lett = AnonGenUtils.Random.Next(80, 103);
      if (lett >= 97)
      {
        num += ((char)lett).ToString();
      }
      return num;
    }

    private static readonly object _streetsJsonLock = new object();
    public string StreetGenerator(User user)
    {
      lock (_streetsJsonLock)
      {
        if (_streets == null)
        {
          string appDataPath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
          string jsonFileName = Path.Combine(appDataPath, $"{GetCulture()}\\streets.json");
          string json = File.ReadAllText(jsonFileName);
          _streets = JArray.Parse(json).ToList<dynamic>();
        }
      }
      int rand = AnonGenUtils.Random.Next(0, _streets.Count - 1);
      var streetData = _streets[rand];
      user.PostalCode = streetData.PostalCode;
      user.City = streetData.City;
      return streetData.Street;
    }

    public string CountryGenerator(User user)
    {
      return "Hrvatska";
    }
  }
}
