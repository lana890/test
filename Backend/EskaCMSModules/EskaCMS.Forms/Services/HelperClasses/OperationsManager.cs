using EskaCMS.Core.Services;
using EskaCMS.DynamicForms.HelperClasses.IHelperClasses;
using EskaCMS.DynamicForms.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.DynamicForms;

namespace EskaCMS.DynamicForms.HelperClasses
{
    public class OperationsManager : IOperationsManager
    {

        private readonly ICurrencyRatesApiService _CurrencyRatesService;

        private string _Pattern = @"(\b[a-zA-Z]\b)";

        public OperationsManager(ICurrencyRatesApiService CurrencyRatesService)
        {
            _CurrencyRatesService = CurrencyRatesService;
        }


        public async Task<string> AppliedOperation(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {

            try
            {
                if (CriteriaItem.Type != EDynamicFormsCriteriaTypes.Operation)
                {
                    throw new Exception("Invalid Criteria");
                }

                switch (CriteriaItem.OperationType)
                {
                    case EDynamicCriteriaOperations.UsePercentageDiscount:
                        return UsePercentageDiscount(CriteriaItem, CriteriaModel);

                    case EDynamicCriteriaOperations.UseMoneyAmountDiscount:
                        return UseMoneyAmountDiscount(CriteriaItem, CriteriaModel);

                    case EDynamicCriteriaOperations.ShowValue:
                        return ShowValue(CriteriaItem);


                    default:
                        throw new Exception("Invalid Condition Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<string> AppliedOperation(Criteria CriteriaItem, List<DynamicFormsWebVM> CriteriaModels)
        {

            try
            {
                if (CriteriaItem.Type != EDynamicFormsCriteriaTypes.Operation)
                {
                    throw new Exception("Invalid Criteria");
                }

                switch (CriteriaItem.OperationType)
                {

                    case EDynamicCriteriaOperations.UseFormula:
                        return UseFormula(CriteriaItem, CriteriaModels);

                    case EDynamicCriteriaOperations.ShowValue:
                        return await AppliedOperation(CriteriaItem, CriteriaModels.FirstOrDefault());

                    case EDynamicCriteriaOperations.UseMoneyAmountDiscount:
                    case EDynamicCriteriaOperations.UsePercentageDiscount:
                        return await AppliedOperation(CriteriaItem, CriteriaModels.FirstOrDefault(c => c.FormFieldId == CriteriaItem.FormFieldId));


                    default:
                        throw new Exception("Invalid Operation Type");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }// database , user data



        #region HelperFunctions
        private string UsePercentageDiscount(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            if (CriteriaModel == null)
            {
                return null;
            }
            decimal DiscountAmount = decimal.Parse(CriteriaItem.Value);
            decimal UserValue = ConvertToDefaultAsync(decimal.Parse(CriteriaModel.Value), CriteriaModel.CurrencyRatesId).Result;
            decimal result = UserValue * DiscountAmount / 100;
            return result.ToString("n" + 3);
        }

        private string UseMoneyAmountDiscount(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel)
        {
            if (CriteriaModel == null)
            {
                return null;
            }
            decimal DiscountAmount = decimal.Parse(CriteriaItem.Value);
            decimal UserValue = ConvertToDefaultAsync(decimal.Parse(CriteriaModel.Value), CriteriaModel.CurrencyRatesId).Result;
            decimal result = DiscountAmount - UserValue < 0 ? 0 : DiscountAmount - UserValue;
            return result.ToString("n" + 3);
        }

        private string UseFormula(Criteria CriteriaItem, List<DynamicFormsWebVM> CriteriaModels)
        {
            return ApplyFormula(CriteriaItem.Value, CriteriaModels).ToString();
        }

        private string ShowValue(Criteria CriteriaItem)
        {
            return CriteriaItem.Value;
        }

        #endregion


        #region Calculator

        private double ApplyFormula(string Formula, List<DynamicFormsWebVM> CriteriaModels)
        {

            Formula = ReplaceVariables(Formula, CriteriaModels); // not completed
            var SplittedFormula = SplitFormula(Formula); // need test
            var PostFixFormula = ConvertToPostFix(SplittedFormula); // need test
            return ExecuteFormula(PostFixFormula);

        }


        private string ReplaceVariables(string Formula, List<DynamicFormsWebVM> CriteriaModels)
        {

            foreach (var Criteria in CriteriaModels)
            {
                Formula = Formula.Replace('(' + Criteria.FormFieldName + ')', Criteria.Value);
            }

            return Formula;


        }


        private List<string> SplitFormula(string Formula)
        {
            List<string> Result = new List<string>();
            string Temp = string.Empty;

            foreach (var character in Formula)
            {
                string item = character.ToString();


                if (IsOpeningParenthesis(item) || IsClosingParenthesis(item))
                {

                    if (!string.IsNullOrEmpty(Temp))
                    {
                        Result.Add(Temp);
                        Temp = string.Empty;
                    }
                    Result.Add(character.ToString());

                }
                else if (IsWhiteSpace(item))
                {
                    if (!string.IsNullOrEmpty(Temp))
                    {
                        Result.Add(Temp);
                        Temp = string.Empty;
                    }
                    continue;
                }
                else if (IsDigitOrDot(item))
                {
                    Temp += character;
                }
                else if (IsOperation(item))
                {

                    if (!string.IsNullOrEmpty(Temp))
                    {
                        Result.Add(Temp);
                        Temp = string.Empty;
                    }
                    Result.Add(character.ToString());

                }
                else
                {
                    throw new Exception("UnKnown Character");
                }

            }
            if (Temp.Length > 0)
            {
                Result.Add(Temp);
            }
            return Result;
        }


        private List<string> ConvertToPostFix(List<string> Formula)
        {
            Stack<string> OperationStack = new Stack<string>();
            List<string> PostFixList = new List<string>();

            for (int index = 0; index < Formula.Count; index++)
            {
                var Item = Formula.ElementAt(index);

                if (IsOpeningParenthesis(Item))
                {
                    OperationStack.Push(Item);

                }
                else if (IsClosingParenthesis(Item))
                {
                    string search = "";
                    while (OperationStack.Count > 0 && !IsOpeningParenthesis(search))
                    {
                        search = OperationStack.Pop();
                        if (!IsOpeningParenthesis(search))
                        {
                            PostFixList.Add(search);

                        }
                    }

                }
                else if (IsOperation(Item))
                {
                    if (IsPower(Item))
                    {
                        PostFixList.Add(Formula.ElementAt(index + 1));
                        PostFixList.Add(Item);
                        index++;
                        continue;
                    }
                    else
                    {
                        while (OperationStack.Count != 0 && !CanPushToStack(Item, OperationStack.Peek()))
                        {
                            PostFixList.Add(OperationStack.Pop());
                        }
                        OperationStack.Push(Item);
                    }
                }
                else
                {
                    // must be number
                    PostFixList.Add(Item);
                }

            }


            while (OperationStack.Count > 0)
            {
                PostFixList.Add(OperationStack.Pop());
            }

            return PostFixList;
        }


        private double ExecuteFormula(List<string> Formula)
        {
            Stack<double> ResultStack = new Stack<double>();
            foreach (var Item in Formula)
            {
                if (IsOperation(Item))
                {
                    var ScoundOp = ResultStack.Pop();
                    var FirstOp = ResultStack.Pop();
                    ResultStack.Push(ExecuteOperation(FirstOp, Item.ElementAt(0), ScoundOp));
                }
                else
                {
                    ResultStack.Push(double.Parse(Item));
                }
            }

            if (ResultStack.Count == 1)
            {
                return ResultStack.Pop();
            }

            throw new Exception("Something Went Wrong");


        }

        private int GetPriority(char Operation)
        {

            switch (Operation)
            {
                case '+':
                    return 1;
                case '-':
                    return 1;
                case '*':
                    return 2;
                case '/':
                    return 2;
            }

            throw new Exception("Unknown Operation");
        }
        private double ExecuteOperation(double FirstOperator, char Operation, double ScoundOperator)
        {
            switch (Operation)
            {
                case '+':
                    return FirstOperator + ScoundOperator;
                case '-':
                    return FirstOperator - ScoundOperator;
                case '*':
                    return FirstOperator * ScoundOperator;
                case '/':
                    return FirstOperator / ScoundOperator;
                case '^':
                    return Math.Pow(FirstOperator, ScoundOperator);
                case 'R':
                    if (ScoundOperator == 0)
                        return 1;
                    return Math.Pow(FirstOperator, (1 / ScoundOperator));
            }

            throw new Exception("Unknown Operation");


        }


        #region checkers
        private bool CanPushToStack(string NewItem, string OldItem)
        {
            if (OldItem == "(")
            {
                return true;
            }
            return GetPriority(NewItem.ElementAt(0)) > GetPriority(OldItem.ElementAt(0));
        }

        private bool IsOpeningParenthesis(string Item)
        {

            return Item.Length == 1 && Item.ElementAtOrDefault(0) == '(';
        }


        private bool IsClosingParenthesis(string Item)
        {
            return Item.Length == 1 && Item.ElementAtOrDefault(0) == ')';
        }



        private bool IsDigitOrDot(string Item)
        {
            if (Item.Length != 1)
                return false;
            char c = Item.ElementAtOrDefault(0);
            return (c >= '0' && c <= '9') || c == '.';
        }

        private bool IsPower(string Item)
        {
            return Item.Length == 1 && Item.ElementAtOrDefault(0) == '^';
        }

        private bool IsWhiteSpace(string Item)
        {
            return Item.Length == 1 && Item.ElementAtOrDefault(0) == ' ';
        }

        private bool IsOperation(string Item)
        {
            char c = Item.ElementAtOrDefault(0);
            return Item.Length == 1 && (c == '*' || c == '/' ||
                                        c == '-' || c == '+' ||
                                        c == '^' || c == 'R');
        }

        private async Task<decimal> ConvertToDefaultAsync(decimal value, long CurrencyRatesId)
        {

            var CurrencyRate = await _CurrencyRatesService.GetCurrencyById(CurrencyRatesId);
            return CurrencyRate == null ? value : value * CurrencyRate.Rate;
        }

        #endregion



        #endregion
    }
}
