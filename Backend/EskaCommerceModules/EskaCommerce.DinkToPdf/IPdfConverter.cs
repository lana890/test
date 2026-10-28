using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.DinkToPdf
{
    public interface IPdfConverter
    {
        byte[] Convert(string htmlContent);
    }
}
