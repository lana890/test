using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Pagination
{
    public static class RepoExtentions 

    {
        public static async Task<PaginationResult<T>> PaginateAsync<T>(this IQueryable<T> source, int pageSize, int pageNumber) where T:class
        {
            
            var count = await source.CountAsync();

            var items = await source.Skip((pageNumber) * pageSize).Take(pageSize).ToListAsync();

            
            var TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            
            return new PaginationResult<T>(items, TotalPages, count);
        }
        
        public static async Task<PaginationResult<T>> PaginateAsync<T>(this IEnumerable<T> source, int pageSize, int pageNumber) where T:class
        {
            
            var count =  source.Count();

            var items =  source.Skip((pageNumber) * pageSize).Take(pageSize).ToList();

            
            var TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            
            return new PaginationResult<T>(items, TotalPages, count);
        }
        public static PaginationResult<T> Paginate<T>(this IEnumerable<T> source, int pageSize, int pageNumber) where T : class
        {

            var count = source.Count();

            var items = source.Skip((pageNumber) * pageSize).Take(pageSize).ToList();


            var TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            return new PaginationResult<T>(items, TotalPages, count);
        }
        public static async Task<PaginationResult<T>> PaginateAsyncDistinct<T>(this IQueryable<T> source, int pageSize, int pageNumber) where T : class
        {


            var items = await source.Distinct() 
                                    .Skip((pageNumber) * pageSize).Take(pageSize).ToListAsync();

            var count = await source.CountAsync();

            var TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            return new PaginationResult<T>(items, TotalPages, count);
        }
    }
    }
    
