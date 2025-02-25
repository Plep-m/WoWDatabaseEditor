using System;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections;

public class NeedsAwaitInspection : IEventInspection
{
    private int waitAction = -1;
    private int awaitAction = -1;
    private int needsAwait = -1;
            
    public NeedsAwaitInspection(ISmartDataManager smartDataManager)
    {
        foreach (var a in smartDataManager.GetAllData(SmartType.SmartAction))
        {
            if (a.Flags.HasFlagFast(ActionFlags.Await))
            {
                if (awaitAction != -1)
                    throw new Exception("Multiple await actions found");
                awaitAction = a.Id;
            }
            if (a.Flags.HasFlagFast(ActionFlags.WaitAction))
            {
                if (waitAction != -1)
                    throw new Exception("Multiple wait actions found");
                waitAction = a.Id;
            }
            if (a.Flags.HasFlagFast(ActionFlags.NeedsAwait))
            {
                if (needsAwait != -1)
                    throw new Exception("Multiple needs await actions found");
                needsAwait = a.Id;
            }
        }
    }
        
    public InspectionResult? Inspect(SmartEvent e)
    {
        bool awaitRequired = false;
        for (int i = e.Actions.Count - 1; i >= 0; --i)
        {
            if (e.Actions[i].Id == needsAwait)
                awaitRequired = true;

            if (e.Actions[i].Id == awaitAction || e.Actions[i].Id == waitAction)
                awaitRequired = false;
        }
            
        if (awaitRequired)
            return new InspectionResult()
            {
                Severity = DiagnosticSeverity.Error,
                Message = "Event contains a 'Loop', but no wait or await actions found. This will loop!",
                Line = e.LineId
            };

        return null;
    }
}