

using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Theming.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using System.IO;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Theming.Services
{
    public class ThemeService : ITheme
    {
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<Theme> _ThemeRepository;
        private readonly IRepository<ThemeImportedFiles> _ThemeImportedFilesRepository;


        public ThemeService(IRepository<Theme> ThemeRepository,
                    IRepository<ThemeImportedFiles> ThemeImportedFilesRepository,
                    EskaDCMSDbContext context)
        {
            _ThemeRepository = ThemeRepository;
            _ThemeImportedFilesRepository = ThemeImportedFilesRepository;
            _context = context;
        }

        public async Task<List<Theme>> GetThemesAsync(long SiteId)
        {
            try
            {
                var themes = await _ThemeRepository.Query()
                                .Where(x => x.SiteId == SiteId && x.Status == EStatus.Published)
                                .Include(i => i.ImportedFiles)
                                .ThenInclude(i => i.Media.MediaDetails)
                                .ToListAsync();

                return themes;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<Theme> SaveTheme(Theme theme, long Id, long UserId, long SiteId)
        {
            try
            {

                if (Id != 0)
                {
                    var Theme = await _ThemeRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                    Theme.Name = theme.Name;
                    Theme.CreatedById = UserId;
                    Theme.CreationDate = DateTime.Now;
                    Theme.SiteId = SiteId;
                    Theme.Status = EStatus.Published;
                    //Theme.Fonts = theme.Fonts;
                    //Theme.Styles = theme.Styles;
                    //Theme.Colors = theme.Colors;
                    //Theme.Classes = theme.Classes;
                    //Theme.Scripts = theme.Scripts;
                    //Theme.ImportFiles = theme.ImportFiles;
                    await _ThemeRepository.SaveChangesAsync();

                    return Theme;
                }
                else
                {
                    var sitethemes = await _ThemeRepository.Query().Where(x => x.SiteId == SiteId).FirstOrDefaultAsync();

                    var Theme = new Theme();
                    Theme.Name = theme.Name;
                    Theme.CreatedById = UserId;
                    Theme.CreationDate = DateTime.Now;
                    Theme.SiteId = SiteId;
                    Theme.Status = EStatus.Published;

                    Theme.IsDefault = sitethemes == null;


                    //Theme.Fonts = theme.Fonts;
                    //Theme.Styles = theme.Styles;
                    //Theme.Colors = theme.Colors;
                    //Theme.Classes = theme.Classes;
                    //Theme.Scripts = theme.Scripts;
                    //Theme.ImportFilesList = theme.ImportFilesList;


                    _ThemeRepository.Add(Theme);
                    await _ThemeRepository.SaveChangesAsync();

                    return Theme;
                }

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<string> SaveThemefonts(string Fonts, long Id, long siteId)
        {
            var theme = await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).FirstOrDefaultAsync();
            theme.Fonts = Fonts;
            await _ThemeRepository.SaveChangesAsync();
            return theme.Fonts;
        }
        public async Task<bool> SaveThemeClass(string classes, long Id, long siteId)
        {
            var theme = await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).FirstOrDefaultAsync();
            theme.Classes = classes;
            await _ThemeRepository.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SaveThemeStyle(string style, long Id, long siteId)
        {
            var theme = await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).FirstOrDefaultAsync();
            theme.Styles = style;

            if (theme == null)
                return true;

            string Folder = "wwwroot/Resources/styles/themes/siteId_" + siteId + "_themeId_" + theme.Id;
            var minifiedCss = FileHelper.MinifyCssFile(cssContent: style);
            FileHelper.UpsertFile(Content: minifiedCss, Name: "themeStyle", Path: Folder, type: "css");
            await _ThemeRepository.SaveChangesAsync();
            return true;

        }
        public async Task<bool> SaveThemeScript(string script, long Id, long siteId)
        {
            var theme = await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).FirstOrDefaultAsync();
            theme.Scripts = script;
            await _ThemeRepository.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SaveThemeFile(List<ImportedFilesVM> ImportedFiles, long ThemeId, long siteId)
        {
            var ImportedFilesList = ImportedFiles.Where(i => i.Id == 0).Select(i => new ThemeImportedFiles()
            {
                MediaId = i.FileId,
                ThemeId = ThemeId
            }).ToList();
            _ThemeImportedFilesRepository.AddRange(ImportedFilesList);
            await _ThemeImportedFilesRepository.SaveChangesAsync();
            return true;
        }

        public async Task<string> GetThemeClass(long Id, long siteId)
        {
            return await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).Select(x=>x.Classes).FirstOrDefaultAsync();
           

        }
        public async Task<string> GetThemeFonts(long Id, long siteId)
        {
            return await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).Select(x=>x.Fonts).FirstOrDefaultAsync();
        }
        public async Task<string> GetThemestyle(long Id, long siteId)
        {
            return await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).Select(x => x.Styles).FirstOrDefaultAsync();
        }
        public async Task<string> GetThemeScript(long Id, long siteId)
        {
            return await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).Select(x => x.Scripts).FirstOrDefaultAsync();
        }

        public async Task<List<ImportedFilesVM>> GetThemefile(long Id, long siteId)
        {
            var theme = await _ThemeRepository.Query().AsNoTracking().Where(x => x.Id == Id && x.SiteId == siteId)
                .Include(x => x.ImportedFiles)
                .ThenInclude(i => i.Media)
                .Include("ImportedFiles.Media.MediaDetails")
                .FirstOrDefaultAsync();

            if (theme is null)
            {
                throw new Exception("Theme Not Found");
            }

            var ThemeImportedFiles = theme.ImportedFiles.Select(i =>
                        new ImportedFilesVM()
                        {
                            Id = i.Id,
                            SourceId = Id,
                            FileUrl = i.Media.PublicUrl,
                            FileId = i.Media.Id,
                            FileName = i.Media
                                            .MediaDetails
                                            .Select(md => md.Title)
                                            .FirstOrDefault()
                        }
                    ).ToList();

            return ThemeImportedFiles;

        }

        public async Task<string> GetThemeColors(long Id, long siteId)
        {
            return await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).Select(X=>X.Colors).FirstOrDefaultAsync();

        }

        public async Task<bool> SaveThemeColors(string colors, long Id, long siteId)
        {
            var theme = await _ThemeRepository.Query().Where(x => x.Id == Id && x.SiteId == siteId).FirstOrDefaultAsync();
            theme.Colors = colors;
            await _ThemeRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetActiveThemeAsync(long Id, long UserId, long SiteId)
        {
            try
            {
                var Themes = await _ThemeRepository.Query().Where(x => x.SiteId == SiteId && x.Status == EStatus.Published).ToListAsync();

                foreach (var item in Themes)
                {
                    item.IsDefault = false;
                }
                await _ThemeRepository.SaveChangesAsync();

                var Theme = await _ThemeRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();

                Theme.ModificationDate = DateTimeOffset.Now;
                Theme.ModifiedById = UserId;
                Theme.IsDefault = true;

                await _ThemeRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<bool> DeleteTheme(long Id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var theme = await _ThemeRepository.Query().Include(x => x.ImportedFiles).Where(x => x.Id == Id).FirstOrDefaultAsync();

                    if (theme == null)
                    {
                        throw new Exception("Theme Not Found");
                    }
                    if (theme.ImportedFiles != null)
                    {
                        _ThemeImportedFilesRepository.RemoveRange(theme.ImportedFiles);
                        await _ThemeImportedFilesRepository.SaveChangesAsync();
                    }


                    _ThemeRepository.Remove(theme);
                    await _ThemeRepository.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    throw ex;

                }
            });
        }

        public async Task<bool> DeleteImportedFile(long Id)
        {
            try
            {
                var ImportedFile = await _ThemeImportedFilesRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                _ThemeImportedFilesRepository.Remove(ImportedFile);
                await _ThemeImportedFilesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }
    }
}
