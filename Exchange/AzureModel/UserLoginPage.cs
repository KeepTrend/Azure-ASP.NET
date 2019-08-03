using System;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace Exchange.AzureModel
{
    public class UserLoginPage 
    {
        public string EmailID { get; set; }

        public string Password { get; set; }

        public string rememberMe { get; set; }
    }
}
