using System.Diagnostics.Tracing;
using System.Text;

using var eventSourceListener = new EventSourceCreatedListener();

using var httpEventListener = new EventSourceListener("System.Net.Http");
using var httpEventInternalListener = new EventSourceListener("Private.InternalDiagnostics.System.Net.Http");
using var socketEventListener = new EventSourceListener("System.Net.Sockets");
using var socketDiagnosticsListener = new EventSourceListener("Private.InternalDiagnostics.System.Net.Sockets");

var client = new HttpClient();

var response = await client.GetAsync("https://nuget.org");

Console.WriteLine(response.StatusCode);


sealed class EventSourceCreatedListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        base.OnEventSourceCreated(eventSource);
        Console.WriteLine($"New event source: {eventSource.Name}");
    }
}

sealed class EventSourceListener : EventListener
{
    private readonly string _eventSourceName;
    private readonly StringBuilder _messageBuilder = new StringBuilder();

    public EventSourceListener(string name)
    {
        _eventSourceName = name;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        base.OnEventSourceCreated(eventSource);

        if (eventSource.Name == _eventSourceName)
        {
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        base.OnEventWritten(eventData);

        string message;
        lock (_messageBuilder)
        {
            _messageBuilder.Append("<- Event ");
            _messageBuilder.Append(eventData.EventSource.Name);
            _messageBuilder.Append(" - ");
            _messageBuilder.Append(eventData.EventName);
            _messageBuilder.Append(" : ");
            _messageBuilder.AppendJoin(',', eventData.Payload);
            if (eventData.EventName == "DumpBuffer" && eventData.Payload[2] is byte[] buffer)
            {
                _messageBuilder.Append(" DumpBuffer: ");
                _messageBuilder.Append(Encoding.UTF8.GetString(buffer.AsSpan(0, 128)));
            }
            _messageBuilder.AppendLine(" ->");
            message = _messageBuilder.ToString();
            _messageBuilder.Clear();
        }
        Console.WriteLine(message);
    }
}