//using EskaCMS.Entities.Entity;
//using EskaCMS.Entities.Entity.General;
//using EskaCMS.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static EskaCMS.Entities.Enum.GeneralEnums;

//namespace EskaCMS.General.Data
//{
//    public class GeneralCustomModelBuilder : ICustomModelBuilder
//    {
//        public void Build(ModelBuilder modelBuilder)
//        {

//            modelBuilder.Entity<Lookup>().HasIndex(ba => new { ba.Code, ba.CultureId, ba.SiteId }).IsUnique();

//            modelBuilder.Entity<Lookup>().HasSeedData(new Lookup { Id = 1L, Code = "Sector", SiteId = 1, CultureId = 3, Estatus = EStatus.Active, Name = "Sector", IsEditable = true });
//            modelBuilder.Entity<Lookup>().HasSeedData(new Lookup { Id = 2L, Code = "International Calling Rate", SiteId = 1, CultureId = 3, Estatus = EStatus.Active, Name = "International Calling Rate", IsEditable = true });
//            modelBuilder.Entity<Lookup>().HasSeedData(new Lookup { Id = 3L, Code = "????", SiteId = 1, CultureId = 4, Estatus = EStatus.Active, Name = "????", IsEditable = true });


//            modelBuilder.Entity<LookupValues>().HasSeedData(
//                new LookupValues { Id = 170L, Name = "Alaska", Value = "1.49", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 171L, Name = "Albania", Value = "2.63", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 172L, Name = "Algeria", Value = "2.40", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 173L, Name = "American-Samoa", Value = "1.49", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 174L, Name = "Andorra", Value = "3.15", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 175L, Name = "Angola", Value = "3.15", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 176L, Name = "Anguilla", Value = "3.38", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 177L, Name = "Antigua-Barbuda", Value = "3.38", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 178L, Name = "Argentina", Value = "3.15", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 179L, Name = "Armenia", Value = "2.63", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 180L, Name = "Aruba", Value = "3.75", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 181L, Name = "Australia", Value = "3.15", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 182L, Name = "Austria", Value = "1.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 183L, Name = "Azerbaijan", Value = "1.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 184L, Name = "Bahamas", Value = "4.06", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 185L, Name = "Bahrain", Value = "1.01", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 186L, Name = "Bangladesh", Value = "1.56", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 187L, Name = "Barbados", Value = "3.51", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 188L, Name = "Belarus", Value = "3.33", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 189L, Name = "Belgium", Value = "2.03", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 190L, Name = "Belize", Value = "3.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 191L, Name = "Benin", Value = "2.65", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 192L, Name = "Bermuda", Value = "3.51", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 193L, Name = "Bhutan", Value = "2.65", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 194L, Name = "Bolivia", Value = "3.51", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 195L, Name = "Bosnia", Value = "2.73", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 196L, Name = "Botswana", Value = "3.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 197L, Name = "Brazil", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 198L, Name = "British-Virgin-Is", Value = "3.51", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 199L, Name = "Brunei", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 200L, Name = "Bulgaria", Value = "3.51", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 201L, Name = "Burkina", Value = "3.99", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 202L, Name = "Burundi", Value = "3.90", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 203L, Name = "Cambodia", Value = "3.51", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 204L, Name = "Cameroon", Value = "4.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 205L, Name = "Canada", Value = "1.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 206L, Name = "Cape-Verde-Is", Value = "3.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 207L, Name = "Central-Africa", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 208L, Name = "Chad", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 209L, Name = "Chile", Value = "2.09", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 210L, Name = "China", Value = "2.73", lookupId = 2, ParentId = null },






//                new LookupValues { Id = 211L, Name = "Colombia", Value = "3.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 212L, Name = "Comoros", Value = "4.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 213L, Name = "Congo", Value = "4.68", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 214L, Name = "Congo", Value = "4.68", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 215L, Name = "Cook-Island", Value = "4.75", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 216L, Name = "Costa-Rica", Value = "2.65", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 217L, Name = "Croatia", Value = "3.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 218L, Name = "Cuba", Value = "4.37", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 219L, Name = "Cyprus", Value = "2.03", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 220L, Name = "Czech", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 221L, Name = "Denmark", Value = "2.03", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 222L, Name = "Diego", Value = "12.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 223L, Name = "Djibouti", Value = "2.09", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 224L, Name = "Dominica", Value = "3.90", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 225L, Name = "Ecuador", Value = "3.90", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 226L, Name = "Egypt", Value = "1.15", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 227L, Name = "El-Salvador", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 228L, Name = "Equitorial-Guinea", Value = "2.65", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 229L, Name = "Eriteria", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 230L, Name = "Estonia", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 231L, Name = "Ethiopia", Value = "2.65", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 232L, Name = "Falkland", Value = "8.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 233L, Name = "Faroe-Island", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 234L, Name = "Fiji", Value = "4.06", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 235L, Name = "Finland", Value = "2.03", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 236L, Name = "France", Value = "2.03", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 237L, Name = "French-Guiana", Value = "2.03", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 238L, Name = "Gabon", Value = "4.37", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 239L, Name = "Gambia", Value = "Gambia", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 240L, Name = "Georgia", Value = "3.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 241L, Name = "Germany", Value = "2.47", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 242L, Name = "Ghana", Value = "3.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 243L, Name = "Gibralter", Value = "4.75", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 244L, Name = "Greece", Value = "2.47", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 245L, Name = "Greenland", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 246L, Name = "Grenada", Value = "4.06", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 247L, Name = "Guadelope", Value = "2.03", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 248L, Name = "Guam", Value = "1.56", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 249L, Name = "Guatemala", Value = "4.06", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 250L, Name = "Guinea-Bissau", Value = "3.92", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 251L, Name = "Guyana", Value = "4.37", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 252L, Name = "Haiti", Value = "3.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 253L, Name = "Hawai", Value = "1.57", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 254L, Name = "Honduras", Value = "3.16", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 255L, Name = "Hongkong", Value = "1.58", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 256L, Name = "Hungary", Value = "2.47", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 257L, Name = "Iceland", Value = "3.16", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 258L, Name = "India", Value = "1.22", lookupId = 2, ParentId = null },




