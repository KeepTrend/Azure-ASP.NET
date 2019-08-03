

using Exchange.AzureModel;
using Exchange.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Exchange.Controllers
{
    public class User_LoginController : Controller
    {
        [NonAction]
        public bool userExist(UserSignupPage data)
        {
            AzureConnection db = new AzureConnection();
            var v = db.user_table.Where(a => a.email == data.email || a.username == data.username).FirstOrDefault();
            return v != null;
        }
        [NonAction]
        public void SendVerificationLinkEmail(user_table tbdata)
        {
            var verifyUrl = "/User_Login/VerifyAccount/" + tbdata.emailCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress(WebConfigurationManager.AppSettings.Get("siteEmail"), "Lyohai");
            var toEmail = new MailAddress(tbdata.email);
            var fromEmailPassword = WebConfigurationManager.AppSettings.Get("EmailPassword"); 
            string subject = "Your account is successfully created";

            string body = "<br/><br/>Dear " + tbdata.username + "<br/><br/>" +
                    "Please click on the below link to verify your account" +
                    "<br/><br/><a href='" + link + "'>" + link + "</a>"; ;

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
        // GET: User_Login
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Logout()
        {
            Session.RemoveAll();
            return RedirectToAction("", "Home");
        }
        [HttpPost]
        public ActionResult Login(UserLoginPage data)
        {
            string message = "";
            try
            {
                AzureConnection db = new AzureConnection();
                var result = db.user_table.Where(x => x.email == data.EmailID && x.password == data.Password).FirstOrDefault();
                if (result == null)
                {
                    message = "Invalid credential provided";
                }
                else
                {
                    if(result.emailVerified == false)
                    {
                        message = "Your account is not verified!";
                    }
                    else
                    {
                        Session["username"] = result.username;
                        Session["email"] = result.email;
                        Session["qrkey"] = result.qrkey;
                        if (Session["next_url"] == null)
                        {
                            return RedirectToAction("", "Home");
                        }
                        else if(Session["user_type"]!=null)
                        {
                            return Redirect(Session["next_url"].ToString());
                        }
                        else
                        {
                            return RedirectToAction("", "Home");
                        }
                    }
                }                
            }
            catch
            {
                message = "Database connection failed!";
            }
            ViewBag.Message = message;
            return View(data);
        }
        [HttpGet]
        public ActionResult Registeration()
        {
            ViewData["phoneNumber"] = Session["phoneNumber"].ToString();
            return View();
        }
        public ActionResult getStarted()
        {
            Session["phone_next_url"] = "/User_Login/Registeration";
            return RedirectToAction("phoneVerify", "Verify");
        }
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            AzureConnection db = new AzureConnection();
            var v = db.user_table.Where(a => a.emailCode == new Guid(id)).FirstOrDefault();
            if (v != null)
            {
                v.emailVerified = true;
                db.SaveChanges();
                Status = true;
            }
            else
            {
                ViewBag.Message = "Invalid Reguest";
            }                
                                                              //confirm password does not match issue on save changes
            ViewBag.Status = Status;
            return View();
        }
        [HttpPost]
        public ActionResult Registeration(UserSignupPage data)
        {
            bool Status = false;
            string message = "";
            ViewData["phoneNumber"] = Session["phoneNumber"].ToString();
            try
            {
                var IsExist = userExist(data);
                if (IsExist)
                {
                    message = "Email or username already exists";
                }
                else
                {
                    AzureConnection db = new AzureConnection();
                    user_table tbdata = new user_table();
                    tbdata.firstname = data.firstname;
                    tbdata.lastname = data.lastname;
                    tbdata.username = data.username;
                    tbdata.email = data.email;
                    tbdata.phone = Session["phoneNumber"].ToString();
                    tbdata.country = Session["countryCode"].ToString();
                    tbdata.phoneVerified = false;
                    tbdata.kycVerified = false;
                    tbdata.password = data.password;
                    tbdata.emailVerified = false;
                    tbdata.qrkey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10);
                    tbdata.signup = DateTime.UtcNow;
                    tbdata.listme = false;
                    tbdata.qrScanned = false;
                    tbdata.emailCode = Guid.NewGuid();                        
                    SendVerificationLinkEmail(tbdata);
                    message = "Activate your email to complete your setup. We have sent activation link to your email: " + data.email;
                    Status = true;
                    db.user_table.Add(tbdata);
                    db.SaveChanges();
                }     
            }
            catch(Exception ex)
            {
                message = ex.Message;
            }
            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(data);
        }
    }
}