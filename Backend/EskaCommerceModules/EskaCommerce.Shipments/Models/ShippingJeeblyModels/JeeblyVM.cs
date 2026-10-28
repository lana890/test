using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.ShippingJeebly.Models
{

    public class Consignments
    {
        public Consignments()
        {
            origin_details = new OriginalDetails();
            destination_details = new DestinationDetails();
            pieces_detail = new List<PiescesDetail>();

        }
        public bool is_risk_surcharge_applicable { get; set; }
        public string customer_code { get; set; }
       // public string reference_number { get; set; }
        public string service_type_id { get; set; }
        public string load_type { get; set; }
        public string description { get; set; }
       // public string cod_favor_of { get; set; }
      //  public string cod_collection_mode { get; set; }
       // public string dimension_unit { get; set; }
       // public string length { get; set; }
      //  public string width { get; set; }
      //  public string height { get; set; }
      //  public string weight_unit { get; set; }
      //  public string weight { get; set; }
        public string declared_value { get; set; }
       // public string cod_amount { get; set; }
        public string num_pieces { get; set; }
        public string customer_reference_number { get; set; }
        public string commodity_id { get; set; }
        // public string commodity_id { get; set; }


        public OriginalDetails origin_details { get; set; }
        public DestinationDetails destination_details { get; set; }
        public List<PiescesDetail> pieces_detail { get; set; }

    }
    public class OriginalDetails
    {


        public string name { get; set; }
        public string phone { get; set; }
        public string address_line_1 { get; set; }
        public string address_line_2 { get; set; }
        public string pincode { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string alternate_phone { get; set; }

    }
    public class DestinationDetails
    {
        public string name { get; set; }
        public string phone { get; set; }
        public string address_line_1 { get; set; }
        public string address_line_2 { get; set; }
        public string pincode { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string alternate_phone { get; set; }



    }
    public class PiescesDetail
    {
        public string description { get; set; }
        public string declared_value { get; set; }
        public string weight { get; set; }
        public string height { get; set; }
        public string length { get; set; }

        public string width { get; set; }


    }
    public class JeeblyVM
    {
        public JeeblyVM()
        {
            consignments = new List<Consignments>();
        }
        public List<Consignments> consignments { get; set; }

    }


}
