

using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.ViewModels;
using System;
using System.Collections.Generic;
using EskaCMS.Entities.Entity.Core;

using static EskaCMS.Entities.Enum.GeneralEnums;

namespace BusinessModels
{
    public class BusinessModel
    {

        public class UserViewModel
        {
            public long UserId { get; set; }
            public string Email { get; set; }
            public string UserName { get; set; }
            public string FullName { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
            public string PhoneNumber { get; set; }
            public List<string> RoleName { get; set; }
            public string Image { get; set; }
            public List<string> UserGroup { get; set; }
            public List<long> RoleId { get; set; }
            public long CustomerGroupId { get; set; }
            public EStatus Status { get; set; }
            public string    StatusDesc { get; set; }
            public DateTimeOffset CreationDate { get; set; }
            public string CreatedBy { get; set; }
            public DateTimeOffset? LastLogin { get; set; }
            public bool IsLockout { get; set; }
            public string PublicURL { get; set; }
            public ICollection<UserOptions> UserOptions { get; set; }
            // public IList<int> AssignedSites { get; set; }
            // public int GroupId { get; set; }

        }

        public class ExportUsersData
        {
       
            public string Email { get; set; }
          
            public string FullName { get; set; }
            public string PhoneNumber { get; set; }

            public string Nationality { get; set; }
            public string TShirt_Size { get; set; }
            public string Blood_Type { get; set; }
            public string Register_Date { get; set; }
            public string University { get; set; }
            public string Gender { get; set; }
            public string National_Id { get; set; }
            public string Address { get; set; }
            public int Day_Of_Birth { get; set; }
            public int Moth_Of_Birth { get; set; }
            public int YearOfBirth { get; set; }
            public string Worked_With_Us_Before { get; set; }
            public string Event_Name { get; set; }
        }




        public class CultureViewModel
        {
            public string Culture { get; set; }
            public string Name { get; set; }
            public long CurrencyId { get; set; }
        }







        public class TemplateSettingsModel
        {
            public long TemplateId { get; set; }
            public string Settings { get; set; }
            public string Css { get; set; }
            public string Scripts { get; set; }
            public string Alias { get; set; }
            public bool IsDraft { get; set; }
            public List<EskaCMS.Entities.Entity.ImportedFilesVM> ImportFilesList { get; set; }
            public long UserId { get; set; }
            public List<string> TemplateComponents { get; set; }


        }




        //public class PostCategoryViewModel
        //{
        //    public long Id { get; set; }
        //    public string Name { get; set; }
        //    public string Name2 { get; set; }

        //    public long CreatedById { get; set; }
        //    public long SiteId { get; set; }
        //    public DateTimeOffset CreationDate { get; set; }
        //    public long? ModifiedById { get; set; }
        //    public DateTimeOffset? ModificationDate { get; set; }


        //}
        public class DuplicatePostCategoryViewModel
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Name2 { get; set; }

            public long CreatedById { get; set; }
            public long SiteId { get; set; }
            public DateTimeOffset CreationDate { get; set; }
            public long? ModifiedById { get; set; }
            public DateTimeOffset? ModificationDate { get; set; }

            //  public List<PostCategories> Posts { get; set; }
        }
        public class TagsTitleAndId
        {
            public long? Id { get; set; }
            public string Title { get; set; }

        }
        public class postMediaWithUrl
        {
            public long? Id { get; set; }
            public string Url { get; set; }

        }



        public class TypesSettingsViewModel
        {

            public EskaCMS.Entities.Enum.GeneralEnums.ESettingsTypes Type { get; set; } /*1 Page*/
            public long PageId { get; set; }
            public long PostId { get; set; }

            public string MetaDescription { get; set; }

            public string MetaKeywords { get; set; }

            public string RedirectUrl { get; set; }
            public Int16 RedirectType { get; set; }
            public string Route { get; set; }


        }





        public class CustomerGroupVM
        {
            public long CustomerGroupId { get; set; }
            public List<UserViewModel> Users { get; set; }
        }







        public class SiteSlugsVM
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
            public SlugsTypes Type { get; set; }
            public string Components { get; set; }
            public IEnumerable<string> Defultslug { get; set; }
        }




















        public class GetFolderContentVM
        {
            public string folderPath { get; set; }
        }

        public class CreateFolderVM
        {
            public string FolderName { get; set; }
            public string Path { get; set; }
            public DateTime Modificationdate { get; set; }
        }

        public class MoveFiles
        {
            public string OldPath { get; set; }
            public string NewPath { get; set; }
            public List<string> FilesNames { get; set; }
        }



        public class AllUsersByRoleResultViewModel
        {
            public long Count { get; set; }

            public List<UserViewModel> List { get; set; }

        }




    }
}
