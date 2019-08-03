using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Exchange.ApiHelper
{
    public class requestData
    {
        public class Name {
            public string first_name;
            public string last_name;
        }
        public class Document
        {
            public string proof = "";
            public Name name;
            public string dob = "";
            public string document_number = "";
            public string expiry_date = "";
            public string issue_date = "";

            public Document()
            {
                name = new Name();
            }
        }
        public string reference;
        public string callback_url;
        public Document document;
        public requestData()
        {
            document = new Document();
        }
    }
   
    public class KYCApi
    {
        public static string verifyIDCard(string image,string firstname,string lastname)
        {
            /*
            string username = "test";
            string pwd = "CEE589B1-C438-4673-94F6-FB3E6D4634D2";

            string authInfo = username + ":" + pwd;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            request.Headers["Authorization"] = "Basic " + authInfo;
            */
            try
            {
                var client = new HttpClient();
                string uri = "https://shuftipro.com/api/";
                client.BaseAddress = new Uri(uri);
                int _TimeoutSec = 3600;
                client.Timeout = new TimeSpan(0, 0, _TimeoutSec);
                string _ContentType = "application/json";
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_ContentType));
                string _CredentialBase64 = "Basic a3Y3REMxbXFJRWpBVGhzQTR6U05sbDdzcnB4THZBN0JBT3c5UTlBZ3Y2" +
                    "bE5VTVhTSUIxNTQ1MTYzMDU0OiQyeSQxMCRMZjdtdXhoT3hHSU1LTFNRaGZTSGl" +
                    "PRlNvQzkxb0FqZndxTVFscWxFbE5LVEdXTERKZjdGSw==";
                client.DefaultRequestHeaders.Add("Authorization", _CredentialBase64);

                requestData reqData = new requestData();
                reqData.document.name.first_name = firstname;
                reqData.document.name.last_name = lastname;
                reqData.callback_url = "http://www.example.com/";
                reqData.document.proof = image;
                reqData.reference = Guid.NewGuid().ToString().Substring(0, 10) + DateTime.UtcNow.ToString();
                string Body = JsonConvert.SerializeObject(reqData);
                HttpContent _Body = new StringContent(Body);
                _Body.Headers.ContentType = new MediaTypeHeaderValue(_ContentType);
                HttpResponseMessage response;
                response = client.PostAsync(uri, _Body).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                return result;
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }        
    }
}