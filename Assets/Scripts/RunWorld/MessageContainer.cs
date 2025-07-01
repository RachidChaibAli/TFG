using System.Collections.Generic;

public class MessageContainer
{
    public List<Message> messages { get; set; }
}

public class Message
{
    public string origin { get; set; }
    public string target { get; set; }
    public string method { get; set; }
    public object content { get; set; }
}