//                new LookupValues { Id = 259L, Name = "Indonesia", Value = "1.49", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 260L, Name = "Iran", Value = "2.53", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 261L, Name = "Iraq", Value = "1.98", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 262L, Name = "Ireland", Value = "2.77", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 263L, Name = "Italy", Value = "2.08", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 264L, Name = "Ivory  Coast  Spacemob", Value = "8.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 265L, Name = "Ivory-Coast", Value = "3.99", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 266L, Name = "Jamaica", Value = "3.20", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 267L, Name = "Japan", Value = "2.08", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 268L, Name = "Jordan", Value = "1.35", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 269L, Name = "Kazakhistan", Value = "3.83", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 270L, Name = "Kenya", Value = "2.72", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 271L, Name = "Kiribati", Value = "6.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 272L, Name = "Korea-North ", Value = "3.60", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 273L, Name = "Korea-South", Value = "2.72", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 274L, Name = "Kuwait", Value = "1.04", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 275L, Name = "Kyrghyzstan", Value = "4.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 276L, Name = "Laos", Value = "2.72", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 277L, Name = "Latvia", Value = "4.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 278L, Name = "Lebanon", Value = "1.35", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 279L, Name = "Lesotho", Value = "2.72", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 280L, Name = "Liberia", Value = "3.60", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 281L, Name = "Libya", Value = "1.76", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 282L, Name = "Liechtenstan", Value = "2.72", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 283L, Name = "Lithiania", Value = "3.20", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 284L, Name = "Luxembourge", Value = "2.72", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 285L, Name = "Macao", Value = "3.36", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 286L, Name = "Macedonia", Value = "3.08", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 287L, Name = "Malagasy", Value = "3.20", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 288L, Name = "Malawi", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 289L, Name = "Malaysia", Value = "1.54", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 290L, Name = "Maldives", Value = "4.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 291L, Name = "Mali", Value = "3.47", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 292L, Name = "Malta", Value = "2.62", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 293L, Name = "Marshall", Value = "4.62", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 294L, Name = "Martinique", Value = "2.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 295L, Name = "Mauritania", Value = "2.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 296L, Name = "Mauritius", Value = "2.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 297L, Name = "Mexico", Value = "3.08", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 298L, Name = "Micronesia", Value = "3.85", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 299L, Name = "Moldova", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 300L, Name = "Monaco", Value = "3.62", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 301L, Name = "Mongolia", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 302L, Name = "Montenegro", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 303L, Name = "Montserrat", Value = "3.99", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 304L, Name = "Morocco", Value = "2.08", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 305L, Name = "Mozanbique", Value = "4.00", lookupId = 2, ParentId = null },



//                new LookupValues { Id = 306L, Name = "Myanmar", Value = "3.75", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 307L, Name = "Namibia", Value = "4.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 308L, Name = "Nauru", Value = "4.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 309L, Name = "Nepal", Value = "2.37", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 310L, Name = "Netherlands", Value = "2.72", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 311L, Name = "Netherlands Antilles", Value = "4.20", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 312L, Name = "New-Caledonia", Value = "3.36", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 313L, Name = "New-Zealand", Value = "3.20", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 314L, Name = "Nicaragua", Value = "3.36", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 315L, Name = "Niger", Value = "3.85", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 316L, Name = "Nigeria", Value = "2.57", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 317L, Name = "Niue", Value = "8.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 318L, Name = "Norfolk", Value = "5.70", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 319L, Name = "Norway", Value = "2.47", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 320L, Name = "Oman", Value = "1.24", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 321L, Name = "Pakistan", Value = "1.22", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 322L, Name = "Palastine", Value = "1.85", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 323L, Name = "Palau", Value = "4.30", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 324L, Name = "Panama", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 325L, Name = "Papua", Value = "5.70", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 326L, Name = "Paraguay", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 327L, Name = "Peru", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 328L, Name = "Philippines", Value = "1.76", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 329L, Name = "Poland", Value = "3.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 330L, Name = "Portugal", Value = "2.47", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 331L, Name = "Puerto", Value = "1.54", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 332L, Name = "Qatar", Value = "1.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 333L, Name = "Rwanda", Value = "3.85", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 334L, Name = "Reunion", Value = "1.93", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 335L, Name = "Romania", Value = "2.70", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 336L, Name = "Russia", Value = "3.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 337L, Name = "S.Africa", Value = "1.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 338L, Name = "San-Marino", Value = "3.23", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 339L, Name = "Sao-Tome", Value = "4.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 340L, Name = "Senegal", Value = "2.55", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 341L, Name = "Serbia", Value = "3.15", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 342L, Name = "Seychelles", Value = "3.99", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 343L, Name = "Sierra", Value = "3.75", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 344L, Name = "Singapore", Value = "1.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 345L, Name = "Slovak", Value = "3.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 346L, Name = "Slovania", Value = "3.38", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 347L, Name = "Solomon", Value = "4.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 348L, Name = "Somalia", Value = "2.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 349L, Name = "South  Sudan", Value = "2.63", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 350L, Name = "Spain", Value = "2.47", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 351L, Name = "Srilanka", Value = "1.49", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 352L, Name = "Sudan", Value = "1.15", lookupId = 2, ParentId = null },






//                new LookupValues { Id = 353L, Name = "Surinam", Value = "4.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 354L, Name = "Swaziland", Value = "3.15", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 355L, Name = "Sweden", Value = "1.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 356L, Name = "Switzerland", Value = "3.99", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 357L, Name = "Syria", Value = "1.42", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 358L, Name = "Taiwan", Value = "1.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 359L, Name = "Tajikistan", Value = "3.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 360L, Name = "Tanzania", Value = "3.38", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 361L, Name = "Thailand", Value = "1.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 362L, Name = "Thuraya", Value = "9.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 363L, Name = "Togoland", Value = "3.38", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 364L, Name = "Tokelau", Value = "7.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 365L, Name = "Tonga", Value = "3.90", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 366L, Name = "Trinidad", Value = "3.75", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 367L, Name = "Tunisia", Value = "2.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 368L, Name = "Turkemanistan", Value = "3.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 369L, Name = "Turkey", Value = "1.42", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 370L, Name = "Tuvalu", Value = "7.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 371L, Name = "U.S.-Virgin-Is", Value = "1.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 372L, Name = "UAE", Value = "0.98", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 373L, Name = "Uganda", Value = "3.38", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 374L, Name = "UK", Value = "1.95", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 375L, Name = "Ukraine", Value = "2.55", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 376L, Name = "Uruguay", Value = "3.75", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 377L, Name = "USA", Value = "1.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 378L, Name = "Uzbekistan", Value = "2.65", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 379L, Name = "Vanuatu", Value = "8.00", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 380L, Name = "Venzeula", Value = "3.51", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 381L, Name = "Vietnam", Value = "4.28", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 382L, Name = "Wallis", Value = "5.50", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 383L, Name = "Western", Value = "3.80", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 384L, Name = "Yemen", Value = "1.35", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 385L, Name = "Zambia", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 386L, Name = "Zimbabwe", Value = "3.12", lookupId = 2, ParentId = null },
//                new LookupValues { Id = 387L, Name = "Afghanistan", Value = "2.52", lookupId = 2, ParentId = null }




