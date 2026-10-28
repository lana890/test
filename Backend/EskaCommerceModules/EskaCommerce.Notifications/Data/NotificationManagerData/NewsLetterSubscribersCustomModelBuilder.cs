using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.NotificationManager.Data
{
    public class NewsLetterSubscribersCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<NewsLetterSubscribers>();

            //modelBuilder.Entity<NewsLetterSubscribers>().HasOne(x => x.User)
            //    .WithMany(x => x.NewsLetterSubscribers)
            //    .HasForeignKey(x => x.UserId);

            //modelBuilder.Entity<NewsLetterSubscribers>().HasOne(x => x.NewsLetter)
            //.WithMany(x => x.SentTo)
            //.HasForeignKey(x => x.NewsLetterId);

        }
    }
}
