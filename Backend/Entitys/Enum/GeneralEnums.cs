using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EskaCMS.Entities.Enum
{
    public class GeneralEnums
    {

        public enum SortType
        {
            UserName = 1,
            BrowserType = 2,
            Role = 3,
            PeriodTime = 4
        }

        public enum WarehouseWorkingDay
        {
            Sunday = 1,
            Monday = 2,
            Tuesday = 3,
            Wednesday = 4,
            Thursday = 5,
            Friday = 6,
            Saturday = 7
        }


        public enum GeoLocationEnum
        {

            Area = 3,
            Country = 1,
            City = 2

        }
        public enum EDimensionType
        {
            DependOnProduct = 1,
            DependOnCarton = 2
        }

        public enum ESearchType
        {

            BirthDate = 3,
            NewEmployees = 1,
            AnniversaryEmployees = 2

        }
        public enum ItemLinkType
        {
            Super = 1, // Variations

            Related = 2,

            CrossSell = 3,

            UpSell = 4,
            Sub= 5 // what's inside
        }
        public enum EComponentType
        {
            Web = 1,
            Builder = 2,
        }
        public enum ETemplateTypes
        {
            Page = 1,
            Post = 2,
            Category = 3,
            Brand = 4,
            Product = 5

        }

        public enum EIntegerationType
        {
            FTTH = 1,
            LTE = 2
        }

        public enum EAssetsType
        {
            CSS = 1,
            JS = 2,
            TXT = 3
        }

        public enum ESettingsTypes
        {
            Page = 1,
            Post = 2,
        }
        public enum EStatus
        {
            Unpublished = 1,
            Published = 2,
            Deleted = 3,
            Inactive = 4,

            Active = 5,
            All = 6
        }
        public enum EProductSerialStatus
        {
            Available = 1,
            Taken = 2,
            Reserved = 3,
            Deleted = 4
        }

        public enum ChangePasswordHistory
        {
            ChangePassword = 1,
            forgetPassword = 2,
            Expire = 3
        }
        public enum ProductSearchSortFieldsEnum
        {
            Description = 1,
            ShortDesciption = 2,
            Specification = 3,
            Slug = 4,
            Name = 5,
            FromMin = 6,
            FromMax = 7,
            CreationDate = 8,
            Popularity=9,

            Offer=10,
            MostOrdered=11

        }
        public enum ProductSerialSearchSortFieldsEnum
        {
            CardNo = 1,
            ControlNo = 2,
            BatchNo = 3,
            Balance = 3,
            Expiry = 4,
            Status = 5
        }
        public enum ProductSerialSearchFieldsEnum
        {
            CardNo = 1,
            CategorId = 2,
            ProductId = 3,
            Time = 4,
            Status = 5,
            BatchNo = 6
        }
        public enum ESortType
        {
            none=0,
            asc = 1,
            desc = 2,
        }
        public enum EUsersTypes
        {
            Admin = 1,
            Customer = 2,
            GuestClient = 3,
            Integration=4
        }
        public enum LoginType
        {
            DirectLogin = 1,
            InDirectLogin = 2,
            PortalLogin = 3,
            MobileLogin = 4,
            ExternalLogin = 5
        }

        public enum EControlTypes
        {
            Text = 0,
            Tel = 1,
            Password = 2,
            Email = 3,
            Date = 4,
            File = 5,
            RadioButton=6,
            DropDownList=7
        }
        public enum ERoleTypes
        {
            Admin = 1,
            Designer = 2,
            BlogPosts = 3,
            Guest = 4,
            ECommerce = 5,
            Customer = 6,
            Vendor = 7,
            Auther = 10,
            Crawler=11

        }


        public enum FillterType
        {
            UserRole = 1,
            Name = 2,
            Usergroup = 3
        }

        public enum EGenderTypes
        {
            Male = 1,
            Female = 2,
            Other = 3,
        }
        public enum EDefinitionType
        {


            [Description("System Defined")]
            SystemDefined = 1,
            UserDefined = 2


        }
        public enum ERoleValue
        {
            Off = 1,
            On = 2

        }

        public enum ETypeLogin
        {
            Admin = 1,
            Web = 2,
            Mobile = 3 
        }
        public enum ERequestStatus
        {
            Pending = 1,
            Approved = 2,
            Rejected = 3,
            Deleted = 4
        }
        public enum ECategoryTypes
        {
            Menu = 1,
            Product = 2,
            Component = 3,
            AdminMenu = 4,
            Page = 5,
            RoleOptions = 6,
            Post = 7,
            Events = 8,
            PricingQuotation = 9,
            ToDoList = 10,
            ComponentRole=15
        }
        public enum ECategory
        {
            CommonComponents = 1,
            Ecommerce = 2,
            SiteOptions = 3,
            PageOptions = 4,
            ComponentOptions = 5,
            LayoutComponents = 6,
            SystemComponents = 7,
            ContentComponents = 8,
            WebArtComponents = 9,
            TemplateOptions = 10,
            MediaManagerOptions = 11,
            GraphicProfileOptions = 12,
            MenusOptions = 13,
            DCMSAdminMenu = 14,
            ViewsOptions = 15,
            ContentOptions = 16,
            SystemOptions = 17,
            UsersAndPermissionsOptions = 18,
            PostsOptions = 19,
            StaticMenuOptions = 20,
            MainMenuOptions = 21,
            Main = 22,
            EcommerceComponents = 23,
            DashboardOptions = 24,
            Charts = 25,
            Gallery = 26,
            InsuranceComponents = 27,
            CatalogOptions = 28,
            SalesOptions = 29,
            PromotionsOptions = 30,
            InventoryOptions = 31,
            VendorsOptions = 32,
            DiscountsOptions = 33,
            TaxOptions = 34,
            PlansAndSubscriptionsOptions = 35,
            ReviewsAndCommentsOptions = 36,
            EcommerceOptions = 37,
            EventsOptions = 67,
            AnnualMeetings = 68,
            Conferences = 69,
            Birthdays = 70,
            SharedElements = 71,
            SmtpOption = 72
           }
        public enum CoreNotificationType
        {
            PushNotification = 1,
            WebNotification = 2,
            Email = 3,
            SMS = 4
        }
        public enum IntervalType
        {
            Date = 1,
            Period = 2,
        }
        public enum BGPeriodType
        {
            Daily = 1,
            Weekly = 2,
            Monthly = 3,
            Yearly = 4,
            ByTime = 5,
            WeekOfMonth = 6,
            MonthOfYear = 7,
            DayOfWeek = 8
        }
        public enum EEntity
        {
            Product = 1,
            Post = 2,
            Category = 3,
            Page = 4,
            Brand = 5,
            None=6
        }

        public enum OS
        {
            Web = 1,
            Android = 2,
            IOS = 3
        }
        public enum EOptions
        {
            CES = 1,
            CESTYLE = 2,
            AO = 3,
            DASH = 4,
            TMPL = 5,
            PAGS = 6,
            BlgsArt = 7,
            SRVY = 8,
            MEMGER = 9,
            MENUS = 10,
            THEME = 11,
            USRAPER = 12,
            RELSMANGR = 13,
            STS = 14,
            DTSRC = 15,
            USRS = 16,
            RLE = 17,
            RLSMANGE = 18,
            CSPD = 19,
            COPB = 20,
            CEP = 21,
            CPUP = 22,
            CEPT = 23,
            CDUP = 24,
            CDP = 25,
            CANP = 26,
            CVP = 27,
            CVBP = 28,
            CANBP = 29,
            CEBP = 30,
            CEBPT = 31,
            CDBP = 32,
            CANSM = 33,
            CVSM = 34,
            CESM = 35,
            CPUSM = 36,
            CDSM = 37,
            CESMT = 38,
            CANT = 39,
            COBT = 40,
            CDUT = 41,
            CET = 42,
            CDT = 43,
            CANF = 44,
            CUF = 45,
            CUSF = 46,
            CUMF = 47,
            CVAF = 48,
            CVIT = 49,
            CVVT = 50,
            CVDT = 51,
            CVFT = 52,
            CCFL = 53,
            CVIE = 54,
            CDF = 55,
            CASOST = 56,
            CASOPS = 57,
            CASOTS = 58,
            CASOCS = 59,
            CANST = 60,
            CVST = 61,
            CACOST = 62,
            CECOST = 63,
            CDCOST = 64,
            CVCOST = 65,
            CVFOST = 66,
            CAFOST = 67,
            CEFOST = 68,
            CDFOST = 69,
            CVCST = 70,
            CACST = 71,
            CDCST = 72,
            CECST = 73,
            CASST = 74,
            CRST = 75,
            CDST = 76,
            CSSTAD = 77,
            CDSTE = 78,
            CANMC = 79,
            CEMC = 80,
            CDUMC = 81,
            CDMC = 82,
            CANM = 83,
            CACM = 84,
            CPURM = 85,
            CERM = 86,
            CDRM = 87,
            CPUCM = 88,
            CECM = 89,
            CDCM = 90,
            CANS = 91,
            CDS = 92,
            CSSAD = 93,
            CESR = 94,
            SUAC = 95,
            SGVC = 96,
            SCSSC = 97,
            SJSC = 98,
            SJSONC = 99,
            CASTU = 100,
            CAEC = 101,
            CATP = 102,
            CATC = 103,
            CATO = 104,
            CATPP = 105,
            CATPAG = 106,
            CATPA = 107,
            CATPT = 108,
            CATS = 109,
            CATCPR = 110,
            CATCPRU = 111,
            CATSI = 112,
            CATWI = 113,
            CATMWPI = 114,
            CATVEN = 115,
            CATDIS = 116,
            CATTC = 117,
            CATTR = 118,
            CATPL = 119,
            CATPPV = 120,
            CATCM = 121,
            CATSM = 122,
            CATPM = 123,
            CATIM = 124,
            CATVM = 125,
            CATDM = 126,
            CATTM = 127,
            CATPLM = 128,
            CATPTS = 129,
            CATUP = 130,
            CATPS = 131,
            CATUB = 132,
            CATProv = 133,
            CATDrivers = 134,
            CATRC = 135,
            CATB = 136,
            CATPV = 137,
            CATD = 138,
            SUFRMS = 139,
            CALR = 140,
            CATRAC = 141,
            CATREV = 142,
            CATREVR = 143,
            CATCOM = 144,
            CALD = 146,
            CAUS = 147,
            CAES = 148,
            CAESS = 149,
            CEES = 150,
            CAPS = 151,
            CVS=155,
            CVB=156,
            CANCAT=157,
            CANPROD=158,
            CANINV=159,
            CSER=174,
            CSCAT=161,
            CSET=162,
            CSDO=163,
            CSNL=164,
            CSCR=165,
            CSES=166,
            CSCN=167,
            CSCT=168,
            USRNT = 170,
            USRSN = 171,
            USRWH = 172,
            USRG = 173,
            CRS=180,
            CRES=181,
            CATLOY = 182,
            CASCS=183,
            CADDSCS=184,
            CDELETESC = 185,
            CEDITSCS=186,
            CATWF=187,
            CPW = 200,
            CDPDT=201,
            CEPDT=202,
            CDNS=203,
            CUPTS = 204,
            DFRMS=205
        }
        public enum EMenuTypes
        {
            URL = 1,
            Page = 2,
            Post = 3,
            Product = 4,
            None = 5,
            Category = 6,
            Brand = 7,


        }
        public enum EMenuRelation
        {

            All = 1,
            Tree = 2,
            Subilings = 3,
            Children = 4,


        }

        public enum SourceMenuType
        {
            Admin = 1,
            Web = 2,
            Mobile=3
        }
        public enum ApihttpMethods
        {
            POST = 1,
            GET = 2,
            PUT = 3,
            DELETE = 4,
            Patch=5
        }

        public enum EPlatformSource
        {
            Unknown =0,
            Admin = 1,
            Web = 2,
            Mobile = 3
        }


        public enum ApiTypes
        {
            WebApi = 1,
            WCF = 2,
            ASMX = 3,
            Other = 4
        }

        public enum ApiAuthorizationTypes
        {
            NoAuth = 0,
            BasicAuth = 1,
            BearerToken = 2,
            OAuth10 = 3,
            OAuth20 = 4,
            Cookies = 5,

        }

        public enum ETokenSource
        {
            LocalStorage = 1,
            DataBase = 2,
            Cookie = 3,
         
        }

        public enum SlugsTypes
        {
            Page = 1,
            Blog = 2,
            Brand = 3,
            Category = 4,
            Product = 5,
        }
        public enum ParamsTypes
        {
            Header = 1,
            Body = 2,
            QueryString = 3,
            Path = 4,
            BodyArray = 5,
            FormFile = 6,
            FormData = 7
        }

        public enum ApiReponseType
        {
            Array = 1,
            Object = 2,
            String = 3,
            Number = 4, File = 5,

        }
        public enum SourceType
        {
            Output = 0,
            Static = 1,
            RouterParameter = 2,
            LocalStorage = 3,
            SessionStorage = 4,
            User = 5,
            ResultData = 6,
            Control = 7,
            File = 8,
            ControlOrDefault = 9,
            TokenClaim=10,
			DependOnOtherApi = 11

		}
		public enum EUserPlansStatus
        {
            WaitingPayment = 1,
            Subscribed = 2,
            Canceled = 3,
            Expired = 4,
            Recurring = 5,
            Inactive = 6,
            Refunded = 7,
            Scheduled = 8

        }

        public enum WeekDays
        {
            Sunday = 0,
            Monday = 1,
            Tuseday = 2,
            Wednesday = 3,
            Thursday = 4,
            Friday = 5,
            Saturday = 6,
            All = 7
        }
        public enum PlanTypes
        {
            Trial = 0,

            [Description("One Month Plan")]
            OneMonthPlan = 1,
            [Description("Three Month Plan")]
            ThreeMonthPlan = 3,

        }
        public enum RecurringTypes
        {
            Recurring = 1,
            [Description("One Time")]
            OneTime = 2
        }
        public enum SmartVisionReposneCodes
        {
            sender_id_not_found = 1002,
            API_Key_not_found = 1003,
            smap_word_detected = 1004,
            internal_error = 1005,
            internal_error2 = 1006,
            low_balance = 1007,
            msg_not_set = 1008,
            sms_type_not_set = 1009,
            invalid_user_and_password = 1010,
            invalid_user_id = 1011,
            Success = 200
        }
        public enum E2FAProviders
        {
            Phone = 1,
            Email = 2,
            WhatsApp=3
        }
        public enum DiscountTiersTypes
        {

            FixedAmount = 1,
            Percentage = 2,


        }
        public enum DiscountTypes
        {

            FixedAmount = 1,
            Percentage = 2,
            Tiers = 3

        }

        public enum UserBookingStatus
        {
            Initiated = 0,
            Booked = 1,
            OnTheWay = 2,
            ServiceStarted = 3,
            ServiceCompleted = 4,
            Expired = 5
        }
        public enum LogTypes
        {
            Information = 1,
            Warning = 2,
            Error = 3
        }

        public enum CartTypes
        {
            Shop = 1,
            Plan = 2,
            Custom = 3
        }

        public enum SMSProviders
        {
            SMSZainJordan = 1,
            SMSGlobal = 2,
            SMSMashastra = 3,
            SMSUniFonic = 4,
            SMSVision = 5,
            SMSMPP = 6,
            SMSJorMall=7
        }
        public enum WhatsAppProviders
        {
            Infoseed = 1
        }

        public enum LoginTypes
        {
            UserName,
            Email,
            PhoneNumber,
            AdditionInfo,
            UsernameOrEmail,
            UsernameOrPhone,
            PhoneOrEmail
        }
        public enum EResponseType
        {
            Text=1,
            PDF=2,
            Excel=3
        }
        /*
           {
        id: "1",
        name: "Exact matches only",
        selected: false,
        color: "primary",
      },
      { id: "2", name: "Search in title", selected: false, color: "primary" },
      { id: "3", name: "Search in content", selected: false, color: "primary" },
      { id: "4", name: "Search in excerpt", selected: false, color: "primary" },
      { id: "5", name: "Search in posts", selected: false, color: "primary" },
      { id: "6", name: "Search in pages", selected: false, color: "primary" },
      {
        id: "7",
        name: "Search in products",
        selected: false,
        color: "primary",
      },
         */
        #region DynamicForms Enums

        public enum EDynamicFormsCriteriaTypes
        {
            Condition = 1,
            Operation = 2,
        }

        public enum EDynamicCriteriaInputTypes
        {
            Text = 1,
            Number = 2,
            Currency = 3,
            Option = 4,
            Date = 5,
            Email = 6,
            PhoneNumber = 7,
            Attachment = 8,
            TextArabic = 9,
            TextEnglish = 10
        }
        public enum FormItemType
        {
            PricingQuotation = 1,
            Survey = 2,
            Workflow = 3,

        }

        public enum Type
        {
            Text = 1,
            Number = 2,
            Currency = 3,
            Option = 4,
            Date = 5,
            Email = 6,
            PhoneNumber = 7,
            Attachment = 8,
            TextArabic = 9,
            TextEnglish = 10
        }
        public enum EDynamicCriteriaOperations
        {
            UsePercentageDiscount = 1,
            UseMoneyAmountDiscount = 2,
            ShowValue = 3,
            UseFormula = 4,
        }

        public enum EDynamicCriteriaConditions
        {
            Equals = 1,
            MoreThan = 2,
            LessThen = 3,
            NotEquals = 4,
            LessThenOrEquals = 5,
            MoreThenOrEquals = 6,
            EqualsOneOf = 7,
        }
        public enum EDynamicFormsOneOfOptions
        {
            Manual = 1,
            DataSource = 2,
            StaticApi = 3
        }

        #endregion
        public enum SearchOptions
        {
            SearchInAll = 1,
            SearchInPosts = 2,
            SearchInPages = 3,
            SearchInProducts = 4,
            SearchInVariation = 5,
        }


        public enum SearchSubOptions
        {
            ExactMatchesOnly = 1,
            SearchInTitle = 2,
            SearchInContent = 3,
            SearchInDescription = 4,
            SearchInAttribute = 5

        }

        public enum AttributeSearchCriteria
        {
            AttributeContains = 1,
            AttributeStartWith = 2,

        }
        public enum CultureType
        {
            SubDomain = 1,
            Directory = 2


        }
        public enum UserLoggingType
        {
            LogginSuccess = 1,
            loginFailed = 2,
            LogoutNormal = 3,
            LogoutTimeout = 4,
            loginLocked = 5,

        }



        public enum OrderByEnum
        {
            Date = 1,
            Alphabetical = 2,
            AlphabeticalDesc = 5,
            Order = 3,
            Id = 4,

        }

        public enum EResouseScope
        {
            AmongSite,
            AmongSlug,
            AmongGroup,
        }

        public enum EMediaFilter
        {
            NewToOld = 1,
            OldToNew = 2,
            AscendingByOrder = 3,
            DescendingByOrder = 4,
        }

        public enum EProductCountryPermission
        {
            Allowed = 1,
            NotAllowed = 2
        
        }

        public enum EHistoryAction
        {
            Add = 1,
            Update = 2,
            Delete = 3

        }
        public enum ProcessReversal
        {
            Paid= 1,
            Refaund=2

        }

        public enum EUserAvailabilityStatus
        {
            Available = 1,
            Busy = 2

        }

        public enum EProductSearchType
        {
            SearchText =0,
            SearchInAttribute =1
        }
    
    }
}
