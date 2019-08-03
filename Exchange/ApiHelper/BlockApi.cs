
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Exchange.ApiHelper
{
    public class BlockAddressModel
    {
        public class AddressData
        {
            public string network;
            public string address;
        }
        public string status;
        public AddressData data;
    }
    public class BlockValidateModel
    {
        public class ValidateData
        {
            public string network;
            public string address;
            public bool is_valid;
        }
        public string status;
        public ValidateData data;
    }
    public class BlockBalanceModel
    {
        public class BalanceData
        {
            public string network;
            public string available_balance;
            public string pending_received_balance;
        }
        public string status;
        public BalanceData data;
    }
    public class BlockPriceModel
    {
        public class PriceData
        {
            public string network;
            public PricePerExchange[] prices;

            public class PricePerExchange
            {
                public string price;
                public string price_base;
                public string exchange;
                public string time;
            };
        }
        public string status;
        public PriceData data;
    }
    public class BlockApi
    {
        public static string btc_key = "e638-540b-44d8-ce8d";
        public static string btctest_key = "2636-b2dd-db7b-d005";
        public static string getNewAddressWithRandom()
        {            
            using (var client = new HttpClient())
            {
                var uri = new Uri("https://block.io/api/v2/get_new_address/?api_key="+btc_key);

                HttpResponseMessage response =  client.GetAsync(uri).Result;
                string result =response.Content.ReadAsStringAsync().Result;
                BlockAddressModel adddress =  JsonConvert.DeserializeObject<BlockAddressModel>(result);
                return adddress.data.address;
            }
        }
        public static string getCurrentPrice()
        {        
            
            using (var client = new HttpClient())
            {
                var uri = new Uri("https://block.io/api/v2/get_current_price/?api_key="+btc_key);

                HttpResponseMessage response = client.GetAsync(uri).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                BlockPriceModel price = JsonConvert.DeserializeObject<BlockPriceModel>(result);
                double btc_usd = 0;
                int count = 0;
                foreach (var exchange in price.data.prices)
                {
                    if(exchange.price_base == "USD")
                    {
                        btc_usd += double.Parse(exchange.price);
                        count ++;
                    }
                }
                btc_usd = Math.Round(btc_usd / count * 1000) / 1000;                
                return btc_usd.ToString();
            }
        }
        public static string getBalanceByAddress(string address)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri("https://block.io/api/v2/get_address_balance/?api_key="+btc_key+"&addresses="+address);

                HttpResponseMessage response = client.GetAsync(uri).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                BlockBalanceModel balance = JsonConvert.DeserializeObject<BlockBalanceModel>(result);
                return balance.data.available_balance;
            }
        }
        public static void withDrawByAddress(string fromAddr,string toAddr,string amount)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri("https://block.io/api/v2/withdraw_from_addresses/?api_key="+btc_key+"&amounts="+amount+"&from_addresses="+fromAddr+"&to_addresses="+toAddr+ "&pin=coccole123");
                HttpResponseMessage response = client.GetAsync(uri).Result;
                string result = response.Content.ReadAsStringAsync().Result;
            }
        }
        public static bool validateAddress(string addr)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri("https://block.io/api/v2/is_valid_address/?api_key=" + btc_key + "&address=" + addr);
                HttpResponseMessage response = client.GetAsync(uri).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                BlockValidateModel validate = JsonConvert.DeserializeObject<BlockValidateModel>(result);
                if (validate.status == "success" && validate.data.is_valid)
                    return true;
                return false;
            }
        }
    }
}