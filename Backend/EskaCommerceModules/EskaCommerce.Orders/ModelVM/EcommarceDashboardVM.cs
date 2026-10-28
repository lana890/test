using Elastic.Clients.Elasticsearch.SnapshotLifecycleManagement;
using EskaCommerce.Orders.ModelVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.UserDashboard.ViewModels
{
    public class OrderChartVM
    {
        public Dictionary<string, OrderChartPVM> response { set; get; }
    }

    public class OrderChartPVM
    {
        public double? value { set; get; }
        public double? trend { set; get; }
        public bool isPositive { set; get; }
    }

    public class MostSearchedVM
    {
        public long productId { set; get; }

        public string productName { set; get; }

        public double popularity { set; get; }

        public double salesPercentage { set; get; }

    }


    public class DashboardDto
    {
        public ClvChartDto clv_chart { set; get; }
        public CustomersDto allCustomers { set; get; }

        public CustomersDto guest_users { set; get; }

        public CustomersDto active_Customers { set; get; }

        public CustomersDto inActive_Customer { set; get; }

        public RateMetricDto retention_rate { set; get; }

        public RateMetricDto churn_rate { set; get; }


    }
    public class ClvChartDto
    {
        public List<ChartDto> chart_data {  set; get; }
        public string period {  set; get; }
    }

    public class CustomersDto
    {
        public int count { set; get; }
        public double trend_percentage { get; set; }
        public string trend_direction { set; get; }
    }

    public class RateMetricDto
    {
        public double percentage {  set; get; }
        public double trend_percentage {  set; get; }
        public string trend_direction {  get; set; }

    }

    public class ChartDto
    {
        
       public DateTime date {  set; get; }
       public decimal clv_value {  set; get; }
       public decimal high_spenders {  set; get; }
       public decimal casual_shoppers {  set; get; }
       public decimal prime_only_buyers {  set; get; }

    }
}