using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Helpers;

using EskaCMS.Entities.Entity;
using EskaCMS.Survey.Models;
using EskaCMS.Survey.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EskaCMS.Entities.Entity.Survey;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EskaCMS.Survey.Controllers
{
    [Route("api/survey")]
    public class SurveyController : Controller
    {
        private readonly ISurvey _service;

        public SurveyController(ISurvey service)
        {
            _service = service;
        }

        [HttpGet("enums")]
        public IActionResult GetQuestionnaireTypes()
        {
            var model = new
            {
                QuestionnaireType = EnumHelper.ToDictionary(typeof(QuestionnaireType)).Select(x => new { Id = x.Key, Name = x.Value }),
                QuestionType = EnumHelper.ToDictionary(typeof(QuestionType)).Select(x => new { Id = x.Key, Name = x.Value }),
                ChoiceType = EnumHelper.ToDictionary(typeof(ChoiceType)).Select(x => new { Id = x.Key, Name = x.Value }),
                AnswerFieldType = EnumHelper.ToDictionary(typeof(AnswerFieldType)).Select(x => new { Id = x.Key, Name = x.Value }),
                DateFormat = EnumHelper.ToDictionary(typeof(DateFormat)).Select(x => new { Id = x.Key, Name = x.Value })
            };
            return Json(model);
        }

        // GET: api/survey
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var items = await _service.GetAllQuestionnaires();
            return Json(items);
        }

        // GET: api/survey/section
        [HttpGet("section")]
        public async Task<IActionResult> GetSections()
        {
            var items = await _service.GetAllSections();
            return Json(items);
        }

        // GET api/survey/1
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var item = await _service.GetQuestionnaire(id);
            return Json(item);
        }

        // GET: api/survey/section/1
        [HttpGet("section/{id}")]
        public async Task<IActionResult> GetSection(long id)
        {
            var item = await _service.GetSection(id);
            return Json(item);
        }

        // POST api/survey
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Questionnaire value)
        {
            var success = await _service.AddQuestionnaire(value);
            if (success) return NoContent();
            else return BadRequest();
        }

        // POST api/survey/section
        [HttpPost("section")]
        public async Task<IActionResult> Post([FromBody]Section value)
        {
            var success = await _service.AddSection(value);
            if (success) return NoContent();
            else return BadRequest();
        }

        // DELETE api/survey/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var success = await _service.DeleteQuestionnaire(id);
            if (success) return NoContent();
            else return BadRequest();
        }

        // DELETE api/survey/1
        [HttpDelete("section/{id}")]
        public async Task<IActionResult> DeleteSection(long id)
        {
            var success = await _service.DeleteSection(id);
            if (success) return NoContent();
            else return BadRequest();
        }
    }
}
