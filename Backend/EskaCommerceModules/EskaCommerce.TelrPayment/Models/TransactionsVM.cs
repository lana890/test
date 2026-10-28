using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace EskaCommerce.TelrPayment.Models
{


    public class TransactionsVm
    {
        public string date { get; set; }
        public string    ttl { get; set; }
        public string trancount { get; set; }

        [XmlElement("transaction")]
        public List<Transaction> transaction { get; set; }
    }
   public class Type
    {
        public string name { get; set; }
        public string code { get; set; }
          
    }
    public class Class
    {
        public string name { get; set; }
        public string code { get; set; }
         
    }
    public class Auth
    {
        public string status { get; set; }
        public string statustxt { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        
          
    }
    public  class Store
    {
        public string id { get; set; }
        public string name { get; set; }
          
    }
    public  class Billing
    {
        public string title { get; set; }
        public string fname { get; set; }
        public string sname { get; set; }
        public string fullname { get; set; }
        public string addr1 { get; set; }
        public string addr2 { get; set; }
        public string addr3 { get; set; }
        public string city { get; set; }
        public string region { get; set; }
        public string country { get; set; }
        public string country_iso { get; set; }
        public string zip { get; set; }
        public string tel { get; set; }
        public string email { get; set; }
        public string ip { get; set; }
          
           
    }

    public  class Transaction
    {
        public string id { get; set; }
        public string prev_id { get; set; }
        public string init_id { get; set; }
        public Type type { get; set; }
        public Class @class { get; set; }
        public string agreementid { get; set; }
        public Auth auth { get; set; }
        public string amount { get; set; }
        public string amount_txt { get; set; }
        public string currency { get; set; }
        public string description { get; set; }
        public string cartid { get; set; }
        public string test { get; set; }
        public string link { get; set; }
        public string date { get; set; }

        public Store store { get; set; }
        public Billing billing { get; set; }
        public Card card { get; set; }
        public string notecount { get; set; }
        public List<Notes> notes { get; set; }
        
    }
    public class Notes
    {
        public string @ref { get; set; }
        public string text { get; set; }
    }
    public class Card
    {
        public string number { get; set; }
        public string type { get; set; }
        public string country { get; set; }
        public string country_iso { get; set; }
       

    }
}
