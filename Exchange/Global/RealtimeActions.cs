
using Exchange.AzureModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Twilio.Http;

namespace Exchange.Global
{
    public class RealtimeActions
    {
        public static void getCurrentPrice()
        {
            try
            {
                string currentPrice = ApiHelper.BlockApi.getCurrentPrice();
                Variables.getInstance().cur_price = currentPrice;
            }
            catch { }
        }
        public static void checkHoldBalance()
        {
            try
            {
                using (AzureConnection db = new AzureConnection())
                {

                    trans_history[] arr = db.trans_history.Where(a => a.holdBTC != true).ToArray();
                    foreach (trans_history v in arr)
                    {
                        string cur_balance = ApiHelper.BlockApi.getBalanceByAddress(v.holdWallet);
                        //                string cur_balance = ""+v.btc;
                        if (double.Parse(cur_balance) >= v.btc)
                        {
                            v.holdBTC = true;
                            v.holdTransID = "adfasdfasdfasdfasdf";
                            string body;

                            string proceed_link = "http://trustbtc.azurewebsites.net/Contract/sentWire?transID=" + v.id;
                            body = "Dear " + v.user_buyer.username + "<br/><br/>" +
                                "We have received " + v.btc + "BTC" + " from user " + v.user_seller.username + ", and we hold them for you." + " To get them in your wallet you need to send a wire of $" + v.cash + " to:" +
                                "<br/><br/>BENEFICIARY:" + v.firstname_seller + " " + v.lastname_seller + "<br/>IBAN:" + v.IBAN + "<br/>SWIFT:" + v.SWIFT + "<br/>REFERENCE:" + v.REFERENCE + " <br/><br/> After you have sent the wire, make sure to click below:<br/> <a href='" + proceed_link + "' class='btn btn-primary'>" + "I have sent wire </a>"; ;
                            Variables.sendEmail(v.user_buyer.email, body);
                        }
                        v.holdAmount = double.Parse(cur_balance);
                        db.SaveChanges();

                    }

                }
            }
            catch
            {  }      
        }
    }
}