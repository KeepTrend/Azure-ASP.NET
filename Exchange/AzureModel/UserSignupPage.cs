using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
namespace Exchange.AzureModel
{
    public class UserSignupPage
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string password { get; set; }
        public string confirm { get; set; }
    }
}