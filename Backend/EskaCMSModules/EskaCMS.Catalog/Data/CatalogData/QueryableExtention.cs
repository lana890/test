using System;
using System.Linq;
using System.Linq.Expressions;
using static EskaCMS.Entities.Enum.GeneralEnums;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySort<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        ESortType sortType)
    {

        return sortType == ESortType.asc
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);
    }
}