using EskaCMS.Core.Services;
using EskaCMS.DynamicForms.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.DynamicForms.HelperClasses.IHelperClasses;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.DynamicForms;

namespace EskaCMS.DynamicForms.HelperClasses
{
    public class ConditionsManager : IConditionsManager
    {
        private readonly ICurrencyRatesApiService _CurrencyRatesService;
        public ConditionsManager(
            ICurrencyRatesApiService CurrencyRatesService)
        {
            _CurrencyRatesService = CurrencyRatesService;
        }


        public async Task<bool> CheckTheCriaterias(List<Criteria> CriteriaList, List<DynamicFormsWebVM> CriteriaModel)
        {
            try
            {
                foreach (var CriteriaModelItem in CriteriaModel)
                {
                    var SelectedCriterias = CriteriaList
                                            .Where(c => c.FormFieldId == CriteriaModelItem.FormFieldId && c.Status == EStatus.Published)
                                            .ToList();

                    if (!IsAppliedTo(SelectedCriterias, CriteriaModelItem))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public bool IsAppliedTo(List<Criteria> CriteriaList, DynamicFormsWebVM CriteriaModel)
        {
            try
            {
                foreach (var CriteriaItem in CriteriaList)
                {
                    if (!IsAppliedToCriateria(CriteriaItem, CriteriaModel))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public bool IsAppliedToCriateria(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            try
            {
                if (CriteriaItem.Type != EDynamicFormsCriteriaTypes.Condition
                    || CriteriaItem.FormField == null)
                {
                    throw new Exception("Invalid Criteria");
                }

                switch (CriteriaItem.ConditionsType)
                {
                    case EDynamicCriteriaConditions.Equals:
                        return CheckIfEquals(CriteriaItem, CriteriaModel);

                    case EDynamicCriteriaConditions.EqualsOneOf:
                        return CheckIfEqualOneOf(CriteriaItem, CriteriaModel);
                    // type = Equal one of && any of list is the value 
                    case EDynamicCriteriaConditions.NotEquals:
                        return CheckIfNotEquals(CriteriaItem, CriteriaModel);

                    case EDynamicCriteriaConditions.MoreThan:
                        return CheckIfMoreThan(CriteriaItem, CriteriaModel);

                    case EDynamicCriteriaConditions.LessThen:
                        return CheckIfLessThan(CriteriaItem, CriteriaModel);


                    case EDynamicCriteriaConditions.MoreThenOrEquals:
                        return CheckIfMoreThanOrEquals(CriteriaItem, CriteriaModel);

                    case EDynamicCriteriaConditions.LessThenOrEquals:
                        return CheckIfLessThanOrEquals(CriteriaItem, CriteriaModel);

                    default:
                        throw new Exception("Unknown Condition Type");
                }

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        //private bool CheckIfEqualOneOf(Criteria criteriaItem, PricingQutationWebVM criteriaModel)
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}

        #region HelperFunction

        private bool CheckIfEquals(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            try
            {

                switch (CriteriaItem.FormField.InputType)
                {
                    case EDynamicCriteriaInputTypes.Text:
                    case EDynamicCriteriaInputTypes.TextArabic:
                    case EDynamicCriteriaInputTypes.TextEnglish:
                    case EDynamicCriteriaInputTypes.PhoneNumber:
                    case EDynamicCriteriaInputTypes.Email:
                        return CriteriaItem.Value == CriteriaModel.Value;

                    case EDynamicCriteriaInputTypes.Date:
                        return DateTime.Compare(Convert.ToDateTime(CriteriaItem.Value), Convert.ToDateTime(CriteriaModel.Value)) == 0;


                    case EDynamicCriteriaInputTypes.Option:
                        switch (CriteriaItem.FormField.OneOfOptionsType)
                        {
                            case EDynamicFormsOneOfOptions.DataSource:
                                return CriteriaItem.Value == CriteriaModel.Value;
                            default:
                                return CriteriaItem.CategoryCriterias.Count == 0 ? false : CriteriaItem.CategoryCriterias.Count > 0 && CriteriaItem.CategoryCriterias.Any(x => x.SubCategoryId.ToString().ToLower().Equals(CriteriaModel.SubCategoryId.ToLower()));
                        }


                    case EDynamicCriteriaInputTypes.Number:
                        return decimal.Parse(CriteriaItem.Value) == decimal.Parse(CriteriaModel.Value);

                    case EDynamicCriteriaInputTypes.Currency:

                        return decimal.Parse(CriteriaItem.Value) == ConvertToDefaultAsync(decimal.Parse(CriteriaModel.Value), CriteriaModel.CurrencyRatesId).Result; ;

                    default:
                        throw new Exception("Unknown Input Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private bool CheckIfEqualOneOf(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            try
            {

                switch (CriteriaItem.FormField.InputType)
                {

                    case EDynamicCriteriaInputTypes.Option:
                        return CriteriaItem.CategoryCriterias == null ? false : CriteriaItem.CategoryCriterias.Any(x => x.SubCategoryId.ToString().ToLower().Equals(CriteriaModel.SubCategoryId.ToLower()));


                    default:
                        throw new Exception("Un valid Input Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private bool CheckIfNotEquals(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            return !CheckIfEquals(CriteriaItem, CriteriaModel);
        }


        private bool CheckIfLessThan(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            try
            {

                switch (CriteriaItem.FormField.InputType)
                {

                    case EDynamicCriteriaInputTypes.Number:
                        return decimal.Parse(CriteriaItem.Value) > decimal.Parse(CriteriaModel.Value);


                    case EDynamicCriteriaInputTypes.Date:
                        return DateTime.Compare(Convert.ToDateTime(CriteriaItem.Value), Convert.ToDateTime(CriteriaModel.Value)) > 0;


                    case EDynamicCriteriaInputTypes.Currency:
                        return decimal.Parse(CriteriaItem.Value) > ConvertToDefaultAsync(decimal.Parse(CriteriaModel.Value), CriteriaModel.CurrencyRatesId).Result;
                    default:
                        throw new Exception("Unknown Input Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private bool CheckIfMoreThan(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            try
            {

                switch (CriteriaItem.FormField.InputType)
                {

                    case EDynamicCriteriaInputTypes.Number:
                        return decimal.Parse(CriteriaItem.Value) < decimal.Parse(CriteriaModel.Value);

                    case EDynamicCriteriaInputTypes.Date:
                        return DateTime.Compare(Convert.ToDateTime(CriteriaItem.Value), Convert.ToDateTime(CriteriaModel.Value)) < 0;

                    case EDynamicCriteriaInputTypes.Currency:
                        return decimal.Parse(CriteriaItem.Value) < ConvertToDefaultAsync(decimal.Parse(CriteriaModel.Value), CriteriaModel.CurrencyRatesId).Result;
                    default:
                        throw new Exception("Unknown Input Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private bool CheckIfMoreThanOrEquals(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            try
            {

                switch (CriteriaItem.FormField.InputType)
                {

                    case EDynamicCriteriaInputTypes.Number:
                        return decimal.Parse(CriteriaItem.Value) <= decimal.Parse(CriteriaModel.Value);

                    case EDynamicCriteriaInputTypes.Date:
                        return DateTime.Compare(Convert.ToDateTime(CriteriaItem.Value), Convert.ToDateTime(CriteriaModel.Value)) <= 0;

                    case EDynamicCriteriaInputTypes.Currency:
                        return decimal.Parse(CriteriaItem.Value) <= ConvertToDefaultAsync(decimal.Parse(CriteriaModel.Value), CriteriaModel.CurrencyRatesId).Result;
                    default:
                        throw new Exception("Unknown Input Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        private bool CheckIfLessThanOrEquals(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            try
            {

                switch (CriteriaItem.FormField.InputType)
                {

                    case EDynamicCriteriaInputTypes.Number:
                        return decimal.Parse(CriteriaItem.Value) >= decimal.Parse(CriteriaModel.Value);

                    case EDynamicCriteriaInputTypes.Date:
                        return DateTime.Compare(Convert.ToDateTime(CriteriaItem.Value), Convert.ToDateTime(CriteriaModel.Value)) >= 0;


                    case EDynamicCriteriaInputTypes.Currency:
                        return decimal.Parse(CriteriaItem.Value) >= ConvertToDefaultAsync(decimal.Parse(CriteriaModel.Value), CriteriaModel.CurrencyRatesId).Result;
                    default:
                        throw new Exception("Unknown Input Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private async Task<decimal> ConvertToDefaultAsync(decimal value, long CurrencyRatesId)
        {
            try
            {

                var CurrencyRate = await _CurrencyRatesService.GetCurrencyById(CurrencyRatesId);
                return value * CurrencyRate.Rate;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion
    }
}
