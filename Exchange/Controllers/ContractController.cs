using Exchange.ApiHelper;
using Exchange.AzureModel;
using Exchange.Global;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Exchange.Controllers
{
    public class ContractController : Controller
    {
        // GET: Contract
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult bankDetail()
        {
            string transID = Session["transID"].ToString();
            AzureConnection db = new AzureConnection();
            trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == transID).FirstOrDefault();
            ViewData["REFERENCE"] = trans_data.REFERENCE;
            return View();
        }
        public ActionResult sellTransPage(string id)
        {
            try
            {
                if (id == null)
                {
                    id = Session["transID"].ToString();
                }
                Session["transID"] = id;
                AzureConnection db = new AzureConnection();
                trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == id).FirstOrDefault();
                ViewData["btc_amount"] = trans_data.btc;
                ViewData["buyerName"] = trans_data.user_seller.username;
                ViewData["holdWallet"] = trans_data.holdWallet;
                ViewData["transID"] = trans_data.id;
                if (trans_data.sellTime == null)
                {
                    trans_data.sellTime = DateTime.UtcNow;
                    db.SaveChanges();
                    ViewData["sellTime"] = 0;
                    return View();
                }
                else
                {
                    TimeSpan diff = (TimeSpan)(DateTime.UtcNow - trans_data.sellTime);
                    ViewData["sellTime"] = diff.TotalSeconds;
                    return View();
                }
            }
            catch
            {
                return RedirectToAction("", "Home");
            }       
        }
        public ActionResult buyTransPage(string id)
        {
            try
            {
                if (id == null)
                {
                    id = Session["transID"].ToString();
                }
                Session["transID"] = id;
                AzureConnection db = new AzureConnection();
                trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == id).FirstOrDefault();
                ViewData["btc_amount"] = trans_data.btc;
                ViewData["sellerName"] = trans_data.user_seller.username;
                ViewData["cash"] = trans_data.cash;
                ViewData["walletID"] = trans_data.buyerWallet == null ? "" : trans_data.buyerWallet;
                ViewData["transID"] = trans_data.id;
                if (trans_data.buyTime == null)
                {
                    trans_data.buyTime = DateTime.UtcNow;
                    db.SaveChanges();
                    ViewData["buyTime"] = 0;
                    return View();
                }
                else
                {
                    TimeSpan diff = (TimeSpan)(DateTime.UtcNow - trans_data.buyTime);
                    ViewData["buyTime"] = diff.TotalSeconds;
                    return View();
                }
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public ActionResult saveBankDetail(string firstname,string lastname,string IBAN,string SWIFT)
        {
            try
            {
                string transID = Session["transID"].ToString();
                AzureConnection db = new AzureConnection();
                trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == transID).FirstOrDefault();
                trans_data.firstname_seller = firstname;
                trans_data.lastname_seller = lastname;
                trans_data.IBAN = IBAN;
                trans_data.SWIFT = SWIFT;
                db.SaveChanges();
                Session["next_url"] = "/Contract/sellTransPage";
                return RedirectToAction("googleAuthPage", "Verify");
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        [HttpGet]
        public ActionResult proceed(int userID,int transID)
        {
            try
            {
                AzureConnection db = new AzureConnection();
                trans_history data = db.trans_history.Where(a => a.id == transID).FirstOrDefault();
                Session["transID"] = transID;
                Session["btc_amount"] = data.btc;
                Session["cash"] = data.cash;
                Session["buyerEmail"] = data.user_buyer.email;
                Session["buyerName"] = data.user_buyer.username;
                Session["sellerEmail"] = data.user_seller.email;
                Session["sellerName"] = data.user_seller.username;
                Session["holdWallet"] = data.holdWallet;
                Session["user_type"] = userID == data.buyer ? "old_buyer": "old_seller";
                db.SaveChanges();
                if (Session["username"] == null)
                {
                    if (double.Parse(Session["cash"].ToString()) >= 1000 && data.user_seller.kycVerified == false)
                    {
                        Session["isCreater"] = "no";
                        Session["next_url"] = "/Verify/IdVerify";
                    }
                    else
                    {
                        Session["next_url"] = userID == data.buyer ? "/Contract/buyTransPage" : "/Contract/bankDetail";
                    }                         
                    return RedirectToAction("Login", "User_Login");
                }
                else
                {
                    if(double.Parse(Session["cash"].ToString()) >= 1000 && data.user_seller.kycVerified == false)
                    {
                        return RedirectToAction("IdVerify","Verify");
                    }
                    return userID == data.buyer ? RedirectToAction("buyTransPage") :RedirectToAction("bankDetail");
                }                    
                
            }
            catch
            {
                return RedirectToAction("","Home");
            }
        }
        public ActionResult recievedWire(int transID)
        {
            try
            {
                AzureConnection db = new AzureConnection();
                trans_history trans_data = db.trans_history.Where(a => a.id == transID).FirstOrDefault();
                if (trans_data.recieveCash == true)
                    return RedirectToAction("", "Home");
                Session["uploadType"] = "recieve";
                Session["qrkey"] = trans_data.user_seller.qrkey;
                Session["next_url"] = "/Contract/uploadReciptPage";
                return RedirectToAction("googleAuthPage", "Verify");
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public ActionResult sentWire(int transID)
        {
            try
            {
                AzureConnection db = new AzureConnection();
                trans_history trans_data = db.trans_history.Where(a => a.id == transID).FirstOrDefault();
                if (trans_data.sentCash == true)
                    return RedirectToAction("", "Home");
                Session["uploadType"] = "sent";
                Session["qrkey"] = trans_data.user_buyer.qrkey;
                Session["next_url"] = "/Contract/uploadReciptPage";
                return RedirectToAction("googleAuthPage", "Verify");
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public JsonResult saveWalletID(string walletID)
        {
            var isValid = BlockApi.validateAddress(walletID);
            if(isValid == false)
            {
                return Json("failed", JsonRequestBehavior.AllowGet);
            }
            string transID = Session["transID"].ToString();
            AzureConnection db = new AzureConnection();
            trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == transID).FirstOrDefault();
            trans_data.buyerWallet = walletID;
            Session["buyerWallet"] = walletID;
            db.SaveChanges();
            return Json("success", JsonRequestBehavior.AllowGet);
        }
        public JsonResult getBalance()
        {
            string cur_balance;
            string transID = Session["transID"].ToString();
            AzureConnection db = new AzureConnection();
            trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == transID).FirstOrDefault();
            cur_balance = ""+trans_data.holdAmount;
            return Json(cur_balance, JsonRequestBehavior.AllowGet);
        }
        public JsonResult isBTCHolded()
        {
            string transID = Session["transID"].ToString();
            AzureConnection db = new AzureConnection();
            trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == transID).FirstOrDefault();
            if(trans_data.holdBTC == true)
                return Json("success", JsonRequestBehavior.AllowGet);
            return Json("failed", JsonRequestBehavior.AllowGet);
        }

        public ActionResult uploadReciptPage()
        {
            return View();
        }
        public ActionResult uploadRecipt(HttpPostedFileBase file)
        {
            try
            {
                string transID = Session["transID"].ToString();
                if (file == null)
                {
                    return View("uploadReciptPage");
                }

                AzureConnection db = new AzureConnection();
                trans_history trans_data = db.trans_history.Where(a => a.id.ToString() == transID).FirstOrDefault();
                if (Session["uploadType"].ToString() == "recieve")
                {
                    trans_data.recieveCash = true;
                    trans_data.sentBTC = true;
                    trans_data.status = "complete";
                    db.SaveChanges();
                    ApiHelper.BlockApi.withDrawByAddress(trans_data.holdWallet, trans_data.buyerWallet, (trans_data.btc - 0.00011795).ToString());
                    string body;
                    body = "Dear " + trans_data.user_buyer.username + "<br/><br/>" + " We have just sent to your wallet, " + (trans_data.btc - 0.00011795) + " BTC (0.00011795 BTC is transaction fee). Transaction completed with user " + trans_data.user_seller.username + "<br/>Thank you";
                    Variables.sendEmail(trans_data.user_buyer.email, body);
                    string path = System.IO.Path.Combine(Server.MapPath("~/Uploads/"), "receive" + transID + ".png");
                    file.SaveAs(path);
                    return RedirectToAction("", "Home");
                }
                else if (Session["uploadType"].ToString() == "sent")
                {
                    trans_data.sentCash = true;
                    db.SaveChanges();
                    string body;
                    string link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "/Contract/recievedWire?transID=" + trans_data.id);
                    body = "Dear " + trans_data.user_seller.username + "<br/><br/>" +
                        "User " + trans_data.user_buyer.username + " has just notified us that the wire of $" + trans_data.cash + " has been sent to your bank account ending in " + trans_data.IBAN.Substring(trans_data.IBAN.Length - 4) + ". Please look your bank account as it will reach very soon. You need to confirm receipt by pressing the button below, after receiving the funds." + "<br/><br/><a href='" + link + "' class='btn btn-primary'>" + "I have receipt the wire.Thank you</a>"; ;
                    Variables.sendEmail(trans_data.user_seller.email, body);

                    string path = System.IO.Path.Combine(Server.MapPath("~/Uploads/"), "sent" + transID + ".png");
                    file.SaveAs(path);
                    return RedirectToAction("", "Home");
                }
                else
                {
                    return RedirectToAction("", "Home");
                }
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
    }
}