//                );

//            modelBuilder.Entity<LookupValues>().HasSeedData(
//                new LookupValues { Id = 1L, Name = "BusinessSupport", Value = "Business Support", lookupId = 1, ParentId = null },
//                new LookupValues { Id = 2L, Name = "Supplay Chain Management", Value = "Supplay Chain Management", lookupId = 1, ParentId = 1 },
//                new LookupValues { Id = 3L, Name = "Logistics", Value = "Logistics", lookupId = 1, ParentId = 2 },
//                new LookupValues { Id = 4L, Name = "Contract Mgt", Value = "Contract Mgt.", lookupId = 1, ParentId = 2 },
//                new LookupValues { Id = 5L, Name = "Purchasing", Value = "Purchasing", lookupId = 1, ParentId = 2 },
//                new LookupValues { Id = 6L, Name = "Document Mgt.", Value = "Document Mgt.", lookupId = 1, ParentId = 2 },
//                new LookupValues { Id = 7L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 2 },


//                new LookupValues { Id = 8L, Name = "Human Resources", Value = "Human Resources", lookupId = 1, ParentId = 1 },
//                new LookupValues { Id = 9L, Name = "Career/Succession Planning", Value = "Career/Succession Planning", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 10L, Name = "Manpower Planning & Recruitment", Value = "Manpower Planning & Recruitment", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 11L, Name = "Training & Development", Value = "Training & Development", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 12L, Name = "Performance Appraisal", Value = "Performance Appraisal", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 13L, Name = "Career/Succession Planning", Value = "Career/Succession Planning", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 14L, Name = "Internal Communications", Value = "Internal Communications", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 15L, Name = "People Services", Value = "People Services", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 16L, Name = "Payroll", Value = "Payroll", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 17L, Name = "Government Relations", Value = "Government Relations", lookupId = 1, ParentId = 8 },
//                new LookupValues { Id = 18L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 8 },




//                new LookupValues { Id = 19L, Name = "Administration", Value = "Administration", lookupId = 1, ParentId = 1 },
//                new LookupValues { Id = 20L, Name = "Admin. Support", Value = "Admin. Support", lookupId = 1, ParentId = 19 },
//                new LookupValues { Id = 21L, Name = "Permises & Facility Management", Value = "Permises & Facility Management", lookupId = 1, ParentId = 19 },
//                new LookupValues { Id = 22L, Name = "Security", Value = "Security", lookupId = 1, ParentId = 19 },
//                new LookupValues { Id = 23L, Name = "Health & Safety", Value = "Health & Safety", lookupId = 1, ParentId = 19 },
//                new LookupValues { Id = 24L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 19 },



//                new LookupValues { Id = 25L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 1 },








//                new LookupValues { Id = 26L, Name = "Commercial Sector", Value = "Commercial Sector", lookupId = 1, ParentId = null },


//                new LookupValues { Id = 27L, Name = "Commerical Strategy & Planning ", Value = "Commerical Strategy & Planning", lookupId = 1, ParentId = 26 },
//                new LookupValues { Id = 28L, Name = "Commercial Budgeting & Reporting", Value = "Commercial Budgeting & Reporting", lookupId = 1, ParentId = 27 },
//                new LookupValues { Id = 29L, Name = "Market Research", Value = "Market Research", lookupId = 1, ParentId = 27 },
//                new LookupValues { Id = 30L, Name = "Incentives & Commission", Value = "Incentives & Commission", lookupId = 1, ParentId = 27 },
//                new LookupValues { Id = 31L, Name = "Other ", Value = "Other", lookupId = 1, ParentId = 27 },









//                new LookupValues { Id = 32L, Name = "Commerical Support ", Value = "Commerical Support", lookupId = 1, ParentId = 26 },
//                new LookupValues { Id = 33L, Name = "Installation and Repair", Value = "Installation and Repair", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 34L, Name = "Customer Technical Support", Value = "Customer Technical Support", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 35L, Name = "Business Technical Support", Value = "Business Technical Support", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 36L, Name = "Back Office Technical Support", Value = "Back Office Technical Support", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 37L, Name = "Consumer , Sales Support", Value = "Consumer , Sales Support", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 38L, Name = "Business Sales Support", Value = "Business Sales Support", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 39L, Name = "Back Office (System Planning & Development)", Value = "Back Office (System Planning & Development)", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 40L, Name = "POS Implementation", Value = "POS Implementation", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 41L, Name = "Complaints & Quality Assurance", Value = "Complaints & Quality Assurance", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 42L, Name = "Commercial Operations Development", Value = "Commercial Operations Development", lookupId = 1, ParentId = 32 },
//                new LookupValues { Id = 43L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 32 },






//                new LookupValues { Id = 44L, Name = "Customer Care ", Value = "Customer Care", lookupId = 1, ParentId = 26 },
//                new LookupValues { Id = 45L, Name = "Contact Center ", Value = "Contact Center", lookupId = 1, ParentId = 44 },
//                new LookupValues { Id = 46L, Name = "Call Center", Value = "Call Center", lookupId = 1, ParentId = 44 },
//                new LookupValues { Id = 47L, Name = "Overflow Call Center", Value = "Overflow Call Center", lookupId = 1, ParentId = 44 },
//                new LookupValues { Id = 48L, Name = "Call Center, Quality Development", Value = "Call Center, Quality Development", lookupId = 1, ParentId = 44 },
//                new LookupValues { Id = 49L, Name = "Retention", Value = "Retention", lookupId = 1, ParentId = 44 },
//                new LookupValues { Id = 50L, Name = "Renewal", Value = "Renewal", lookupId = 1, ParentId = 44 },
//                new LookupValues { Id = 51L, Name = "TeleSales", Value = "TeleSales", lookupId = 1, ParentId = 44 },
//                new LookupValues { Id = 52L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 44 },





