using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using MailKit.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Search
{
    public static class QuerableExtensions
    {


        public static IQueryable<Page> FilterPage(this IQueryable<Page> Pages, SearchFilterInputVM searchFilter, bool exactMatchesOnly, bool searchInTitle, bool searchInDescription, bool searchInContent, string inputwithTrim)
        {

            var ToUpperSearch = inputwithTrim == null ? null : inputwithTrim.ToUpper();

            if (searchFilter.SubSearchOptions.Count > 0)
            {
                Pages = Pages.Where(x =>

                        (searchInTitle && x.Name.ToLower().Contains(inputwithTrim))
                     || (searchInDescription && x.Description.ToLower().Contains(inputwithTrim))
                     || (exactMatchesOnly &&

                         (x.Name.ToLower().Equals(inputwithTrim)
                        || x.NavigationTitle.ToLower().Equals(inputwithTrim)
                        || x.Description.ToLower().Equals(inputwithTrim))
                       )
                     || (searchInContent &&
                        (x.PageVersions.OrderByDescending(p => p.VersionId).First().SettingsBody.Contains(inputwithTrim) || x.PageVersions.OrderByDescending(p => p.VersionId).First().SettingsBody.Contains(ToUpperSearch)))

                     );

            }
            else
            {
                Pages = Pages.Where(x => (
                                 x.Name.ToLower().Contains(inputwithTrim)
                                 || x.Description.Contains(inputwithTrim)
                                 || (x.PageVersions.OrderByDescending(p => p.VersionId).First().SettingsBody.Contains(inputwithTrim) || x.PageVersions.OrderByDescending(p => p.VersionId).First().SettingsBody.Contains(ToUpperSearch))));

            }

            return Pages;

        }


        public static IQueryable<ProductDetails> FilterProducts(this IQueryable<ProductDetails> products, SearchFilterInputVM searchFilter, bool exactMatchesOnly, bool searchInTitle, bool searchInDescription, bool searchInContent, bool searchInAttribute, long? selectedAttributeId, string inputwithTrim)
        {

            if (!string.IsNullOrEmpty(inputwithTrim))
            {
                if (searchFilter.SubSearchOptions.Count > 0)
                {



                    bool isSearchInVariation = searchFilter.SearchOptionId == (int)Entities.Enum.GeneralEnums.SearchOptions.SearchInVariation;

                    products = products.Where(x => x.Description != null)
                        .Where(x =>
                            (exactMatchesOnly &&
                                (x.Name.ToLower().Equals(inputwithTrim) ||
                                 x.Description.ToLower().Equals(inputwithTrim)))
                            || (searchInTitle && x.Name.ToLower().Contains(inputwithTrim))
                            || (searchInAttribute && x.Product.AttributeValues.Any(attr =>
                                    (!selectedAttributeId.HasValue || attr.AttributeId == selectedAttributeId.Value) &&
                                    attr.AttributeValueDetails.Any(val => val.Value.ToLower().StartsWith(inputwithTrim))))
                            || (searchInDescription && x.Description.ToLower().Contains(inputwithTrim))
                        );

                    if (isSearchInVariation)
                    {
                        products = products
                            .Where(x =>
                                x.Name.ToLower().Contains(inputwithTrim) ||
                                x.Description.ToLower().Contains(inputwithTrim) ||
                                x.ShortDescription.ToLower().Contains(inputwithTrim) ||
                                x.Specification.ToLower().Contains(inputwithTrim))
                            .Where(x =>
                                x.Product.LinkedProductLinks.Count() == 0 ||
                                x.Product.LinkedProductLinks.Any(p =>
                                    p.LinkedProductId == x.ProductId &&
                                    p.LinkType == ItemLinkType.Related))
                            .Where(x =>
                                x.Product.ProductLinks.Count() == 0 ||
                                x.Product.ProductLinks.Any(p => p.LinkedProductId != x.ProductId));
                    }
                    else
                    {
                        products = products
                            .Where(x =>
                                x.Name.ToLower().Contains(inputwithTrim) ||
                                x.Description.ToLower().Contains(inputwithTrim) ||
                                x.ShortDescription.ToLower().Contains(inputwithTrim) ||
                                x.Specification.ToLower().Contains(inputwithTrim))
                            .Where(x =>
                                x.Product.LinkedProductLinks.Count() == 0);
                    }
                }
            }

            return products;

        }

        public static IQueryable<Posts> FilterPosts(this IQueryable<Posts> posts, SearchFilterInputVM searchFilter, bool exactMatchesOnly, bool searchInTitle, bool searchInDescription, bool searchInContent, string inputwithTrim)
        {

            if (searchFilter.SubSearchOptions.Count > 0)
            {
                posts = posts.Where(x =>
                                                (exactMatchesOnly
                                                && (x.Title.ToLower().Equals(inputwithTrim)
                                                || x.ShortDescription.ToLower().Equals(inputwithTrim)
                                                || x.Description.ToLower().Equals(inputwithTrim))
                                                )
                                                || ((searchInTitle)
                                                && x.Title.ToLower().Contains(inputwithTrim))

                                                || (searchInDescription
                                                && x.Description.ToLower().Contains(inputwithTrim))

                                                || (searchInContent
                                                && x.PostContent.ToLower().Contains(inputwithTrim))
                                                );
            }
            else
            {
                posts = posts.Where(x => (x.Title.ToLower().Contains(inputwithTrim) || x.Description.ToLower().Contains(inputwithTrim)));
            }

            return posts;
        }


    }
}
