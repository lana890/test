using EskaCommerce.UserCatalog.Dtos;
using System.Threading.Tasks;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IUserPlansApiService
    {
        GridOutputVM<SubscriberListVM> GetFilteredUserPlans(SearchTableParam<SubscribersListSearchVM> param);
        GridOutputVM<UserPlansGridOutput> GetUserPlansById(long userId, SearchTableParam<UserPlansListSearchVM> param);
    }

}
