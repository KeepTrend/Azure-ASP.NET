using Exchange.AzureModel;
using Exchange.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Exchange.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            //    bool status = ApiHelper.KYCApi.verifyIDCard();
            try
            {
                AzureConnection db = new AzureConnection();
                var v = db.user_table.Where(a => a.listme == true).ToArray();
                ViewData["users"] = v;
                if (Session["isTransPending"] == null)
                    Session["isTransPending"] = "no";
                return View();
            }
            catch
            {                
                ViewData["users"] = null;
                if (Session["isTransPending"] == null)
                    Session["isTransPending"] = "no";
                return View();
            }
            
        }
        public ActionResult status()
        {
            try
            {
                AzureConnection db = new AzureConnection();
                string email = Session["email"].ToString();
                var v = db.trans_history.Where(a => (a.user_buyer.email == email || a.user_seller.email == email) && a.status == "pending").ToArray();
                ViewData["trans"] = v;
                return View();
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public JsonResult secure_trans(string amount,string unit,string name_email)
        {
            /*   var google_res = Request["g-recaptcha-response"];
               string secretKey = WebConfigurationManager.AppSettings.Get("privateRecaptchaKey");
               var client = new WebClient();
               var result = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secretKey, google_res));
               var obj = JObject.Parse(result);
               var status = (bool)obj.SelectToken("success");*/
            JObject result = new JObject();
            if (amount.Length==0|| name_email.Length == 0)
            {
                result["state"] = false;
                result["message"] = "Input Correctly";
                return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
            }
            AzureConnection db = new AzureConnection();
            var client = db.user_table.Where(x => x.email == name_email || x.username == name_email).FirstOrDefault();
            if(client == null)
            {
                result["state"] = false;
                result["message"] = "Please invite " + name_email + " to sign up in order to start transacting with you";
                return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
            }
            if(unit == "BTC")
            {
                Session["btc_amount"] = amount;
                Session["sellerEmail"] = client.email;
                Session["cash"] = Math.Round(double.Parse(amount) * double.Parse(Variables.getInstance().cur_price)).ToString();
            }
            else
            {
                Session["btc_amount"] = Math.Round(double.Parse(amount) / double.Parse(Variables.getInstance().cur_price), 6, MidpointRounding.AwayFromZero).ToString();
                Session["buyerEmail"] = client.email;
                Session["cash"] = amount;
            }
            if(Session["username"] == null)
            {
                Session["user_type"] = unit == "BTC" ? "new_buyer":"new_seller";
                Session["qrScanned"] = "false";
                Session["phone_next_url"] = "/Verify/passportID";
            }
            else
            {
                Session["user_type"] = unit == "BTC" ? "old_buyer":"old_seller";
                Session["qrScanned"] = "true";
                string username = Session["username"].ToString();
                user_table data = db.user_table.Where(a => a.username == username).FirstOrDefault();
                if (double.Parse(Session["cash"].ToString()) >= 1000 && data.kycVerified == false)
                {
                    Session["isCreater"] = "yes";
                    Session["phone_next_url"] = "/Verify/IdVerify";
                }
                else
                {
                    Session["phone_next_url"] = "/Verify/sendEmailToOpp";
                    Session["next_url"] = unit == "BTC" ? "/Contract/buyTransPage": "/Contract/bankDetail";
                }
            }
            result["state"] = true;
            result["message"] = "";
            return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
        }
        public JsonResult getCurrentPrice()
        {
            return Json(Variables.getInstance().cur_price, JsonRequestBehavior.AllowGet);
        }
        public ActionResult user_info(string username)
        {
            AzureConnection db = new AzureConnection();
            user_table user_data = db.user_table.Where(a => a.username == username).FirstOrDefault();
            var trans_data  = db.trans_history.Where(a => (a.seller == user_data.id||a.buyer == user_data.id)).ToArray();
            ViewData["username"] = username;
            ViewData["star"] = 5;
            ViewData["rated_users"] = 30;
            ViewData["positive"] = 100;
            ViewData["no_trans"] = trans_data.Length;
            ViewData["btc"] = trans_data.Sum(x => x.btc) / trans_data.Length;
            if (ViewData["btc"].ToString() == "NaN")
                ViewData["btc"] = 0;
            ViewData["cash"] = trans_data.Sum(x => x.cash) / trans_data.Length;
            if (ViewData["cash"].ToString() == "NaN")
                ViewData["cash"] = 0;
            ViewData["country"] = (user_data.country == "gb" ? "UK": user_data.country).ToUpper();
            ViewData["city"] = user_data.city;
            ViewData["avg"] = 2;
            return View();
        }
    }
}