using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Http.Cors;

namespace AnonGenServiceWebRole
{
	[ServiceContract]
	public interface IAnonGenService
	{
		[OperationContract]
		[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "GetSupportedCultures")]
		List<string> GetSupportedCultures();

		[OperationContract]
		[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "GenerateUsers/{culture}/?count={count}")]
		[EnableCors(origins: "*", headers: "*", methods: "*")]
		GenerateUsersResponse GenerateUsers(string culture, int count);
	}

	[DataContract]
	public class User
	{
		[DataMember]
		public string IdentificationNumber { get; set; }
		[DataMember]
		public string IdentificationNumber2 { get; set; }
		[DataMember]
		public string Firstname { get; set; }
		[DataMember]
		public string Lastname { get; set; }
		[DataMember]
		public string EMail { get; set; }
		[DataMember]
		public string PhoneNumber { get; set; }
		[DataMember]
		public string Street { get; set; }
		[DataMember]
		public string StreetNumber { get; set; }
		[DataMember]
		public string PostalCode { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string Country { get; set; }
	}

	[DataContract]
	public class GenerateUsersResponse
	{
		[DataMember]
		public bool Success { get; set; }
		[DataMember]
		public List<string> Messages { get; set; } = new List<string>();
		[DataMember]
		public List<User> Users { get; set; } = new List<User>();
	}
}