//                new LookupValues { Id = 53L, Name = "Marketing", Value = "Marketing", lookupId = 1, ParentId = 26 },
//                new LookupValues { Id = 54L, Name = "Consumer Products Development", Value = "Consumer Products Development", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 55L, Name = "Business Products Development", Value = "Business Products Development", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 56L, Name = "Internationaln Call Monitoring", Value = "Internationaln Call Monitoring", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 57L, Name = "Business Analysis & UAT Segmentayion", Value = "Business Analysis & UAT Segmentayion", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 58L, Name = "Testing", Value = "Testing", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 59L, Name = "Pricing", Value = "Pricing", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 60L, Name = "Marcom", Value = "Marcom", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 61L, Name = "Channel Marketing", Value = "Channel Marketing", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 62L, Name = "Media", Value = "Media", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 63L, Name = "E-Marketing", Value = "E-Marketing", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 64L, Name = "Graphic Design", Value = "Graphic Design", lookupId = 1, ParentId = 53 },
//                new LookupValues { Id = 65L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 53 },




//                new LookupValues { Id = 66L, Name = "Sales", Value = "Sales", lookupId = 1, ParentId = 26 },
//                new LookupValues { Id = 67L, Name = "InDirect Sales", Value = "InDirect Sales", lookupId = 1, ParentId = 66 },
//                new LookupValues { Id = 68L, Name = "Business Sales", Value = "Business Sales", lookupId = 1, ParentId = 66 },
//                new LookupValues { Id = 69L, Name = "VIP Sales", Value = "VIP Sales", lookupId = 1, ParentId = 66 },
//                new LookupValues { Id = 70L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 66 },
//                new LookupValues { Id = 71L, Name = "Direct Sales", Value = "Direct Sales", lookupId = 1, ParentId = 66 },








//                new LookupValues { Id = 72L, Name = "Operations", Value = "Operations", lookupId = 1, ParentId = null },
//                new LookupValues { Id = 73L, Name = "Operation Strategy & Support", Value = "Operation Strategy & Support", lookupId = 1, ParentId = 72 },
//                new LookupValues { Id = 74L, Name = "IT Strategy & Support", Value = "IT Strategy & Support", lookupId = 1, ParentId = 73 },
//                new LookupValues { Id = 75L, Name = "Network management Strategy & Support", Value = "Network management Strategy & Support", lookupId = 1, ParentId = 73 },
//                new LookupValues { Id = 76L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 73 },








//                new LookupValues { Id = 77L, Name = "IT Development", Value = "IT Development", lookupId = 1, ParentId = 72 },
//                new LookupValues { Id = 78L, Name = "Business Management", Value = "Business Management", lookupId = 1, ParentId = 77 },
//                new LookupValues { Id = 79L, Name = "Delivery Project Management", Value = "Delivery Project Management", lookupId = 1, ParentId = 77 },
//                new LookupValues { Id = 80L, Name = "Requirement Management", Value = "Requirement Management", lookupId = 1, ParentId = 77 },
//                new LookupValues { Id = 81L, Name = "Change & Release Managment", Value = "Change & Release Managment", lookupId = 1, ParentId = 77 },
//                new LookupValues { Id = 82L, Name = "Business Systems Development", Value = "Business Systems Development", lookupId = 1, ParentId = 77 },
//                new LookupValues { Id = 83L, Name = "Corporate Systems Development", Value = "Corporate Systems Development", lookupId = 1, ParentId = 77 },
//                new LookupValues { Id = 84L, Name = "IT Quality Assurance", Value = "IT Quality Assurance", lookupId = 1, ParentId = 77 },
//                new LookupValues { Id = 85L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 77 },

//                 new LookupValues { Id = 86L, Name = "IT Operations", Value = "IT Operations", lookupId = 1, ParentId = 72 },
//                 new LookupValues { Id = 87L, Name = "OSS/BSS Operations", Value = "OSS/BSS Operations", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 88L, Name = "IT Opertions", Value = "IT Opertions", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 89L, Name = "IT Security", Value = "IT Security", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 90L, Name = "IT Security & Network Management", Value = "IT Security & Network Management", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 91L, Name = "IT Network", Value = "IT Network", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 92L, Name = "Data Center Technical Services", Value = "Data Center Technical Services", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 93L, Name = "Data Center Services", Value = "Data Center Services", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 94L, Name = "System & Database Administration", Value = "System & Database Administration", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 95L, Name = "Corprate Sytems Support", Value = "Corprate Sytems Support", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 96L, Name = " Support Services ", Value = "Support Services ", lookupId = 1, ParentId = 86 },
//                 new LookupValues { Id = 97L, Name = " IT Corprate Applications(CA)", Value = "IT Corprate Applications(CA)", lookupId = 1, ParentId = 86 },
//                new LookupValues { Id = 98L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 86 },



//                new LookupValues { Id = 99L, Name = "Network Engineering", Value = "Network Engineering", lookupId = 1, ParentId = 72 },
//                new LookupValues { Id = 100L, Name = "Network Planning", Value = "Network Planning", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 101L, Name = "Core Planning", Value = "Core Planning", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 102L, Name = "Access Planning", Value = "Access Planning", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 103L, Name = "Service Platform Planing", Value = "Service Platform Planing", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 104L, Name = "Planning & Subsystems ", Value = "Planning & Subsystems", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 105L, Name = "Network Design", Value = "Network Design", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 106L, Name = "Acccess Design", Value = "Acccess Design", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 107L, Name = "Services PlatForm Design", Value = "Services PlatForm Design", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 108L, Name = "Network  Deployment", Value = "Network  Deployment", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 109L, Name = "Electromechanical", Value = "Electromechanical", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 110L, Name = "Network  Deployment  Core", Value = "Network  Deployment  Core", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 111L, Name = "Network  Deployment  Access", Value = "Network  Deployment  Access", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 112L, Name = "Project  Deployment  Regions", Value = "Project  Deployment  Regions", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 113L, Name = "Site  Acquisition", Value = "Site  Acquisition", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 114L, Name = "Network  Integration", Value = "Network  Integration", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 115L, Name = "Network  Integration  Core", Value = "Network  Integration  Core", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 116L, Name = "Network  Integration  Access", Value = "Network  Integration  Access", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 117L, Name = "Network  Integration  Service", Value = "Network  Integration  Service", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 118L, Name = "Projects  &  Contract  Execution  &  Management", Value = "Projects  &  Contract  Execution  &  Management", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 119L, Name = "Project  Execution  &  Reporting", Value = "Project  Execution  &  Reporting", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 120L, Name = "Contract  Execution  &  Management", Value = "Contract  Execution  &  Management", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 121L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 99 },
//                new LookupValues { Id = 122L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 26 },


