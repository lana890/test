using System;

namespace EskaCommerce.ProductComparison.Services
{
    public class TooManyComparingProductException : Exception
    {
        public TooManyComparingProductException(int maxNumComparingProduct)
        {
            MaxNumComparingProduct = maxNumComparingProduct;
        }

        public int MaxNumComparingProduct {get; private set; }
    }
}
