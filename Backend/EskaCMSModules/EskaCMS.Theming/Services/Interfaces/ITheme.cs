

using EskaCMS.Entities.Entity;
using EskaCMS.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Theming.Services.Interfaces
{
    public interface ITheme
    {
        Task <List<Theme>> GetThemesAsync(long SiteId);
        Task<Theme> SaveTheme(Theme Theme, long Id, long UserId, long SiteId);
        Task<bool> SetActiveThemeAsync(long Id, long UserId, long SiteId);
        Task<bool> DeleteTheme(long Id);
        Task<string> SaveThemefonts(string Fonts, long Id,long siteId);
        Task<string> GetThemeFonts(long Id,long siteId);
        Task<bool> SaveThemeClass(string classes, long Id,long siteId);
        Task<string> GetThemeClass(long Id, long siteId);
        Task<bool> SaveThemeStyle(string style, long Id, long siteId);
        Task<string> GetThemestyle(long Id, long siteId);
        Task<bool> SaveThemeScript(string script, long Id, long siteId);
        Task<string> GetThemeScript(long Id, long siteId);
        Task<bool> SaveThemeFile(List<ImportedFilesVM> importedFiles, long Id, long siteId);
        Task<List<ImportedFilesVM>> GetThemefile(long Id, long siteId);
        Task<string> GetThemeColors(long Id,long siteId);
        Task<bool> SaveThemeColors(string colors, long Id, long siteId);

        Task<bool> DeleteImportedFile(long Id);


    }
}