//                new LookupValues { Id = 123L, Name = "Network Opertions", Value = "Network Opertions", lookupId = 1, ParentId = 72 },
//                new LookupValues { Id = 124L, Name = "Quality  Assurance", Value = "Quality  Assurance", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 125L, Name = "Power  &  Admin", Value = "Power & Admin", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 126L, Name = "Core  &  Access  O  &  M", Value = "Core & Access O & M", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 127L, Name = "Tech.  Support", Value = "Tech.  Support", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 128L, Name = "Field  Support", Value = "Field  Support", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 129L, Name = "EMS", Value = "EMS", lookupId = 1, ParentId = 26 },
//                new LookupValues { Id = 130L, Name = "Electro-mechanical", Value = "Electro-mechanical", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 131L, Name = "IMS  Sevices", Value = "IMS  Sevices", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 132L, Name = "ISP  Services", Value = "ISP  Services", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 133L, Name = "Optimization  &  Performance", Value = "Optimization  &  Performance", lookupId = 1, ParentId = 123 },
//                new LookupValues { Id = 134L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 123 },



//                new LookupValues { Id = 135L, Name = "Network Solutions", Value = "Network Solutions", lookupId = 1, ParentId = 72 },
//                new LookupValues { Id = 136L, Name = "System  Solutions", Value = "System  Solutions", lookupId = 1, ParentId = 135 },
//                new LookupValues { Id = 137L, Name = "VAS  Solutions", Value = "VAS  Solutions", lookupId = 1, ParentId = 135 },
//                new LookupValues { Id = 138L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 135 },



//                new LookupValues { Id = 139L, Name = "InterConnection", Value = "InterConnection", lookupId = 1, ParentId = 72 },
//                new LookupValues { Id = 140L, Name = "International Agreements / Contracts", Value = "International Agreements / Contracts", lookupId = 1, ParentId = 139 },
//                new LookupValues { Id = 141L, Name = "Interconnection Settlements / Agreements", Value = "Interconnection Settlements / Agreements", lookupId = 1, ParentId = 139 },
//                new LookupValues { Id = 142L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 139 },
//                new LookupValues { Id = 143L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 72 },

//                new LookupValues { Id = 144L, Name = "Finance", Value = "Finance", lookupId = 1, ParentId = null },

//                new LookupValues { Id = 145L, Name = "Financial Accounting & Treasury", Value = "Financial Accounting & Treasury", lookupId = 1, ParentId = 144 },
//                new LookupValues { Id = 146L, Name = "Fixed  Assets  Insurance  Risk", Value = "Fixed Assets Insurance Risk", lookupId = 1, ParentId = 145 },
//                new LookupValues { Id = 147L, Name = "Accounts  Payable  GL", Value = "Accounts Payable GL", lookupId = 1, ParentId = 145 },
//                new LookupValues { Id = 148L, Name = "Treasury", Value = "Treasury", lookupId = 1, ParentId = 145 },
//                new LookupValues { Id = 149L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 145 },
//                new LookupValues { Id = 150L, Name = "Financial Palanning & Reporting", Value = "Other", lookupId = 1, ParentId = 144 },
//                new LookupValues { Id = 151L, Name = "Financial  Planning  Expenses  Capex", Value = "Financial Planning Expenses & Capex", lookupId = 1, ParentId = 150 },
//                new LookupValues { Id = 152L, Name = "Financial  Reporting  /  Revenue", Value = "Financial Reporting Revenue", lookupId = 1, ParentId = 150 },
//                new LookupValues { Id = 153L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 150 },


//                new LookupValues { Id = 154L, Name = "Credit  Control  &  Revenue  Assurance", Value = "Credit Control Revenue Assurance'", lookupId = 1, ParentId = 144 },
//                new LookupValues { Id = 155L, Name = "Billing", Value = "Billing", lookupId = 1, ParentId = 154 },
//                new LookupValues { Id = 156L, Name = "Collection  &  Bank Reconciliation", Value = "Collection Bank Reconciliation", lookupId = 1, ParentId = 154 },
//                                new LookupValues { Id = 157L, Name = "Credit Control & Revenue  Assurance", Value = "Credit Control Revenue Assurance'", lookupId = 1, ParentId = 154 },

//                new LookupValues { Id = 158L, Name = "Revenue Assurance", Value = "Revenue Assurance", lookupId = 1, ParentId = 154 },
//                new LookupValues { Id = 159L, Name = "Credit Control & Receivable", Value = "Credit Control & Receivable", lookupId = 1, ParentId = 154 },
//                new LookupValues { Id = 160L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 154 },
//                new LookupValues { Id = 161L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 144 },
//                new LookupValues { Id = 162L, Name = "Regulatory", Value = "Regulatory", lookupId = 1, ParentId = null },

//                new LookupValues { Id = 163L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 162 },

//                new LookupValues { Id = 164L, Name = "Audit", Value = "Audit", lookupId = 1, ParentId = null },
//                new LookupValues { Id = 165L, Name = "Other", Value = "Other", lookupId = 1, ParentId = 164 },






//                new LookupValues { Id = 166L, Name = "Process & Quality Management", Value = "Process & Quality Management", lookupId = 1, ParentId = 1 },
//                new LookupValues { Id = 167L, Name = "Legal", Value = "Legal", lookupId = 1, ParentId = 1 },
//                new LookupValues { Id = 168L, Name = "Public Relations", Value = "Public Relations", lookupId = 1, ParentId = 1 },
//                new LookupValues { Id = 169L, Name = "ShareHolders Relations", Value = "ShareHolders Relations", lookupId = 1, ParentId = 1 }







//                );






//            modelBuilder.Entity<LookupValues>().HasSeedData(
//             new LookupValues { Id = 388L, Name = "??? ???????", Value = "??? ???????", lookupId = 3, ParentId = null },
//             new LookupValues { Id = 389L, Name = "?????  ????? ???????", Value = "????? ????? ???????", lookupId = 3, ParentId = 388 },
//             new LookupValues { Id = 390L, Name = "??????? ?????????", Value = "??????? ?????????", lookupId = 3, ParentId = 389 },
//             new LookupValues { Id = 391L, Name = "????? ??????", Value = "????? ??????", lookupId = 3, ParentId = 389 },
//             new LookupValues { Id = 392L, Name = "????", Value = "????", lookupId = 3, ParentId = 389 },
//             new LookupValues { Id = 393L, Name = "????? ???????", Value = "????? ???????", lookupId = 3, ParentId = 389 },
//             new LookupValues { Id = 394L, Name = "??????", Value = "??????", lookupId = 3, ParentId = 389 },


