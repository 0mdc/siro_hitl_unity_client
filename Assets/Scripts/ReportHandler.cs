using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReportHandler : IKeyframeMessageConsumer
{
    public void ProcessMessage(Message message)
    {
        if (string.IsNullOrEmpty(message.report))
        {
            return;
        }

        Debug.LogError($"If a problem occurs, create a new entry in the error reporting spreadsheet and paste the following message.\n***\n{message.report}\n***");
    }

    void IUpdatable.Update() {}
}
