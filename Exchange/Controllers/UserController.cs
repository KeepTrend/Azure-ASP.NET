using Exchange.AzureModel;
using Exchange.Global;
using Exchange.Google.Authenticator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Exchange.Controllers
{
    public class UserController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("mylistings", "User");
        }
        [HttpGet]
        public ActionResult mylistings()
        {
            try
            {
                string email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table v = db.user_table.Where(a => a.email == email).FirstOrDefault();
                ViewBag.Action = "mylistings";
                return View(v);
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public ActionResult saveMylisting()
        {
            try
            {
                string email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table v = db.user_table.Where(a => a.email == email).FirstOrDefault();
                v.check_btc = Request.Form["check_btc"] == null ? false : true;
                v.btc_cash = Request.Form["btc_cash"] == null ? false : true;
                v.btc_cash_com = double.Parse(Request.Form["btc_cash_com"]);
                v.btc_wire = Request.Form["btc_wire"] == null ? false : true;
                v.btc_wire_com = double.Parse(Request.Form["btc_wire_com"]);
                v.check_eth = Request.Form["check_eth"] == null ? false : true;
                v.eth_cash = Request.Form["eth_cash"] == null ? false : true;
                v.eth_cash_com = double.Parse(Request.Form["eth_cash_com"]);
                v.eth_wire = Request.Form["eth_wire"] == null ? false : true;
                v.eth_wire_com = double.Parse(Request.Form["eth_wire_com"]);
                v.from_cash = double.Parse(Request.Form["from_cash"]);
                v.to_cash = double.Parse(Request.Form["to_cash"]);
                v.city = Request.Form["city"];
                v.country = Request.Form["country"];
                v.listme = Request.Form["listme"] == null ? false : true;
                db.SaveChanges();
                return RedirectToAction("mylistings");
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        [HttpGet]
        public ActionResult affiliate_links()
        {
            ViewBag.Action = "affiliate_links";
            return View();
        }
        [HttpGet]
        public ActionResult affiliate_trans()
        {
            ViewBag.Action = "affiliate_trans";
            return View();
        }
        [HttpGet]
        public ActionResult mybank()
        {
            try
            {
                string email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table v = db.user_table.Where(a => a.email == email).FirstOrDefault();
                ViewBag.Action = "mybank";
                return View(v);
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public ActionResult saveBank(string beneficiery,string bank,string IBAN, string SWIFT)
        {
            try
            {
                string email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table v = db.user_table.Where(a => a.email == email).FirstOrDefault();
                v.beneficiery = beneficiery;
                v.bank = bank;
                v.IBAN = IBAN;
                v.SWIFT = SWIFT;
                db.SaveChanges();
                return RedirectToAction("mybank");
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        [HttpGet]
        public ActionResult google_auth()
        {
            ViewData["step"] = "first";
            ViewBag.Action = "google_auth";
            return View();
        }
        public ActionResult sendSmsCode()
        {
            try
            {
                string email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table v = db.user_table.Where(a => a.email == email).FirstOrDefault();
                try
                {
                    string code = Variables.sendSms(v.phone);
                    Session["phoneCode"] = code;
                    ViewData["step"] = "second";
                    ViewBag.Action = "google_auth";
                    return View("google_auth");
                }
                catch
                {
                    ViewBag.Message = "Phone number isn't correct.";
                    ViewData["step"] = "first";
                    ViewBag.Action = "google_auth";
                    return View("google_auth");
                }
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public ActionResult validateSms(string phoneCode)
        {
            try
            {
                if (Session["phoneCode"].ToString() == phoneCode)
                {
                    ViewData["step"] = "third";
                    ViewBag.Action = "google_auth";
                    AzureConnection db = new AzureConnection();
                    string email = Session["email"].ToString();
                    var user_data = db.user_table.Where(a => a.email == email).FirstOrDefault();
                    user_data.qrScanned = true;
                    db.SaveChanges();
                    TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                    SetupCode setupInfo = tfa.GenerateSetupCode("TRUSTBTC", Session["email"].ToString(), Session["qrkey"].ToString(), 300, 300);
                    ViewData["qrUrl"] = setupInfo.QrCodeSetupImageUrl;
                    return View("google_auth");
                }
                else
                {
                    ViewData["step"] = "second";
                    ViewBag.Action = "google_auth";
                    ViewBag.Message = "Code isn't correct.";
                    return View("google_auth");
                }
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        [HttpGet]
        public ActionResult pers_info()
        {
            try
            {
                string email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table v = db.user_table.Where(a => a.email == email).FirstOrDefault();
                ViewBag.Action = "pers_info";
                return View(v);
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public ActionResult savePersInfo(string firstname,string lastname,string username,string email,string phone)
        {
            try
            {
                string cur_email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table v = db.user_table.Where(a => a.email == cur_email).FirstOrDefault();
                if (v.email == email && v.firstname == firstname && v.username == username)
                {
                    return RedirectToAction("pers_info");
                }
                else
                {
                    user_table existing = db.user_table.Where(a => a.email != v.email && (a.email == email || a.username == username)).FirstOrDefault();
                    v.firstname = firstname;
                    v.lastname = lastname;
                    v.username = username;
                    v.email = email;
//                    v.phone = phone;
                    if (existing != null)
                    {
                        ViewBag.Message = "Username or Email already exists.";
                        ViewBag.Action = "pers_info";
                        return View("pers_info", v);
                    }                    
                    db.SaveChanges();
                    Session["username"] = username;
                    Session["email"] = email;
                    return RedirectToAction("pers_info");
                }
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        [HttpGet]
        public ActionResult mytrans()
        {
            try
            {
                AzureConnection db = new AzureConnection();
                string email = Session["email"].ToString();
                var v = db.trans_history.Where(a => a.user_buyer.email == email || a.user_seller.email == email).ToArray();
                ViewData["trans"] = v;
                ViewBag.Action = "mytrans";
                return View();
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
    }
}