//             new LookupValues { Id = 395L, Name = "??????? ???????", Value = "??????? ???????", lookupId = 3, ParentId = 388 },
//             new LookupValues { Id = 396L, Name = "??????? ??????? / ???????", Value = "??????? ??????? / ???????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 397L, Name = "????? ?????? ????? ???????", Value = "????? ?????? ????? ???????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 398L, Name = "??????? ? ???????", Value = "??????? ? ???????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 399L, Name = "????? ??????", Value = "????? ??????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 400L, Name = "??????? ??????? / ???????", Value = "??????? ??????? / ???????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 401L, Name = "????? ?????", Value = "????? ?????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 402L, Name = "????? ???????", Value = "????? ???????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 403L, Name = "??? ?????", Value = "??? ?????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 404L, Name = "?????? ??????", Value = "?????? ??????", lookupId = 3, ParentId = 395 },
//             new LookupValues { Id = 405L, Name = "????", Value = "????", lookupId = 3, ParentId = 395 },




//             new LookupValues { Id = 406L, Name = "???????", Value = "???????", lookupId = 3, ParentId = 388 },
//             new LookupValues { Id = 407L, Name = "????? ???????", Value = "????? ???????", lookupId = 3, ParentId = 406 },
//             new LookupValues { Id = 408L, Name = "??????? ?????? ???????", Value = "??????? ?????? ???????", lookupId = 3, ParentId = 406 },
//             new LookupValues { Id = 409L, Name = "????? ", Value = "?????", lookupId = 3, ParentId = 406 },
//             new LookupValues { Id = 410L, Name = "????? ????????", Value = "????? ????????", lookupId = 3, ParentId = 406 },
//             new LookupValues { Id = 411L, Name = "????", Value = "????", lookupId = 3, ParentId = 406 },



//             new LookupValues { Id = 412L, Name = "????", Value = "????", lookupId = 3, ParentId = 388 },








//             new LookupValues { Id = 413L, Name = "???? ?????", Value = "???? ?????", lookupId = 3, ParentId = null },


//             new LookupValues { Id = 414L, Name = "???????????? ???????? ?????????", Value = "???????????? ???????? ?????????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 415L, Name = "?????????? ???????? ?????????", Value = "?????????? ???????? ?????????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 416L, Name = "????? ?? ??????", Value = "????? ?? ??????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 417L, Name = "??????? ?????????", Value = "??????? ?????????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 418L, Name = "???? ", Value = "????", lookupId = 3, ParentId = 413 },









//             new LookupValues { Id = 419L, Name = "??? ?????", Value = "??? ?????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 420L, Name = "??????? ????????", Value = "??????? ????????", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 421L, Name = "??? ??? ??????? ", Value = "??? ??? ??????? ", lookupId =3, ParentId = 419 },
//             new LookupValues { Id = 422L, Name = "????? ????? ???????", Value = "????? ????? ???????", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 423L, Name = "????? ????? ?????? ??????", Value = "????? ????? ?????? ??????", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 424L, Name = "???????? ? ??? ???????? ", Value = "???????? ? ??? ???????? ", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 425L, Name = "??? ?????? ???????", Value = "??? ?????? ???????", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 426L, Name = "(?????? ?????? (????? ?????? ??????", Value = "(?????? ?????? (????? ?????? ??????", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 427L, Name = "????? ???? ????? ", Value = "????? ???? ????? ", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 428L, Name = "??????? ? ???? ??????", Value = "??????? ? ???? ??????", lookupId = 3, ParentId = 419 },
//             new LookupValues { Id = 429L, Name = "????? ???????? ????????", Value = "????? ???????? ????????", lookupId =3, ParentId = 419 },
//             new LookupValues { Id = 430L, Name = "????", Value = "????", lookupId = 3, ParentId = 419 },






//             new LookupValues { Id = 431L, Name = "????? ??????", Value = "????? ??????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 432L, Name = "???? ???????", Value = "???? ???????", lookupId = 3, ParentId = 431 },
//             new LookupValues { Id = 433L, Name = "???? ???????", Value = "???? ???????", lookupId = 3, ParentId = 431 },
//             new LookupValues { Id = 434L, Name = "???? ????? ????? ????? ", Value = "???? ????? ????? ????? ", lookupId = 3, ParentId = 431 },
//             new LookupValues { Id = 435L, Name = "???? ??????? ? ????? ??????", Value = "???? ??????? ? ????? ??????", lookupId = 3, ParentId = 431 },
//             new LookupValues { Id = 436L, Name = "???", Value = "???", lookupId = 3, ParentId = 431 },
//             new LookupValues { Id = 437L, Name = "???????", Value = "???????", lookupId = 3, ParentId = 431 },
//             new LookupValues { Id = 438L, Name = "???????? ??? ??????", Value = "???????? ??? ??????", lookupId = 3, ParentId = 431 },
//             new LookupValues { Id = 439L, Name = "????", Value = "????", lookupId = 3, ParentId = 431 },





//             new LookupValues { Id = 440L, Name = "?????", Value = "?????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 441L, Name = "????? ???????? ???????????", Value = "????? ???????? ???????????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 442L, Name = "????? ?????? ???????", Value = "????? ?????? ???????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 443L, Name = "?????? ????????? ???????", Value = "?????? ????????? ???????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 444L, Name = "Uat ????? ??????? ????", Value = "Uat ????? ??????? ????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 445L, Name = "????????", Value = "????????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 446L, Name = "???????", Value = "???????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 447L, Name = "??????", Value = "??????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 448L, Name = "???? ???????", Value = "???? ???????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 449L, Name = "????? ???????", Value = "????? ???????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 450L, Name = "??????? ??????????", Value = "??????? ??????????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 451L, Name = "??????? ?????????", Value = "??????? ?????????", lookupId = 3, ParentId = 440 },
//             new LookupValues { Id = 452L, Name = "????", Value = "????", lookupId = 3, ParentId = 440 },




