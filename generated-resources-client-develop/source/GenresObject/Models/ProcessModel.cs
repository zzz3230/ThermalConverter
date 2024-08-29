using GeneratedResourceClient.GenresObject.Collections;
using GeneratedResourceClient.GraphMaster.Tools;
using Nntc.CSEx.Types.Range;

namespace GeneratedResourceClient.GenresObject.Models;

public class ProcessModel : BaseGenResObject
{
    public ProcessModel() : base("Process")
    {
    }
    public ProcessModel(Guid influencedObjectId, string type, Range<DateTime> period) : this(influencedObjectId, type)
    {
        StartDate = period.From;
        Days = (period.To - period.From).TotalDays;
    }
    public ProcessModel(Guid influencedObjectId, string type) : base(type)
    {
        InfluencedObjectId = influencedObjectId;
        ProcessType = type;
    }

    public ProcessModel(Guid influencedObjectId, string type, NamedTimeSeriesCollection scheduleSpending) : this(influencedObjectId, type)
    {
        ScheduleSpending = scheduleSpending;
    }
    public string? InvestingProjectListReference { get; set; }
    public Guid? InvestingProjectListId { get; set; }
    public DateTime? SplitDate { get; set; }
    public bool? isFact { get; set; }
    public string InfluencedObjectReference { get; set; }
    public string InfluencedObjectType { get; set; }
    public Guid InfluencedObjectId { get; set; }
    public string ProcessType { get; set; }
    public NamedTimeSeriesCollection ScheduleSpending { get; private set; }

    [NotGrEntity]
    public double? Days { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate => StartDate?.AddDays(Days ?? 0);

    public override string GetKey()
    {
        return $"{ProcessType}_{StartDate}_{Days}_{InfluencedObjectReference}";
    }

    protected virtual ProcessModel Copy()
    {
        return new ProcessModel(InfluencedObjectId, type, new NamedTimeSeriesCollection())
        {
            Days = Days,
            StartDate = StartDate,
            ProcessType = ProcessType,
            NameShortRu = NameShortRu
        };
    }
    
    public void SetId(Guid newId)
    {
        Id = newId;
    }
}