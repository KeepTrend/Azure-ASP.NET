
using Exchange.ApiHelper;
using Exchange.AzureModel;
using Exchange.Global;
using Exchange.Google.Authenticator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Helpers;
using System.Web.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Exchange.Controllers
{
    public class VerifyController : Controller
    {
        // GET: Verify
        
        public ActionResult Index()
        {            
            return RedirectToAction("phoneVerify");
        }
        public JsonResult SendMessage(string phone,string countryCode)
        {
            AzureConnection db = new AzureConnection();
            var data = db.user_table.Where(x => x.phone == phone).FirstOrDefault();
            if (data != null)
            {
                return Json("exist", JsonRequestBehavior.AllowGet);
            }
            try
            {
                // string code = Variables.sendSms(phone);
                string code = "111111";
                Session["phoneNumber"] = phone;
                Session["phoneCode"] = code;
                Session["countryCode"] = countryCode;
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                ViewBag.Message = "Phone number isn't correct.";
                return Json("failed", JsonRequestBehavior.AllowGet);
            }            
        }
        [HttpPost]
        public ActionResult checkPhoneCode(string phoneCode)
        {
            try
            {
                if (Session["phoneCode"].ToString() == phoneCode)
                {
                    return Redirect(Session["phone_next_url"].ToString());
                }
                else
                {
                    ViewBag.Message = "Code isn't correct.";
                    return View("codePage");
                }
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        [HttpGet]
        public ActionResult phoneVerify()
        {
            try
            {
                if (Session["username"] != null)
                {
                    AzureConnection db = new AzureConnection();
                    string username = Session["username"].ToString();
                    var data = db.user_table.Where(x => x.username == username).FirstOrDefault();
                    try
                    {
                        string code = Variables.sendSms(data.phone);
                        Session["phoneCode"] = code;
                        return RedirectToAction("codePage");
                    }
                    catch
                    {
                        ViewBag.Message = "Phone number isn't correct.";
                        return View();
                    }
                }
                return View();
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        [HttpGet]
        public ActionResult codePage()
        {
            return View();
        }
        [HttpGet]
        public ActionResult passportID()
        {
            try
            {
                ViewData["phoneNumber"] = Session["phoneNumber"].ToString();
                return View();
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public ActionResult IdVerify()
        {
            return View();
        }
        public JsonResult uploadID(HttpPostedFileBase file)
        {
            try
            {
                JObject result = new JObject();
                AzureConnection db = new AzureConnection();
                shufti_log id_log = new shufti_log();
                id_log.time = DateTime.UtcNow;
                string email = Session["email"].ToString();
                var user_data = db.user_table.Where(a => a.email == email).FirstOrDefault();
                var memStream = new MemoryStream();
                file.InputStream.CopyTo(memStream);
                byte[] imageBytes = memStream.ToArray();
                var base64String = Convert.ToBase64String(imageBytes);
                string reponse = ApiHelper.KYCApi.verifyIDCard(base64String,user_data.firstname,user_data.lastname);
                var reponseData = JObject.Parse(reponse);
                if (reponseData["event"].ToString() == "verification.declined")
                {
                    id_log.status = "verification.declined";
                    id_log.declined_reason = reponseData["declined_reason"].ToString();
                    db.shufti_log.Add(id_log);
                    db.SaveChanges();
                    result["state"] = false;
                    result["message"] = reponseData["declined_reason"];
                    return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
                }
                else if (reponseData["event"].ToString() == "verification.accepted")
                {
                    string first_name = reponseData["verification_data"]["document"]["name"]["first_name"].ToString();
                    string last_name = reponseData["verification_data"]["document"]["name"]["last_name"].ToString();
                    string birthday = reponseData["verification_data"]["document"]["dob"].ToString();
                    id_log.status = "verification.accepted";
                    id_log.first_name = first_name;
                    id_log.last_name = last_name;
                    id_log.birthday = birthday;
                    id_log.document_number = reponseData["verification_data"]["document"]["document_number"].ToString();
                    id_log.expiry_date = reponseData["verification_data"]["document"]["expiry_date"].ToString();
                    id_log.issue_date = reponseData["verification_data"]["document"]["issue_date"].ToString();
                    db.shufti_log.Add(id_log);
                    db.SaveChanges();
                    var old_kyc = db.user_table.Where(a => a.firstname == first_name && a.lastname == last_name && a.kycVerified == true && a.birthday == birthday).FirstOrDefault();
                    if (old_kyc != null)
                    {
                        result["state"] = false;
                        result["message"] = "Someone already used this document.";
                        return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
                    }
                    user_data.birthday = birthday;
                    user_data.kycVerified = true;
                    db.SaveChanges();
                }
                result["state"] = true;
                db.SaveChanges();
                if(Session["isCreater"].ToString() == "yes")
                {
                    Session["next_url"] = "/Verify/sendEmailToOpp";
                }
                else if (Session["user_type"].ToString() == "old_seller")
                {
                    Session["next_url"] = "/Contract/bankDetail";
                }
                else
                {
                    Session["next_url"] = "/Contract/buyTransPage";
                }
                return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult saveUserInfo(UserSignupPage data, HttpPostedFileBase file)
        {
            bool Status = false;
            string message = "";
            JObject result = new JObject();
            try
            {
                AzureConnection db = new AzureConnection();
                
                var IsExist = userExist(data);
                if (IsExist)
                {
                    message = "Already exist. If you have an account , please log in";
                }
                else
                {
                    user_table tbdata = new user_table();
                    if (file != null)
                    {
                        shufti_log id_log = new shufti_log();
                        id_log.time = DateTime.UtcNow;
                        var memStream = new MemoryStream();
                        file.InputStream.CopyTo(memStream);
                        byte[] imageBytes = memStream.ToArray();
                        var base64String = Convert.ToBase64String(imageBytes);
                        string reponse = ApiHelper.KYCApi.verifyIDCard(base64String,data.firstname,data.lastname);
                        var reponseData = JObject.Parse(reponse);                        
                        if (reponseData["event"].ToString() == "verification.declined")
                        {
                            id_log.status = "verification.declined";
                            id_log.declined_reason = reponseData["declined_reason"].ToString();
                            db.shufti_log.Add(id_log);
                            db.SaveChanges();
                            result["state"] = false;
                            result["message"] = reponseData["declined_reason"];
                            return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
                        }
                        else if(reponseData["event"].ToString() == "verification.accepted")
                        {
                            string first_name = reponseData["verification_data"]["document"]["name"]["first_name"].ToString();
                            string last_name = reponseData["verification_data"]["document"]["name"]["last_name"].ToString();
                            string birthday = reponseData["verification_data"]["document"]["dob"].ToString();
                            id_log.status = "verification.accepted";
                            id_log.first_name = first_name;
                            id_log.last_name = last_name;
                            id_log.birthday = birthday;
                            id_log.document_number = reponseData["verification_data"]["document"]["document_number"].ToString();
                            id_log.expiry_date = reponseData["verification_data"]["document"]["expiry_date"].ToString();
                            id_log.issue_date = reponseData["verification_data"]["document"]["issue_date"].ToString();
                            db.shufti_log.Add(id_log);
                            db.SaveChanges();
                            var old_kyc = db.user_table.Where(a => a.firstname == first_name && a.lastname == last_name && a.kycVerified == true && a.birthday == birthday).FirstOrDefault();
                            if (old_kyc != null)
                            {
                                result["state"] = false;
                                result["message"] = "Someone already used this document.";
                                return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
                            }
                            /*
                            if (data.firstname.ToUpper() != first_name.ToUpper()||data.lastname.ToUpper() != last_name.ToUpper())
                            {
                                result["state"] = false;
                                result["message"] = "Your info is not correct!";
                                return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
                            }*/
                            tbdata.birthday = birthday;
                            tbdata.kycVerified = true;
                        }
                    }                                        
                    
                    tbdata.firstname = data.firstname;
                    tbdata.lastname = data.lastname;
                    tbdata.username = data.username;
                    tbdata.email = data.email;
                    tbdata.password = data.password;
                    tbdata.emailVerified = false;
                    tbdata.phone = Session["phoneNumber"].ToString();
                    tbdata.country = Session["countryCode"].ToString();
                    tbdata.phoneVerified = false;
                    tbdata.kycVerified = (file != null);
                    tbdata.qrkey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10);
                    tbdata.qrScanned = false;
                    tbdata.signup = DateTime.UtcNow;
                    tbdata.listme = false;
                    tbdata.emailCode = Guid.NewGuid();
                    SendVerificationLinkEmail(tbdata);
                    message = "Activate your email to complete your setup.We have sent activation link to your email: " + data.email;

                    Status = true;
                    db.user_table.Add(tbdata);
                    db.SaveChanges();
                    Session["email"] = tbdata.email;
                    Session["username"] = tbdata.username;
                    Session["qrkey"] = tbdata.qrkey;
                }                
            }
            catch(Exception ex)
            {
                message = "Some error occured.Please try again";
            }
            result["state"] = Status;
            result["message"] = message;
            return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
        }
        
        public bool userExist(UserSignupPage data)
        {
            AzureConnection db = new AzureConnection();
            var v = db.user_table.Where(a => a.email == data.email||a.username == data.username).FirstOrDefault();
            return v != null;
        }
        [NonAction]
        public void SendVerificationLinkEmail(user_table tbdata)
        {
            try
            {
                var verifyUrl = "/Verify/VerifyAccount/" + tbdata.emailCode;
                var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

                var fromEmail = new MailAddress(WebConfigurationManager.AppSettings.Get("siteEmail"), "Lyohai");
                var toEmail = new MailAddress(tbdata.email);
                var fromEmailPassword = WebConfigurationManager.AppSettings.Get("EmailPassword");
                string subject = "TRUSTBTC to you";

                string body = "Dear " + tbdata.username + "<br/><br/>" +
                    "Please click on the below link to verify your account" +
                    "<br/><br/><a href='" + link + "'>" + link + "</a>";

                var smtp = new SmtpClient
                {
                    Host = "smtp-mail.outlook.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
                };
                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
            }
            catch
            {

            }
        }
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            try { 
                AzureConnection db = new AzureConnection();
                var v = db.user_table.Where(a => a.emailCode == new Guid(id)).FirstOrDefault();
                if (v != null)
                {
                    v.emailVerified = true;
                    db.SaveChanges();
                    
                    Session["email"] = v.email;
                    Session["username"] = v.username;
                    Session["qrkey"] = v.qrkey;   
                    if (Session["user_type"].ToString() == "new_buyer")
                    {
                        Session["next_url"] = "/Contract/buyTransPage";
                        return RedirectToAction("sendEmailToOpp", "Verify");
                    }
                    else if(Session["user_type"].ToString() == "new_seller")
                    {
                        Session["next_url"] = "/Contract/bankDetail";                    
                        return RedirectToAction("sendEmailToOpp", "Verify");
                    }
                    else
                    {
                        return RedirectToAction("", "Home");
                    }                    
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
        public ActionResult sendEmailToOpp()
        {
            try
            {
                trans_history data = new trans_history();
                string email = Session["email"].ToString();
                AzureConnection db = new AzureConnection();
                user_table user_data = db.user_table.Where(a => a.email == email).FirstOrDefault();
                if (Session["user_type"].ToString() == "new_buyer" || Session["user_type"].ToString() == "old_buyer")
                {
                    data.buyer = user_data.id;
                    email = Session["sellerEmail"].ToString();
                    user_table seller = db.user_table.Where(a => a.email == email).FirstOrDefault();
                    Session["sellerName"] = seller.username;
                    data.seller = seller.id;
                }
                else
                {
                    data.seller = user_data.id;
                    email = Session["buyerEmail"].ToString();
                    user_table buyer = db.user_table.Where(a => a.email == email).FirstOrDefault();
                    Session["buyerName"] = buyer.username;
                    data.buyer = buyer.id;
                }

                data.btc = double.Parse(Session["btc_amount"].ToString(), CultureInfo.InvariantCulture.NumberFormat);
                data.cash = double.Parse(Session["cash"].ToString(), CultureInfo.InvariantCulture.NumberFormat);
                data.REFERENCE = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10).ToUpper();
                data.status = "pending";
                data.holdBTC = false;
                data.sentBTC = false;
                data.sentCash = false;
                data.recieveCash = false;
                data.holdAmount = 0;
                data.holdWallet = BlockApi.getNewAddressWithRandom();
                db.trans_history.Add(data);
                db.SaveChanges();
                Session["holdWallet"] = data.holdWallet;
                Session["transID"] = data.id;
                var fromEmail = new MailAddress(WebConfigurationManager.AppSettings.Get("siteEmail"), "Lyohai");
                MailAddress toEmail;
                var fromEmailPassword = WebConfigurationManager.AppSettings.Get("EmailPassword");
                string subject = "TRUSTBTC to you";
                string body;
                if (Session["user_type"].ToString() == "new_buyer" || Session["user_type"].ToString() == "old_buyer")
                {
                    string proceed_link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "/Contract/proceed?" + "userID=" + data.seller + "&transID=" + data.id);
                    toEmail = new MailAddress(data.user_seller.email);
                    body = "Dear " + Session["sellerName"] + "<br/><br/>" +
                    "One of our members," + Session["username"].ToString() + " will pay you " + "<b>$" + Session["cash"].ToString() + "</b>, if you agree to pay <b>" + Session["btc_amount"].ToString() + "BTC</b>.We will handle all process and secure a smooth transaction for you, in 3 very easy steps." + "<br/><br/>If you are interested to proceed with this transaction, please click the botton below.<br/><br/> <a href='" + proceed_link + "'>" + "Proceed </a>";
                }
                else
                {
                    toEmail = new MailAddress(data.user_buyer.email);
                    string proceed_link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "/Contract/proceed?" + "userID=" + data.buyer + "&transID=" + data.id);
                    body = "Dear " + Session["buyerName"] + "<br/><br/>" +
                    "One of our members," + Session["username"].ToString() + " will pay you " + "<b>" + Session["btc_amount"].ToString() + "BTC</b>, if you agree to pay <b>$" + Session["cash"].ToString() + "</b>.We will handle all process and secure a smooth transaction for you, in 3 very easy steps." + "<br/><br/>If you are interested to proceed with this transaction, please click the botton below.<br/><br/> <a href='" + proceed_link + "'>" + "Proceed </a>";
                }

                var smtp = new SmtpClient
                {
                    Host = "smtp-mail.outlook.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
                };
                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                return RedirectToAction("googleAuthPage", "Verify");
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }            
        }
        public ActionResult googleAuthPage()
        {
            try {
                AzureConnection db = new AzureConnection();
                string email = Session["email"].ToString();
                var user_data = db.user_table.Where(a => a.email == email).FirstOrDefault();
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                SetupCode setupInfo = tfa.GenerateSetupCode("TRUSTBTC", Session["email"].ToString(), Session["qrkey"].ToString(), 200, 200);
                ViewData["qrUrl"] = setupInfo.QrCodeSetupImageUrl;
                ViewBag.scanned = user_data.qrScanned;
                return View();
            }
            catch
            {
                return RedirectToAction("", "Home");
            }
        }
        public JsonResult validateQRCode(string qrCode)
        {
            try
            {
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                AzureConnection db = new AzureConnection();
                string email = Session["email"].ToString();
                var user_data = db.user_table.Where(a => a.email == email).FirstOrDefault();
                var result = tfa.ValidateTwoFactorPIN(user_data.qrkey, qrCode);
                result = true;
                if (result)
                {
                    user_data.qrScanned = true;
                    db.SaveChanges();
                    return Json("success", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json("failed", JsonRequestBehavior.AllowGet);
                }
            }
            catch
            {
                return Json("failed", JsonRequestBehavior.AllowGet);
            }

        }        
    }
}