//             new LookupValues { Id = 453L, Name = "??????", Value = "??????", lookupId = 3, ParentId = 413 },
//             new LookupValues { Id = 454L, Name = "????? ???????", Value = "????? ???????", lookupId = 3, ParentId = 453 },
//             new LookupValues { Id = 456L, Name = "?????? ??????? ", Value = "?????? ??????? ", lookupId = 3, ParentId = 453 },
//             new LookupValues { Id = 457L, Name = "?????? VIP", Value = "?????? VIP", lookupId = 3, ParentId = 453 },
//             new LookupValues { Id = 458L, Name = "????", Value = "????", lookupId = 3, ParentId = 453 },
//             new LookupValues { Id = 459L, Name = "?????? ??????", Value = "?????? ??????", lookupId = 3, ParentId = 453 },








//             new LookupValues { Id = 460L, Name = "??????", Value = "??????", lookupId = 3, ParentId = null },
//             new LookupValues { Id = 461L, Name = "?????????? ??????? ?????? ", Value = "?????????? ??????? ??????", lookupId = 3, ParentId = 460 },
//             new LookupValues { Id = 462L, Name = "?????????? ???? ????????? ?????????", Value = "?????????? ???? ????????? ?????????", lookupId = 3, ParentId = 461 },
//             new LookupValues { Id = 463L, Name = "?????????? ???? ????? ?????? ", Value = "?????????? ???? ????? ?????? ", lookupId = 3, ParentId = 461 },
//             new LookupValues { Id = 464L, Name = "????", Value = "????", lookupId = 3, ParentId = 461 },







//             new LookupValues { Id = 465L, Name = "????? ????????? ????????? ", Value = "????? ????????? ????????? ", lookupId = 3, ParentId = 460 },
//             new LookupValues { Id = 466L, Name = "????? ?????", Value = "????? ?????", lookupId = 3, ParentId = 465 },
//             new LookupValues { Id = 467L, Name = "????? ????? ???????", Value = "????? ????? ???????", lookupId = 3, ParentId = 465 },
//             new LookupValues { Id = 468L, Name = "????? ?????????", Value = "????? ?????????", lookupId = 3, ParentId = 465 },
//             new LookupValues { Id = 469L, Name = "??????? ???????? ?? ???????", Value = "??????? ???????? ?? ???????", lookupId = 3, ParentId = 465 },
//             new LookupValues { Id = 470L, Name = "????? ????? ???????", Value = "????? ????? ???????", lookupId = 3, ParentId = 465 },
//             new LookupValues { Id = 471L, Name = "???? ????? ???????", Value = "???? ????? ???????", lookupId = 3, ParentId = 465 },
//             new LookupValues { Id = 472L, Name = "???? ???? ????????? ????????? ", Value = "???? ???? ????????? ????????? ", lookupId = 3, ParentId = 465 },
//             new LookupValues { Id = 473L, Name = "????", Value = "????", lookupId = 3, ParentId = 465 },

//              new LookupValues { Id = 474L, Name = "?????? ????????? ????????? ", Value = "?????? ????????? ????????? ", lookupId = 3, ParentId = 460 },
//              new LookupValues { Id = 475L, Name = "?????? OSS / BSS", Value = "?????? OSS / BSS", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 476L, Name = "?????? ????????? ?????????", Value = "?????? ????????? ?????????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 477L, Name = "??? ????????? ?????????", Value = "??? ????????? ?????????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 478L, Name = "??? ????????? ????????? ?????? ???????", Value = "??? ????????? ????????? ?????? ???????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 479L, Name = "???? ????????? ?????????", Value = "???? ????????? ?????????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 480L, Name = "??????? ?????? ????? ????????", Value = "??????? ?????? ????? ????????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 481L, Name = "????? ???? ????????", Value = "????? ???? ????????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 482L, Name = "????? ?????? ?????? ????????", Value = "????? ?????? ?????? ????????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 483L, Name = "??? ????? Corprate", Value = "??? ????? Corprate", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 484L, Name = "????? ????? ", Value = "????? ?????", lookupId = 3, ParentId = 474 },
//              new LookupValues { Id = 485L, Name = " ??????? ????? ????????? ????????? (CA) ", Value = "??????? ????? ????????? ????????? (CA) ", lookupId = 3, ParentId = 474 },
//             new LookupValues { Id = 486L, Name = "????", Value = "????", lookupId = 3, ParentId = 474 },



//             new LookupValues { Id = 487L, Name = "????? ???????", Value = "????? ???????", lookupId = 3, ParentId = 460 },
//             new LookupValues { Id = 488L, Name = "????? ??????", Value = "????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 489L, Name = "??????? ???????", Value = "??????? ???????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 490L, Name = "????? ??????", Value = "????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 491L, Name = "????? ???? ??????", Value = "????? ???? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 492L, Name = "??????? ???????? ???????", Value = "??????? ???????? ???????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 493L, Name = "????? ???????", Value = "????? ???????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 494L, Name = " ????? ??????", Value = "????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 495L, Name = "  ????? ?????  PlatForm ", Value = "????? ?????  PlatForm ", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 496L, Name = "??? ??????", Value = "??? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 497L, Name = "??????????????? ", Value = "??????????????? ", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 498L, Name = "?????? ?????? ????????", Value = "?????? ?????? ????????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 499L, Name = "?????? ??? ??? ??????", Value = "?????? ??? ??? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 500L, Name = "????? ?????? ???????", Value = "????? ?????? ???????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 501L, Name = "?????? ??????", Value = "?????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 502L, Name = "????? ??????", Value = "????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 503L, Name = "???? ????? ??????", Value = "???? ????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 504L, Name = "?????? ??? ????? ??????", Value = "?????? ??? ????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 505L, Name = "???? ????? ??????", Value = "???? ????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 506L, Name = "????? ?????? ???????? ???????", Value = "????? ?????? ???????? ???????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 507L, Name = "????? ??????? ?????? ????????", Value = "????? ??????? ?????? ????????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 508L, Name = "????? ?????? ??????", Value = "????? ?????? ??????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 509L, Name = "????", Value = "????", lookupId = 3, ParentId = 487 },
//             new LookupValues { Id = 510L, Name = "????", Value = "????", lookupId = 3, ParentId = 413 },


