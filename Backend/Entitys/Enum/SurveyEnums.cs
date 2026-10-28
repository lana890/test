using System.ComponentModel;

namespace EskaCMS.Entities.Enum
{
    public enum QuestionnaireType
    {
        Survey,
        Quiz
    }
    public enum QuestionType
    {
        [Description("Free Text")]
        UserInput,
        [Description("Multiple Choice")]
        MultipleAnswers,
        Name,
        Email,
        Address,
        Phone,
        [Description("Date/Time")]
        DateTime
    }
    public enum AnswerFieldType
    {
        [Description("Single Line")]
        SingleLine,
        [Description("Multi Line")]
        MultiLine,
        Password
    }
    public enum ChoiceType
    {
        Radio,
        [Description("Select (single selection)")]
        SingleSelect,
        Checkbox,
        [Description("Select (multiple selection)")]
        MultiSelect
    }
    public enum DateFormat
    {
        [Description("mm/dd/yyyy hh:mm")]
        MDT,
        [Description("dd/mm/yyyy hh:mm")]
        DMT,
        [Description("mm/dd/yyyy")]
        MD,
        [Description("dd/mm/yyyy")]
        DM,
        [Description("hh:mm")]
        T
    }
}

