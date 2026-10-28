using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EskaCMS.Entities.Entity
{
    public class TelrEnums
    {

        public enum TerlSearchTypes
        {
            ByEmail = 1,
            ByOrderId = 2,
            ByCustomerNo = 3
        }

        public enum TelrTransTest

        {
            Test = 1,
            Production = 0
        }

        public enum MyEnum
        {

            Value1,
            [Description("value 2")]
            Value2,
            [Description("value 3")]
            Value3
        }

        public enum TelrTransStatusCodes
        {
            [Description("Set by issuer Authorised")]
            A,
            [Description("Authorised, but placed on hold. Manual inspection required")]
            H,
            [Description("Invalid request")]
            E01,
            [Description("Transaction cost or currency not supplied")]
            E02,
            [Description("Cart ID not set")]
            E03,
            [Description("Invalid store ID")]
            E04,
            [Description("Transaction cost or currency not valid")]
            E05,
            [Description("Invalid transaction mode")]
            E06,
            [Description("Card expiry not supplied")]
            E07,
            //[Description("Card number not supplied")]
            //E08,
            //[Description("Invalid card number")]
            //E09,
            [Description("Card number not supplied")]
            E10,
            [Description("Invalid card number")]
            E11,
            [Description("Card expired")]
            E12,
            //[Description("Authorised, but placed on hold. Manual inspection required")]
            //E13,
            [Description("Card type mismatch")]
            E14,
            [Description("Invalid card security code")]
            E15,
            [Description("Card security code not supplied")]
            E16,
            [Description("Name not valid/not supplied")]
            E17,
            [Description("Address not valid/not supplied")]
            E18,
            [Description("Country not valid/not supplied")]
            E19,
            [Description("IP address not valid/not supplied")]
            E20,
            [Description("Card/Currency combination not supported")]
            E21,
            [Description("Invalid transaction reference")]
            E22,
            [Description("Amount differs from original")]
            E23,
            [Description("Currency differs from original")]
            E24,
            [Description("Original transaction not authorized")]
            E25,
            [Description("Original transaction already voided")]
            E26,
            [Description("Original transaction not a sale")]
            E27,
            [Description("Original transaction not a refund")]
            E28,
            [Description("Amount greater than available balance")]
            E29,
            [Description("Card details differ from original")]
            E30,
            [Description("Not authorized")]
            D31,
            [Description("Original transaction cannot be voided")]
            D32,
            [Description("Transaction cancelled")]
            C33,
            [Description("No response")]
            D34,
            [Description("Unable to refund")]
            E35,
            [Description("Previous transaction is on hold")]
            E36,
            [Description("Blocked by acquirer")]
            D37,
            [Description("Invalid expiry date")]
            E38,
            [Description("Invalid transaction class")]
            E39,
            [Description("Invalid transaction type")]
            E40,
            [Description("Insufficient funds")]
            D41,
            [Description("Card security code mismatch")]
            D42,
            [Description("Email not valid/not supplied")]
            E43,
            [Description("Phone number not valid/not supplied")]
            E44,
            [Description("Transaction mode differs from original")]
            E45,
            [Description("3DSecure authentication not available for this card")]
            D46,
            [Description("3DSecure authentication rejected")]
            D47,
            [Description("Description not set")]
            E48,
            [Description("Sold out")]
            D49,
            [Description("Card is for ATM use only")]
            E50,
            [Description("Transaction part 1 not authorised")]
            D51,
            [Description("Transaction part 2 not authorised")]
            D52,
            [Description("Authorisation expired")]
            X53,
            [Description("Not authorised")]
            D80,
            [Description("Not authorised")]
            D90,
            [Description("Not authorised")]
            D91,
            [Description("Not authorised")]
            D92,
            [Description("Internal system error")]
            E98,
            [Description("Unknown error")]
            E99,


        }
    }
}
