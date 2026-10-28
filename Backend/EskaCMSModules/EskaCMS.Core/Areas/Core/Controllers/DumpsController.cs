using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EskaCMS.Plans.Controllers
{
    [Route("api/BackUps/[action]")]
    [Authorize]
    [ApiController]

    public class DumpsController : ControllerBase
    {
        // private readonly IWebHostEnvironment _env;
        // private readonly IConfiguration _configuration;
        
        public readonly double BYTE_TO_GIGA = 1024.0 * 1024.0 * 1024.0;
        public readonly double MIN_FREE_SPACE_IN_GIGA = 10;


        public DumpsController(/* IWebHostEnvironment env , IConfiguration configuration */)
        {
            // _env = env;
            // _configuration = configuration;
        }

      
        //[HttpGet]
        //public async Task<IActionResult> TakeBackup()
        //{
        //    try
        //    {

        //        // Create a DriveInfo instance of the D:\ drive
        //        DriveInfo dDrive = new DriveInfo("C");

        //        // When the drive is accessible..
        //        if (dDrive.IsReady)
        //        {
        //            // Calculate the percentage free space
        //            double freeSpacePerc = (dDrive.AvailableFreeSpace / (float)dDrive.TotalSize) * 100;

        //            // Ouput drive information
        //            var Name = dDrive.Name;
        //            var AvailableFreeSpace = dDrive.AvailableFreeSpace / BYTE_TO_GIGA; 
        //            var TotalSize = dDrive.TotalSize;


        //            if(AvailableFreeSpace >= MIN_FREE_SPACE_IN_GIGA )
        //            {
        //               var BackupStatus =  Backup();
        //                if (BackupStatus)
        //                {
        //                    return Ok(BackupStatus);
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception("Not Enough Space in " + Name.Split(':')[0] +" Disk .");
        //            }

        //        }


        //        throw new Exception("Something Went Wrong .");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}




        //private bool Backup()
        //{
        //    string constring = _configuration.GetConnectionString("DefaultConnection");
        //    string file = _env.WebRootPath + "\\backup.sql";
        //    using (MySqlConnection conn = new MySqlConnection(constring))
        //    {
        //        using (MySqlCommand cmd = new MySqlCommand())
        //        {
        //            using (MySqlBackup mb = new MySqlBackup(cmd))
        //            {
        //                cmd.Connection = conn;
        //                conn.Open();
        //                mb.ExportToFile(file);
        //                conn.Close();
        //                return true;
        //            }
        //        }
        //    }
        //}






    }
}
