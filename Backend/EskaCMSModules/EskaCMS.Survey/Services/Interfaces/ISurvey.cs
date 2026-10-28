
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Survey;
using EskaCMS.Survey.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Survey.Services.Interfaces
{
    public interface ISurvey
    {
        Task<List<Questionnaire>> GetAllQuestionnaires();
        Task<List<Section>> GetAllSections();
        Task<Questionnaire> GetQuestionnaire(long Id);
        Task<Section> GetSection(long Id);
        Task<bool> AddQuestionnaire(Questionnaire Questionnaire);
        Task<bool> AddSection(Section Section);
        Task<bool> EditQuestionnaire(Questionnaire Questionnaire);
        Task<bool> EditSection(Section Section);
        Task<bool> DeleteQuestionnaire(long Id);
        Task<bool> DeleteSection(long Id);
    }
}