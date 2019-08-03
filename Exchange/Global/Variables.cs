using Exchange.AzureModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Exchange.Global
{
    public class Variables
    {
        static Variables instance;
        public string cur_price = "";
        public static Variables getInstance()
        {
            if (instance == null)
                instance = new Variables();
            return instance;
        }
        public static void sendEmail(string reciever, string body)
        {
            var fromEmail = new MailAddress(WebConfigurationManager.AppSettings.Get("siteEmail"), "Lyohai");
            var toEmail = new MailAddress(reciever);
            var fromEmailPassword = WebConfigurationManager.AppSettings.Get("EmailPassword");
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
                Subject = "TRUSTBTC to you",
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }
        public static string sendSms(string phone)
        {
            string Account_SID = "ACe409319353d37f7582e7517b9b0d3f66";
            string Auth_Token = "405961c9165a4b1b1d0869bb18100344";
            TwilioClient.Init(Account_SID, Auth_Token);
            var code = new Random().Next(100000, 999999);
            var sms = MessageResource.Create(
                                body: "Your verification code for TRUSTBTC is " + code.ToString(),
                                from: new Twilio.Types.PhoneNumber("+18558385778"),
                                to: new Twilio.Types.PhoneNumber("+"+phone)
                                );
            return code.ToString();            
        }
    }
}