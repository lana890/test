

using System.Collections.Generic;

namespace EskaCMS.Infrastructure.Models.ElasticVM;

public class NaviagtableSearchModel
{
    public long Id { get; set; }
    public string ElasticId { get; set; }
    public string EntityType { get; set; }
    public long SiteId { get; set; }

    public List<Details> Details { get; set; }
}

public class Details
{
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }
    public string Specification { get; set; }
    public string MetaDescription { get; set; }
    public string addtionalMetaTags { get; set; }
    public string Slug { get; set; }
    public string Keywords { get; set; }
    public string ThumbnailImageUrl { get; set; }
    public string BranchId { get; set; }
    public string CountryId { get; set; }
    public long CultureId { get; set; }
    public long Id { get; set; }
}

public class NaviagtableSearchOutputModel 
{
    public int TotalCount { get; set; }
    public List<Details> ListItems = new List<Details>();
}

public class ProductSearchOutputModel
{
    public int TotalCount { get; set; }
    public List<ProductElasticModel> ListItems = new List<ProductElasticModel>();
}

public class ProductElasticModel
{
    public long Id { get; set; }
    public string ElasticId { get; set; }
    public string EntityType { get; set; }
    public long SiteId { get; set; }
    public long CultureId { get; set; }
    public EDetails ProductDetails { get; set; }
    public List<EProductAttribute> Attribute { get; set; }
    public EBrandDetails BrandDetails { get; set; }
    public List<ECategoryDetails> CategoryDetails { get; set; }
    public List<EDetails> ProductLink { get; set; }

}
public class DataCulutre 
{
    public long CultureId { get; set; }
    public EDetails ProductDetails { get; set; }
    public List<EProductAttribute> Attribute { get; set; }
    public EBrandDetails BrandDetails { get; set; }
    public List<ECategoryDetails> CategoryDetails { get; set; }
    public List<EDetails>  ProductLink { get; set; }


}
public class EDetails 
{
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }
    public string Specification { get; set; }
    public string addtionalMetaTags { get; set; }
    public string Slug { get; set; }
    public string ThumbnailImageUrl { get; set; }
    public string BranchId { get; set; }
    public string CountryId { get; set; }
}
public class ECategoryDetails
{
    public long CategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}


public class EBrandDetails 
{
    public long BrandId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
public class EProductAttribute
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
  
}

public class ProductAttributeDetails 
{
    public string Name { get; set; }
    public long CulutreId { get; set; }
}

public class ProductAttributeValue
{
    public long Id { get; set; }
    public List<ProductAttributeValueDetails> AttributeValueDetails { get; set; }
}
public class ProductAttributeValueDetails 
{
    public string Value { get; set; }
    public long CulutreId { get; set; }
}