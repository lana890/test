
using EskaCMS.Infrastructure.Data;

using EskaCMS.Entities.Entity;
using EskaCMS.Survey.Models;
using EskaCMS.Survey.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Survey;

namespace EskaCMS.Survey.Services
{
    public class SurveyService : ISurvey
    {
        private readonly IRepository<Questionnaire> _questionnairesRepository;
        private readonly IRepository<Section> _sectionsRepository;
        public SurveyService(
            IRepository<Questionnaire> questionnairesRepository,
            IRepository<Section> sectionsRepository
            )
        {
            _questionnairesRepository = questionnairesRepository;
            _sectionsRepository = sectionsRepository;
        }

        public async Task<bool> AddQuestionnaire(Questionnaire Questionnaire)
        {
            bool result;
            try
            {
                _questionnairesRepository.Add(Questionnaire);
                await _questionnairesRepository.SaveChangesAsync();
                result = true;
            } catch
            {
                result = false;
            }
            return result;
        }

        public async Task<bool> AddSection(Section Section)
        {
            bool result;
            try
            {
                _sectionsRepository.Add(Section);
                await _sectionsRepository.SaveChangesAsync();
                result = true;
            }
            catch (Exception e)
            {
                result = false;
            }
            return result;
        }

        public async Task<bool> DeleteQuestionnaire(long Id)
        {
            bool result;
            try
            {
                var entity = await _questionnairesRepository.Query().Where(x => x.Id == Id).SingleAsync();
                _questionnairesRepository.Remove(entity);
                await _questionnairesRepository.SaveChangesAsync();
                result = true;
            }
            catch (Exception e)
            {
                result = false;
            }
            return result;
        }

        public async Task<bool> DeleteSection(long Id)
        {
            bool result;
            try
            {
                var entity = await _sectionsRepository.Query().Where(x => x.Id == Id).SingleAsync();
                _sectionsRepository.Remove(entity);
                await _sectionsRepository.SaveChangesAsync();
                result = true;
            }
            catch (Exception e)
            {
                result = false;
            }
            return result;
        }

        public async Task<bool> EditQuestionnaire(Questionnaire Questionnaire)
        {
            bool result;
            try
            {
                _questionnairesRepository.Update(Questionnaire);
                await _questionnairesRepository.SaveChangesAsync();
                result = true;
            }
            catch (Exception e)
            {
                result = false;
            }
            return result;
        }

        public async Task<bool> EditSection(Section Section)
        {
            bool result;
            try
            {
                _sectionsRepository.Update(Section);
                await _sectionsRepository.SaveChangesAsync();
                result = true;
            }
            catch (Exception e)
            {
                result = false;
            }
            return result;
        }

        public async Task<List<Questionnaire>> GetAllQuestionnaires()
        {
            var result = await _questionnairesRepository.Query().ToListAsync();
            return result;
        }

        public async Task<List<Section>> GetAllSections()
        {
            var result = await _sectionsRepository.Query().ToListAsync();
            return result;
        }

        public async Task<Questionnaire> GetQuestionnaire(long Id)
        {
            var result = await _questionnairesRepository.Query().Where(x => x.Id == Id)
                .Include(x => x.Sections)
                .ThenInclude(x => x.Questions)
                .ThenInclude(x => x.MultipleChoiceAnswers).SingleAsync();
            return result;
        }

        public async Task<Section> GetSection(long Id)
        {
            var result = await _sectionsRepository.Query().Where(x => x.Id == Id)
                .Include(x => x.Questions)
                .ThenInclude(x => x.MultipleChoiceAnswers).SingleAsync();
            return result;
        }
    }
}