//             new LookupValues { Id = 511L, Name = "?????? ??????", Value = "?????? ??????", lookupId = 3, ParentId = 460 },
//             new LookupValues { Id = 512L, Name = "???? ??????", Value = "???? ??????", lookupId = 3, ParentId = 511 },
//             new LookupValues { Id = 513L, Name = "?????? ???????", Value = "?????? ???????", lookupId = 3, ParentId = 511 },
//             new LookupValues
//             {
//                 Id = 514L,
//                 Name = "???????? ??????? O & M",
//                 Value = "???????? ??????? O & M",
//                 lookupId = 3,
//                 ParentId = 511
//             },
//             new LookupValues { Id = 515L, Name = "??? ????", Value = "??? ????", lookupId = 3, ParentId = 511 },
//                 new LookupValues { Id = 516L, Name = "????? ????????", Value = "????? ????????", lookupId = 3, ParentId = 511 },
//                 new LookupValues { Id = 517L, Name = "EMS", Value = "EMS", lookupId = 3, ParentId = 413 },
//                 new LookupValues { Id = 518L, Name = "???????????????", Value = "???????????????", lookupId = 3, ParentId = 511 },
//                 new LookupValues { Id = 519L, Name = "IMS ????? ", Value = "IMS ????? ", lookupId = 3, ParentId = 511 },
//                 new LookupValues { Id = 520L, Name = "ISP ????? ", Value = "ISP ????? ", lookupId = 3, ParentId = 511 },
//                 new LookupValues { Id = 521L, Name = "??????? ???????", Value = "??????? ???????", lookupId = 3, ParentId = 511 },
//                 new LookupValues { Id = 522L, Name = "????", Value = "????", lookupId = 3, ParentId = 511 },



//                 new LookupValues { Id = 523L, Name = "???? ???????", Value = "???? ???????", lookupId = 3, ParentId = 460 },
//                 new LookupValues { Id = 524L, Name = "???? ??????", Value = "???? ??????", lookupId = 3, ParentId = 523 },
//                 new LookupValues { Id = 525L, Name = "???? VAS", Value = "???? VAS", lookupId = 3, ParentId = 523 },
//                 new LookupValues { Id = 526L, Name = "????", Value = "????", lookupId = 3, ParentId = 523 },



//                 new LookupValues { Id = 527L, Name = "?????? ??????", Value = "?????? ??????", lookupId = 3, ParentId = 460 },
//                 new LookupValues { Id = 528L, Name = "?????????? / ?????? ???????", Value = "???? ??????", lookupId = 3, ParentId = 527 },
//                 new LookupValues { Id = 529L, Name = "???????? / ?????????? ???????", Value = "???? VAS", lookupId = 3, ParentId = 527 },
//                 new LookupValues { Id = 530L, Name = "????", Value = "????", lookupId = 3, ParentId = 527 },
//                 new LookupValues { Id = 531L, Name = "????", Value = "????", lookupId = 3, ParentId = 460 },

//                 new LookupValues { Id = 532L, Name = "?????", Value = "?????", lookupId = 3, ParentId = null },

//                 new LookupValues
//                 {
//                     Id = 533L,
//                     Name = "???????? ??????? ???????? ",
//                     Value = "???????? ??????? ? ??????? ",
//                     lookupId = 3,
//                     ParentId = 144
//                 },
//             new LookupValues { Id = 534L, Name = "????? ????? ?????? ???????", Value = "????? ????? ?????? ???????", lookupId = 3, ParentId = 533 },
//                     new LookupValues { Id = 535L, Name = "???????? ??????? GL", Value = "???????? ??????? GL", lookupId = 3, ParentId = 533 },
//                     new LookupValues { Id = 536L, Name = "?????", Value = "?????", lookupId = 3, ParentId = 533 },
//                     new LookupValues { Id = 537L, Name = "????", Value = "????", lookupId = 3, ParentId = 533 },

//                     new LookupValues { Id = 538L, Name = "??????? ?????? ?????? ????????", Value = "??????? ?????? ?????? ????????", lookupId = 3, ParentId = 532 },
//                     new LookupValues { Id = 539L, Name = "????? ??????? ??????", Value = "????? ??????? ??????", lookupId = 3, ParentId = 538 },
//                     new LookupValues { Id = 540L, Name = "??????? ???????? ??????? ", Value = "??????? ???????? ??????? ", lookupId = 3, ParentId = 538 },
//                     new LookupValues { Id = 541L, Name = "????", Value = "????", lookupId = 3, ParentId = 538 },


//                     new LookupValues { Id = 542L, Name = "?????? ???????? ?????? ?????????", Value = "?????? ???????? ?????? ?????????", lookupId = 3, ParentId = 532 },
//                     new LookupValues { Id = 543L, Name = "????????", Value = "????????", lookupId = 3, ParentId = 542 },
//                     new LookupValues
//                     {
//                         Id = 544L,
//                         Name = " ??????? ???????? ????????",
//                         Value = "??????? ???????? ????????",
//                         lookupId = 3,
//                         ParentId = 542
//                     },
//             new LookupValues { Id = 545L, Name = "?????? ???????? ????? ?????????", Value = "?????? ???????? ????? ?????????", lookupId = 3, ParentId = 542 },

//                         new LookupValues { Id = 546L, Name = "???? ????????? ", Value = "???? ????????? ", lookupId = 3, ParentId = 542 },
//                         new LookupValues { Id = 547L, Name = "?????? ???????? ?????? ??????? ", Value = "?????? ???????? ?????? ??????? ", lookupId = 3, ParentId = 542 },
//                         new LookupValues { Id = 548L, Name = "????", Value = "????", lookupId = 3, ParentId = 542 },
//                         new LookupValues { Id = 549L, Name = "????", Value = "????", lookupId = 3, ParentId = 532 },
//                         new LookupValues { Id = 550L, Name = "?????????", Value = "?????????", lookupId = 3, ParentId = null },

//                         new LookupValues { Id = 551L, Name = "????", Value = "????", lookupId = 3, ParentId = 550 },

//                         new LookupValues { Id = 552L, Name = "??????", Value = "??????", lookupId = 3, ParentId = null },
//                         new LookupValues { Id = 553L, Name = "????", Value = "????", lookupId = 3, ParentId = 552 },






//                         new LookupValues { Id = 554L, Name = "????? ???????? ??????? ", Value = "????? ???????? ??????? ", lookupId = 3, ParentId = 388 },
//                         new LookupValues { Id = 555L, Name = "??????", Value = "??????", lookupId = 3, ParentId = 1 },
//                         new LookupValues { Id = 556L, Name = "???????? ??????", Value = "???????? ??????", lookupId = 3, ParentId = 388 },
//                         new LookupValues { Id = 557L, Name = "?????? ?????????", Value = "?????? ?????????", lookupId = 3, ParentId = 388 }







//             );
//        }



//    }
//}

